using BlazorServerApp_Server.Services.BlazorServerApp_Server.Services;

namespace BlazorServerApp_Server
{
    public class NavigationRuleEngine
    {
        private readonly PrerenderRegistry _prerenderRegistry;

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
        };

        public IEnumerable<string> GetPagesToPrerender(string? currentPage)
        {
            if (currentPage == null) return Enumerable.Empty<string>();
            var currentPageTypesToRegister = new HashSet<Type>(); 
            var pagesToPrerender = _rules.TryGetValue(currentPage, out var targets) ? targets : Enumerable.Empty<string>();
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
        public Type? GetPageTypeFromUrl(string url)
        {
            var normalizedUrl = url.TrimEnd('/');
            if (normalizedUrl == "") return typeof(Components.Pages.Home);
            if (normalizedUrl.Equals("/weather", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather);
            if (normalizedUrl.Equals("/weather2", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather2);
            if (normalizedUrl.Equals("/contact", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.Weather2); 
            if (normalizedUrl.Equals("/heavy-report", StringComparison.OrdinalIgnoreCase)) return typeof(Components.Pages.ReportPrerendered); 

            return null; 
        }
        public void UpdateRules(string sourcePage, string[] destinationPages)
        {
            _rules[sourcePage] = destinationPages;
        }
    }

}
