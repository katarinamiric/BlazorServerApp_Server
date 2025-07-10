using System.Collections.Concurrent;

namespace BlazorServerApp_Server.Services
{
    public class InMemoryPageHistoryService
    {
        private readonly ConcurrentDictionary<string, List<string>> _userHistories = new ConcurrentDictionary<string, List<string>>();
        private readonly ILogger<InMemoryPageHistoryService> _logger;
        private const int MaxHistorySize = 3;

        public InMemoryPageHistoryService(ILogger<InMemoryPageHistoryService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Records a page visit in memory for a specific user.
        /// </summary>
        public Task AddPageVisitAsync(string userId, string pageUrl, string deviceType)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(pageUrl))
            {
                _logger.LogWarning("Attempted to add page visit with empty UserId or PageUrl.");
                return Task.CompletedTask;
            }

            var history = _userHistories.GetOrAdd(userId, _ => new List<string>());

            lock (history)
            {
                // Remove the oldest entry if history is full
                if (history.Count >= MaxHistorySize)
                {
                    history.RemoveAt(0);
                }
                history.Add(pageUrl);
            }

            _logger.LogInformation($"InMemoryPageHistoryService: Recorded page visit: User={userId}, Page={pageUrl}. Current for user: {string.Join(" -> ", history)}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves the last N pages visited by a specific user from memory.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An array of strings representing the last N pages, padded with empty strings if less than N.</returns>
        public Task<string[]> GetLastNPagesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId) || !_userHistories.TryGetValue(userId, out var history))
            {
                _logger.LogInformation($"InMemoryPageHistoryService: No history found for User={userId}. Returning empty pages.");
                return Task.FromResult(new string[MaxHistorySize] { "", "", "" });
            }

            string[] result;
            lock (history)
            {
                var relevantHistory = history.TakeLast(MaxHistorySize).ToList();

                result = new string[MaxHistorySize];
                for (int i = 0; i < MaxHistorySize; i++)
                {
                    result[i] = (i < relevantHistory.Count) ? relevantHistory[i] : "";
                }
            }

            _logger.LogInformation($"InMemoryPageHistoryService: Retrieved history for User={userId}: {string.Join(" -> ", result)}");
            return Task.FromResult(result);
        }

        /// <summary>
        /// (Optional) Helper to extract a basic device type from User-Agent.
        /// (Can be used by the middleware, even if not stored here).
        /// </summary>
        public static string GetDeviceTypeFromUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";
            if (userAgent.Contains("Mobi") || userAgent.Contains("Android") || userAgent.Contains("iPhone")) return "Mobile";
            if (userAgent.Contains("Tablet") || userAgent.Contains("iPad")) return "Tablet";
            return "Desktop";
        }
    }
}