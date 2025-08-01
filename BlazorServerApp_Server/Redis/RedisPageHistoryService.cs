using BlazorServerApp_Server.Data; 
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BlazorServerApp_Server.Services
{
    public class RedisPageHistoryService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisPageHistoryService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        private const int RedisHistoryMaxCount = 4; 

        private const int MaxHistorySizeForPrediction = 3;

        public RedisPageHistoryService(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisPageHistoryService> logger,
            IDbContextFactory<ApplicationDbContext> dbContextFactory) 
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
                await _redisDb.ListLeftPushAsync(redisKey, normalizedPageUrl);

                await _redisDb.ListTrimAsync(redisKey, 0, RedisHistoryMaxCount - 1);

                _logger.LogInformation($"RedisPageHistoryService: Recorded page visit for User={userId}, Page={normalizedPageUrl}. Current Redis list size: {await _redisDb.ListLengthAsync(redisKey)}");

                if (await _redisDb.ListLengthAsync(redisKey) == RedisHistoryMaxCount)
                {
                    RedisValue[] historyValues = await _redisDb.ListRangeAsync(redisKey, 0, RedisHistoryMaxCount - 1);

                    var logEntry = new AdvancedNavigationLogEntry
                    {
                        PreviousPage1Url = historyValues[1].ToString(),
                        PreviousPage2Url = historyValues[2].ToString(), 
                        PreviousPage3Url = historyValues[3].ToString(), 
                        TimeOfDayInHours = (float)DateTime.UtcNow.TimeOfDay.TotalHours, 
                        UserId = userId,
                        DeviceType = deviceType,
                        NextPageUrl = historyValues[0].ToString() 
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
        public async Task<string[]> GetLastNPagesAsync(string userId, int n = MaxHistorySizeForPrediction)
        {
            string redisKey = $"user:{userId}:history";
            string[] result = new string[n];
            Array.Fill(result, ""); 

            try
            {
                RedisValue[] redisValues = await _redisDb.ListRangeAsync(redisKey, 1, n);

                var recentPages = redisValues.Select(v => v.ToString()).Reverse().ToArray();

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
            using (var context = _dbContextFactory.CreateDbContext()) 
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
            using (var context = _dbContextFactory.CreateDbContext())
            {
                _logger.LogInformation("Fetching all AdvancedNavigationLogEntries from database for ML retraining.");
                return await context.AdvancedNavigationLogEntries.AsNoTracking().ToListAsync();
            }
        }

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