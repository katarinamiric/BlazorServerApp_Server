using Microsoft.JSInterop;

namespace BlazorServerApp_Server.Services
{
    public class BrowserHistoryService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<BrowserHistoryService> _logger;

        private List<string> _pageHistoryList = new List<string>();
        private const int MaxHistorySize = 3;

        public BrowserHistoryService(IJSRuntime jsRuntime, ILogger<BrowserHistoryService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
            for (int i = 0; i < MaxHistorySize; i++)
            {
                _pageHistoryList.Add("");
            }
        }

        /// <summary>
        /// Loads page history from browser's localStorage when the app starts.
        /// </summary>
        public async Task LoadHistoryAsync()
        {
            try
            {
                var loadedHistory = await _jsRuntime.InvokeAsync<List<string>>("blazorPageHistory.load");

                _pageHistoryList.Clear();

                var relevantHistory = loadedHistory.TakeLast(MaxHistorySize).ToList();
                _pageHistoryList.AddRange(relevantHistory);
                while (_pageHistoryList.Count < MaxHistorySize)
                {
                    _pageHistoryList.Insert(0, ""); // Insert at beginning to maintain order
                }

                _logger.LogInformation($"BrowserHistoryService: Loaded history from localStorage: {string.Join(" -> ", _pageHistoryList)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BrowserHistoryService: Error loading history from localStorage.");
            }
        }

        /// <summary>
        /// Adds a new page URL to the history and saves it to browser's localStorage.
        /// </summary>
        /// <param name="pageUrl">The absolute path of the navigated page.</param>
        public async Task AddPageToHistoryAsync(string pageUrl)
        {
            // Remove the oldest entry if history is full
            if (_pageHistoryList.Count >= MaxHistorySize)
            {
                _pageHistoryList.RemoveAt(0); // Remove the oldest page
            }
            _pageHistoryList.Add(pageUrl); // Add the new page as the most recent

            try
            {
                await _jsRuntime.InvokeVoidAsync("blazorPageHistory.save", _pageHistoryList.ToArray());
                _logger.LogInformation($"BrowserHistoryService: Added '{pageUrl}' to history. Current: {string.Join(" -> ", _pageHistoryList)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BrowserHistoryService: Error saving history for '{pageUrl}' to localStorage.");
            }
        }

        /// <summary>
        /// Returns the last 3 pages for the ML model (PreviousPage3, PreviousPage2, PreviousPage1).
        /// PreviousPage1 will be the most recent page, PreviousPage3 the oldest.
        /// </summary>
        /// <returns>An array of 3 strings: [Oldest, Middle, MostRecent].</returns>
        public string[] GetLast3Pages()
        {
            // Ensure we always return exactly MaxHistorySize elements
            // The list is already managed to be MaxHistorySize, so .ToArray() is sufficient.
            return _pageHistoryList.ToArray();
        }

        /// <summary>
        /// Clears the history in memory and in browser storage.
        /// </summary>
        public async Task ClearHistoryAsync()
        {
            _pageHistoryList.Clear();
            for (int i = 0; i < MaxHistorySize; i++)
            {
                _pageHistoryList.Add(""); // Reset with empty strings
            }
            try
            {
                await _jsRuntime.InvokeVoidAsync("blazorPageHistory.clear");
                _logger.LogInformation("BrowserHistoryService: History cleared.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BrowserHistoryService: Error clearing history from localStorage.");
            }
        }
    }
}