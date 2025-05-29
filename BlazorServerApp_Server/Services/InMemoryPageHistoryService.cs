using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent; // For thread-safe dictionary
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorServerApp_Server.Services
{
    public class InMemoryPageHistoryService
    {
        // Stores history per user/connection. Key: UserId, Value: List of page URLs
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
        public Task AddPageVisitAsync(string userId, string pageUrl, string deviceType) // DeviceType can be ignored for simple in-memory
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(pageUrl))
            {
                _logger.LogWarning("Attempted to add page visit with empty UserId or PageUrl.");
                return Task.CompletedTask;
            }

            // Get or create the user's history list
            var history = _userHistories.GetOrAdd(userId, _ => new List<string>());

            // Ensure thread-safe modification of the list
            lock (history)
            {
                // Remove the oldest entry if history is full
                if (history.Count >= MaxHistorySize)
                {
                    history.RemoveAt(0); // Remove the oldest page
                }
                history.Add(pageUrl); // Add the new page as the most recent
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
                // If user not found or history is empty, return an array of empty strings
                _logger.LogInformation($"InMemoryPageHistoryService: No history found for User={userId}. Returning empty pages.");
                return Task.FromResult(new string[MaxHistorySize] { "", "", "" });
            }

            string[] result;
            lock (history) // Ensure thread-safe access
            {
                // Get the last N elements
                var relevantHistory = history.TakeLast(MaxHistorySize).ToList();

                // Pad with empty strings if there aren't enough entries
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