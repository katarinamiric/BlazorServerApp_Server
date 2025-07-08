using System.CodeDom;
using BlazorServerApp_Server.Components.Pages;
using BlazorServerApp_Server.Components.Pages.Product;
using BlazorServerApp_Server.Data.Model;
using BlazorServerApp_Server.Services;
using BlazorServerApp_Server.Services.BlazorServerApp_Server.Services;

namespace BlazorServerApp_Server
{
    public class NavigationRuleEngine
    {
        private readonly PrerenderRegistry _prerenderRegistry;
        private readonly ILogger<NavigationRuleEngine> _logger;
        private readonly NavigationPredictorService _navigationPredictor;
        private const int MaxHistory = 3;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly InMemoryPageHistoryService _inMemoryPageHistoryService;
        private HashSet<Type> _lastRegisteredPageTypes = new HashSet<Type>();

        private readonly IHttpContextAccessor _httpContextAccessor;
        private Queue<string> _pageHistory = new Queue<string>(3);
        public NavigationRuleEngine(PrerenderRegistry prerenderRegistry, ILogger<NavigationRuleEngine> logger,
            NavigationPredictorService navigationPredictor, IServiceScopeFactory scopeFactory,
            InMemoryPageHistoryService inMemoryPageHistoryService, IHttpContextAccessor httpContextAccessor)
        {
            _prerenderRegistry = prerenderRegistry;
            _logger = logger;
            _navigationPredictor = navigationPredictor;
            _scopeFactory = scopeFactory;
            _inMemoryPageHistoryService = inMemoryPageHistoryService;
            _httpContextAccessor = httpContextAccessor;


            for (int i = 0; i < MaxHistory; i++) _pageHistory.Enqueue("");
        }


        private readonly Dictionary<string, string[]> _rules = new()
        {
            { "", new[] { "/weather" } },
            //{ "weather-prerendered", new[] { "/weather2" } },
            { "weather-prerendered", new[] { "/heavy-report" } }
        };

        public async Task<IEnumerable<string>> GetPagesToPrerender(string? currentPage)
        {
            using (var scope = _scopeFactory.CreateScope()) // NEW: Create a new scope
            {
                var _context = scope.ServiceProvider.GetRequiredService<BrowserHistoryService>();
                if (string.IsNullOrEmpty(currentPage))
                {
                    _logger.LogInformation("GetPagesToPrerender: Current page is empty. No pages to prerender.");
                    return Enumerable.Empty<string>();
                }

                string userId = _httpContextAccessor.HttpContext?.Connection.Id ?? "default_anonymous_user";
                string deviceType = InMemoryPageHistoryService.GetDeviceTypeFromUserAgent( // Use InMemoryPageHistoryService's helper
                    _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "");

                // Get the last 3 pages from the InMemoryPageHistoryService
                var historyArray = await _inMemoryPageHistoryService.GetLastNPagesAsync(userId);

                // Get the last 3 pages from history (most recent first)
                string previousPage1 = historyArray[2]; // Most recent
                string previousPage2 = historyArray[1]; // Second most recent
                string previousPage3 = historyArray[0]; // Third most recent

                // Get current time of day
                float timeOfDayInHours = (float)DateTime.Now.Hour + (float)DateTime.Now.Minute / 60f;

                // Simulate User ID and Device Type (replace with actual values in a real app)
                //string userId = "simulated_user_id"; // Get from AuthenticationStateProvider, etc.
                //string deviceType = "Desktop"; // Get from JS interop (User-Agent string analysis) or fixed for demo

                _logger.LogInformation(
                    $"GetPagesToPrerender: Requesting ML.NET prediction for '{currentPage}' with context (P1:{previousPage1}, P2:{previousPage2}, P3:{previousPage3}, Time:{timeOfDayInHours}, User:{userId}, Device:{deviceType}).");

                // Use the advanced ML.NET predictor to get the next pages
                var pagesToPrerender = _navigationPredictor.PredictNextPages(
                    currentPage, // This is P1
                    previousPage2,
                    previousPage3,
                    timeOfDayInHours,
                    userId,
                    deviceType,
                    maxPredictions: 3).ToList();

                //var pagesToPrerender = _navigationPredictor.PredictNextPages(currentPage, maxPredictions: 1).ToList();

                // ... (rest of your logic to convert URLs to Types, register/unregister pages) ...

                var currentPageTypesToRegister = new HashSet<Type>();
                foreach (var targetUrl in pagesToPrerender)
                {
                    var pageType = GetPageTypeFromUrl(targetUrl);
                    if (pageType != null)
                    {
                        currentPageTypesToRegister.Add(pageType);
                    }
                    else
                    {
                        Console.WriteLine($"[NavigationRuleEngine] Warning: No page Type found for URL: {targetUrl}");
                    }
                }

                foreach (var prevType in _lastRegisteredPageTypes)
                {
                    if (!currentPageTypesToRegister.Contains(prevType))
                    {
                        _prerenderRegistry.UnregisterPageForPrerendering(prevType);
                    }
                }

                foreach (var currentType in currentPageTypesToRegister)
                {
                    _prerenderRegistry.RegisterPageForPrerendering(currentType);
                }

                _lastRegisteredPageTypes = currentPageTypesToRegister;

                return pagesToPrerender;
            }
        }
        public Type? GetPageTypeFromUrl(string url)
        {
            var normalizedUrl = url.TrimEnd('/');
            if (normalizedUrl == "") return typeof(Components.Pages.Home);
            if (normalizedUrl.Equals("/weather", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather);
            if (normalizedUrl.Equals("/weather2", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather2);
            //if (normalizedUrl.Equals("/contact", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather2); 
            if (normalizedUrl.Equals("/heavy-report", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.ReportPrerendered); 
            if (normalizedUrl.Contains("/product", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Product.ProductDetails); 

            return null; 
        }
        public void UpdateRules(string sourcePage, string[] destinationPages)
        {
            _rules[sourcePage] = destinationPages;
        }
    }

}
