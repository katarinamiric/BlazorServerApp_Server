using BlazorServerApp_Server.Data; // For AppDbContext
using Microsoft.EntityFrameworkCore; // For IDbContextFactory
using StackExchange.Redis;

namespace BlazorServerApp_Server.Services
{
    public class RedisPageHistoryService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisPageHistoryService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory; // Correctly injecting the factory

        // Number of pages to keep in Redis for a user's history.
        // We need 4 pages in Redis to form a complete ML training entry (P3, P2, P1, NextPage).
        private const int RedisHistoryMaxCount = 4; // This is for the Redis list length

        // MaxHistorySizeForPrediction is for the output of GetLastNPagesAsync (3 previous pages)
        private const int MaxHistorySizeForPrediction = 3;

        public RedisPageHistoryService(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisPageHistoryService> logger,
            IDbContextFactory<ApplicationDbContext> dbContextFactory) // Correctly injecting the factory
        {
            _redisDb = connectionMultiplexer.GetDatabase();
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Records a page visit in Redis and, if enough history is available,
        /// generates and saves an ML training entry to the SQL Server database.
        /// </summary>
        public async Task AddPageVisitAsync(string userId, string pageUrl, string deviceType)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(pageUrl))
            {
                _logger.LogWarning("Attempted to log a page visit with null/empty userId or pageUrl.");
                return;
            }

            string normalizedPageUrl = NormalizeUrlForML(pageUrl);
            string redisKey = $"user:{userId}:history";

            try
            {
                // 1. Add current page to Redis history
                // LPUSH adds to the left (head) of the list. So, the newest visit will be at index 0.
                await _redisDb.ListLeftPushAsync(redisKey, normalizedPageUrl);

                // Trim the list to keep only the last 'RedisHistoryMaxCount' entries.
                // If RedisHistoryMaxCount is 4, we keep elements at index 0, 1, 2, 3.
                // The newest is at 0, oldest of the 4 is at 3.
                await _redisDb.ListTrimAsync(redisKey, 0, RedisHistoryMaxCount - 1);

                _logger.LogInformation($"RedisPageHistoryService: Recorded page visit for User={userId}, Page={normalizedPageUrl}. Current Redis list size: {await _redisDb.ListLengthAsync(redisKey)}");

                // 2. Try to generate and save AdvancedNavigationLogEntry for ML training
                // This logic is now back inside this service.
                if (await _redisDb.ListLengthAsync(redisKey) == RedisHistoryMaxCount)
                {
                    // Get the last 4 pages from Redis (LRange 0 to 3)
                    // This will be [CurrentPage_Url, PreviousPage1_Url, PreviousPage2_Url, PreviousPage3_Url] (newest to oldest)
                    RedisValue[] historyValues = await _redisDb.ListRangeAsync(redisKey, 0, RedisHistoryMaxCount - 1);

                    var logEntry = new AdvancedNavigationLogEntry
                    {
                        PreviousPage1Url = historyValues[1].ToString(), // P1: Page immediately preceding NextPageUrl
                        PreviousPage2Url = historyValues[2].ToString(), // P2: Page two before NextPageUrl
                        PreviousPage3Url = historyValues[3].ToString(), // P3: Page three before NextPageUrl
                        TimeOfDayInHours = (float)DateTime.UtcNow.TimeOfDay.TotalHours, // Time of day for the NextPageUrl visit
                        UserId = userId,
                        DeviceType = deviceType, // Device type recorded for the NextPageUrl visit
                        NextPageUrl = historyValues[0].ToString() // The current page is the 'NextPageUrl' for this sequence
                    };

                    await SaveAdvancedNavigationLogEntryAsync(logEntry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RedisPageHistoryService: Error processing page visit for User={userId}, Page={normalizedPageUrl}.");
            }
        }

        /// <summary>
        /// Retrieves the last N pages visited by a specific user from Redis for real-time prediction input.
        /// Returns them in chronological order (oldest of the N -> most recent).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="n">The number of pages to retrieve (defaults to 3 for ML input).</param>
        /// <returns>An array of strings representing the last N pages, padded with empty strings if less than N.</returns>
        public async Task<string[]> GetLastNPagesAsync(string userId, int n = MaxHistorySizeForPrediction) // n=3 for prediction input (P1, P2, P3)
        {
            string redisKey = $"user:{userId}:history";
            string[] result = new string[n];
            Array.Fill(result, ""); // Initialize with empty strings

            try
            {
                // LRange 1 to n gets N items starting from the second item (index 1) of the Redis list.
                // If Redis list is [CurrentPage, P1, P2, P3], and n=3, this gets [P1, P2, P3].
                RedisValue[] redisValues = await _redisDb.ListRangeAsync(redisKey, 1, n);

                // These are currently in order of newest to oldest (P1, P2, P3).
                // We need them oldest to newest for the output array (P3, P2, P1).
                var recentPages = redisValues.Select(v => v.ToString()).Reverse().ToArray();

                // Copy available recent pages into the result array, aligning to the right.
                // e.g., if n=3 and recentPages = {"/home", "/about"}, result will be {"", "/home", "/about"}
                Array.Copy(recentPages, 0, result, n - recentPages.Length, recentPages.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RedisPageHistoryService: Error retrieving history for User={userId} from Redis.");
            }

            return result;
        }

        /// <summary>
        /// Saves an AdvancedNavigationLogEntry to the SQL Server database.
        /// </summary>
        private async Task SaveAdvancedNavigationLogEntryAsync(AdvancedNavigationLogEntry entry)
        {
            using (var context = _dbContextFactory.CreateDbContext()) // Create a new DbContext instance per operation
            {
                context.AdvancedNavigationLogEntries.Add(entry);
                await context.SaveChangesAsync();
                _logger.LogDebug($"Saved AdvancedNavigationLogEntry to DB for user {entry.UserId}. Next page: {entry.NextPageUrl}");
            }
        }

        /// <summary>
        /// Retrieves all AdvancedNavigationLogEntry records from the database for ML training.
        /// This is intended for periodic retraining, not per-request.
        /// </summary>
        public async Task<List<AdvancedNavigationLogEntry>> GetAllAdvancedNavigationLogEntriesAsync()
        {
            using (var context = _dbContextFactory.CreateDbContext()) // Create a new DbContext instance per operation
            {
                _logger.LogInformation("Fetching all AdvancedNavigationLogEntries from database for ML retraining.");
                return await context.AdvancedNavigationLogEntries.AsNoTracking().ToListAsync();
            }
        }

        // Helper to normalize URLs for ML.NET training and consistent storage
        private string NormalizeUrlForML(string url)
        {
            if (url.StartsWith("/product/", StringComparison.OrdinalIgnoreCase) && url.Length > "/product/".Length)
            {
                if (int.TryParse(url.Substring("/product/".Length), out _))
                {
                    return "/product/{id}";
                }
            }
            return url.TrimEnd('/');
        }

        // This method remains static as it doesn't depend on service state
        public static string GetDeviceTypeFromUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";
            userAgent = userAgent.ToLowerInvariant();
            if (userAgent.Contains("mobile") && !userAgent.Contains("tablet")) return "Mobile";
            if (userAgent.Contains("ipad") || (userAgent.Contains("android") && userAgent.Contains("tablet"))) return "Tablet";
            if (userAgent.Contains("windows") || userAgent.Contains("macintosh") || userAgent.Contains("linux")) return "Desktop";
            return "Other";
        }
    }
}