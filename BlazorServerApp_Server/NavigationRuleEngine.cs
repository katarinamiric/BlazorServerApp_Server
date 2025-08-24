using BlazorServerApp_Server.Redis; // Assuming RedisPageHistoryService is in this namespace
using BlazorServerApp_Server.Services; // Assuming NavigationPredictorService is in this namespace
using Microsoft.AspNetCore.Components; // For IComponent
using Microsoft.AspNetCore.Http; // For IHttpContextAccessor
using Microsoft.Extensions.DependencyInjection; // For IServiceScopeFactory (will remove)
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlazorServerApp_Server.Data;

// Removed the duplicate namespace BlazorServerApp_Server.Services.BlazorServerApp_Server.Services
// Assuming NavigationRuleEngine is directly in BlazorServerApp_Server
namespace BlazorServerApp_Server
{
    public class NavigationRuleEngine
    {
        private readonly RedisPrerenderRegistry _prerenderRegistry;
        private readonly ILogger<NavigationRuleEngine> _logger;
        private readonly NavigationPredictorService _navigationPredictor;
        // private readonly IServiceScopeFactory _scopeFactory; // Removed: No longer needed
        private readonly RedisPageHistoryService _redisPageHistoryService;
        private readonly IUserService _userService;
        private HashSet<Type> _lastRegisteredPageTypes = new HashSet<Type>();

        private readonly IHttpContextAccessor _httpContextAccessor;

        public NavigationRuleEngine(
            RedisPrerenderRegistry prerenderRegistry,
            ILogger<NavigationRuleEngine> logger,
            NavigationPredictorService navigationPredictor,
            RedisPageHistoryService redisPageHistoryService,
            IHttpContextAccessor httpContextAccessor, IUserService userService)
        {
            _prerenderRegistry = prerenderRegistry;
            _logger = logger;
            _navigationPredictor = navigationPredictor;
            _redisPageHistoryService = redisPageHistoryService;
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
        }

        public async Task<IEnumerable<string>> GetPagesToPrerender(string? currentPage)
        {
            if (string.IsNullOrEmpty(currentPage))
            {
                _logger.LogInformation("GetPagesToPrerender: Current page is empty. No pages to prerender.");
                return Enumerable.Empty<string>();
            }

            string userIdOrSession = "unknown";

            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                if (httpContext.User.Identity?.IsAuthenticated ?? false)
                {
                    // Authenticated user: stable user ID
                    userIdOrSession = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                      ?? httpContext.User.Identity.Name!;
                }
                else
                {
                    // Anonymous user: check cookie
                    const string AnonymousUserCookieName = "anon-user-id";
                    if (!httpContext.Request.Cookies.TryGetValue(AnonymousUserCookieName, out userIdOrSession))
                    {
                        userIdOrSession = Guid.NewGuid().ToString();
                        httpContext.Response.Cookies.Append(
                            AnonymousUserCookieName,
                            userIdOrSession,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Expires = DateTimeOffset.UtcNow.AddYears(1),
                                IsEssential = true,
                                Secure = httpContext.Request.IsHttps
                            });
                    }
                }
            }

            ApplicationUser? user = null;

            if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false)
            {
                // Fetch the user entity from the database
                user = await _userService.GetCurrentUserAsync();
            }

            string deviceType = RedisPageHistoryService.GetDeviceTypeFromUserAgent(
                _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "");

            var historyArray = await _redisPageHistoryService.GetLastNPagesAsync(userIdOrSession);

            string previousPage3 = historyArray[0]; // Oldest of the 3
            string previousPage2 = historyArray[1]; // Middle of the 3
            string previousPage1 = historyArray[2]; // Most recent of the 3

            _logger.LogInformation(
                $"GetPagesToPrerender: Requesting ML.NET prediction for '{currentPage}' with context (P1:{previousPage1}, P2:{previousPage2}, P3:{previousPage3}, User:5 Device:{deviceType}).");

            // Call the prediction method from NavigationPredictorService
            // This method now returns a List<string> of top predictions
            List<string> pagesToPrerender = new List<string>();
            if (user != null)
            {
                pagesToPrerender = _navigationPredictor.PredictNextPage(
                    previousPage1,
                    previousPage2,
                    previousPage3,
                    deviceType,
                    user.Gender,
                    maxPredictions: 3); // Request top 3 predictions
            }
            else
            {
                pagesToPrerender = _navigationPredictor.PredictNextPage(
                    previousPage1,
                    previousPage2,
                    previousPage3,
                    deviceType,
                    maxPredictions: 3); // Request top 3 predictions
            }

                var currentPageTypesToRegister = new HashSet<Type>();
            foreach (var targetUrl in pagesToPrerender)
            {
                var pageType = GetPageTypeFromUrl(targetUrl);
                if (pageType != null)
                {
                    currentPageTypesToRegister.Add(pageType);
                    _logger.LogDebug($"[NavigationRuleEngine] Predicted and added for prerendering: {targetUrl} -> {pageType.Name}");
                }
                else
                {
                    _logger.LogWarning($"[NavigationRuleEngine] Warning: No prerenderable component Type found for predicted URL: {targetUrl}");
                }
            }

            // Unregister pages that are no longer predicted
            foreach (var prevType in _lastRegisteredPageTypes)
            {
                if (!currentPageTypesToRegister.Contains(prevType))
                {
                    await _prerenderRegistry.UnregisterPageForPrerenderingAsync(prevType);
                    _logger.LogInformation($"[NavigationRuleEngine] Unregistering prerendering for {prevType.Name}.");
                }
            }

            // Register newly predicted pages
            foreach (var currentType in currentPageTypesToRegister)
            {
                // Only register if it's not already registered (avoids redundant calls)
                if (!_lastRegisteredPageTypes.Contains(currentType))
                {
                    await _prerenderRegistry.RegisterPageForPrerenderingAsync(currentType);
                    _logger.LogInformation($"[NavigationRuleEngine] Registering prerendering for {currentType.Name}.");
                }
            }

            _lastRegisteredPageTypes = currentPageTypesToRegister; // Update the set of currently registered types

            return pagesToPrerender;
        }

        /// <summary>
        /// Maps a URL string to its corresponding Blazor component Type for prerendering.
        /// </summary>
        /// <param name="url">The URL to map.</param>
        /// <returns>The Type of the Blazor component, or null if no mapping is found.</returns>
        public Type? GetPageTypeFromUrl(string url)
        {
            var normalizedUrl = url.TrimEnd('/');
            if (normalizedUrl == "") return typeof(Components.Pages.Home);
            if (normalizedUrl.Equals("/weather", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather);
            if (normalizedUrl.Equals("/weather2", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather2);
            // Assuming HeavyReportLive is the component that gets prerendered for /heavy-report
            if (normalizedUrl.Equals("/heavy-report", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Report);
            if (normalizedUrl.Contains("/product", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Product.ProductDetails);
            if (normalizedUrl.Contains("/about", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.About);
            if (normalizedUrl.Contains("/women/discounts", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Discounts);
            // Add other page mappings as needed
            return null;
        }

        // The UpdateRules method was commented out, so it's kept as-is (commented out)
        // public void UpdateRules(string sourcePage, string[] destinationPages)
        // {
        //     //_rules[sourcePage] = destinationPages;
        // }
    }
}
