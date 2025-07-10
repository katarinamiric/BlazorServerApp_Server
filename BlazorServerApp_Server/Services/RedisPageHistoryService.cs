using StackExchange.Redis;

namespace BlazorServerApp_Server.Services
{
    public class RedisPageHistoryService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisPageHistoryService> _logger;

        // We need the last 3 pages for ML prediction input.
        // If we store 3 items in a Redis List, the first item added will be the oldest, last added will be newest.
        // ListLeftPush adds to the head (left). So, the newest item is at index 0.
        // If we want to retrieve oldest to newest for ML model input (P3, P2, P1), we'll need to reverse.
        private const int MaxHistorySizeForPrediction = 3; // To store the 3 previous pages needed for ML prediction

        public RedisPageHistoryService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisPageHistoryService> logger)
        {
            _redisDb = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        /// <summary>
        /// Records a page visit in Redis for a specific user.
        /// This stores the URL, trimming the list to keep only the most recent N visits.
        /// </summary>
        public async Task AddPageVisitAsync(string userId, string pageUrl, string deviceType) // deviceType is logged but not stored in Redis List here
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(pageUrl))
            {
                _logger.LogWarning("Attempted to log a page visit with null/empty userId or pageUrl.");
                return;
            }

            // Normalize URL if you have dynamic routes like /product/123
            string normalizedPageUrl = NormalizeUrlForML(pageUrl);

            string redisKey = $"user:{userId}:history";

            try
            {
                // LPUSH adds to the left (head) of the list. So, the newest visit will be at index 0.
                await _redisDb.ListLeftPushAsync(redisKey, normalizedPageUrl);

                // Trim the list to keep only the last MaxHistorySizeForPrediction entries.
                // If MaxHistorySizeForPrediction is 3, we keep elements at index 0, 1, 2.
                // The newest is at 0, oldest is at 2.
                await _redisDb.ListTrimAsync(redisKey, 0, MaxHistorySizeForPrediction - 1);

                _logger.LogInformation($"RedisPageHistoryService: Recorded page visit for User={userId}, Page={normalizedPageUrl}. Current Redis list size: {await _redisDb.ListLengthAsync(redisKey)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RedisPageHistoryService: Error adding page visit for User={userId}, Page={normalizedPageUrl} to Redis.");
            }
        }

        /// <summary>
        /// Retrieves the last N pages visited by a specific user from Redis.
        /// Returns them in chronological order (oldest of the N -> most recent).
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="n">The number of pages to retrieve (defaults to 3 for ML input).</param>
        /// <returns>An array of strings representing the last N pages, padded with empty strings if less than N.</returns>
        public async Task<string[]> GetLastNPagesAsync(string userId, int n = MaxHistorySizeForPrediction)
        {
            string redisKey = $"user:{userId}:history";
            string[] result = new string[n];
            Array.Fill(result, ""); // Initialize with empty strings

            try
            {
                // LRange 0 (start) to n-1 (end) gets the newest N items from the left (head) of the list.
                // Example: if list is [D, C, B, A] (D is newest, A is oldest), LRange 0 2 gives [D, C, B]
                RedisValue[] redisValues = await _redisDb.ListRangeAsync(redisKey, 0, n - 1);

                // We need to reverse these values to get them in chronological order (oldest to newest of the N).
                // If redisValues is [D, C, B], after reverse it's [B, C, D]
                var recentPages = redisValues.Select(v => v.ToString()).Reverse().ToArray();

                // Copy available recent pages into the result array, aligning to the right.
                // This means if we ask for 3 pages and only have 1 (e.g., "A"), result will be {"", "", "A"}
                Array.Copy(recentPages, 0, result, n - recentPages.Length, recentPages.Length);

                _logger.LogDebug($"RedisPageHistoryService: Retrieved last {n} pages for user {userId}: {string.Join(" -> ", result)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RedisPageHistoryService: Error retrieving history for User={userId} from Redis.");
                // On error, return padded empty strings to avoid breaking prediction
            }

            return result;
        }

        // Helper to normalize URLs for ML.NET training and consistent storage
        // This is important for routes like /product/123 -> /product/{id}
        private string NormalizeUrlForML(string url)
        {
            if (url.StartsWith("/product/", StringComparison.OrdinalIgnoreCase) && url.Length > "/product/".Length)
            {
                // Simple check for digits after /product/
                if (int.TryParse(url.Substring("/product/".Length), out _))
                {
                    return "/product/{id}";
                }
            }
            // Add more normalization rules here as needed for other dynamic routes
            // e.g., /category/shoes/page/1 -> /category/{name}/page/{num}
            // Be careful not to normalize static pages like /weather if they are distinct.
            return url.TrimEnd('/'); // Remove trailing slashes for consistency
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