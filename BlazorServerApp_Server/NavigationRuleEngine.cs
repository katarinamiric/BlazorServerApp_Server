using BlazorServerApp_Server.Services.BlazorServerApp_Server.Services;

namespace BlazorServerApp_Server
{
    public class NavigationRuleEngine
    {
        private readonly PrerenderRegistry _prerenderRegistry; // Inject the registry

        public NavigationRuleEngine(PrerenderRegistry prerenderRegistry)
        {
            _prerenderRegistry = prerenderRegistry;
        }

        private HashSet<Type> _lastRegisteredPageTypes = new HashSet<Type>();

        private readonly Dictionary<string, string[]> _rules = new()
        {
            { "", new[] { "/weather" } },
            //{ "weather-prerendered", new[] { "/weather2" } },
            { "/contact", new[] { "/weather2" } },
            { "weather-prerendered", new[] { "/heavy-report" } }
            // Later, you can update this based on usage statistics.
        };

        public IEnumerable<string> GetPagesToPrerender(string? currentPage)
        {
            if (currentPage == null) return Enumerable.Empty<string>();
            var currentPageTypesToRegister = new HashSet<Type>(); // Pages that should be active for this prediction cycle
            var pagesToPrerender = _rules.TryGetValue(currentPage, out var targets) ? targets : Enumerable.Empty<string>();
            foreach (var targetUrl in pagesToPrerender)
            {
                var pageType = GetPageTypeFromUrl(targetUrl); // Convert URL string to Type
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
            // Register pages that are newly predicted or remain active
            foreach (var currentType in currentPageTypesToRegister)
            {
                _prerenderRegistry.RegisterPageForPrerendering(currentType);
            }

            // Update the set of last registered pages for the next cycle
            _lastRegisteredPageTypes = currentPageTypesToRegister;

            return pagesToPrerender;
        }
        public Type? GetPageTypeFromUrl(string url)
        {
            // Normalize URL for consistent matching
            var normalizedUrl = url.TrimEnd('/');
            if (normalizedUrl == "") return typeof(Components.Pages.Home); // Assuming Home page is at "/"
            if (normalizedUrl.Equals("/weather", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather);
            if (normalizedUrl.Equals("/weather2", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather2);
            if (normalizedUrl.Equals("/contact", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather2); // Assuming you have a Weather2.razor
            if (normalizedUrl.Equals("/heavy-report", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.ReportPrerendered); // Assuming you have a Weather2.razor
            //if (normalizedUrl.Equals("/counter", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Counter);
            //if (normalizedUrl.Equals("/contact", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Contact);
            // Add more mappings for your other pages
            // Example: For dynamic routes like /product/{id}, you might map to typeof(ProductDetail)
            // if (url.StartsWith("/product/", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.ProductDetail);

            return null; // No matching page type found
        }
        // Optional: expose a method to change the rules dynamically
        public void UpdateRules(string sourcePage, string[] destinationPages)
        {
            _rules[sourcePage] = destinationPages;
        }
    }

}
