//using System.Text.Encodings.Web;
//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Web;

//namespace BlazorServerApp_Server
//{
//    public class BackgroundPagePrerenderer
//    {
//        private readonly HtmlRenderer _renderer;
//        private readonly IServiceProvider _services;
//        private readonly ILogger<BackgroundPagePrerenderer> _logger;
//        private readonly HtmlCache _cache;

//        public BackgroundPagePrerenderer(HtmlRenderer renderer, IServiceProvider services, ILogger<BackgroundPagePrerenderer> logger, HtmlCache cache)
//        {
//            _renderer = renderer;
//            _services = services;
//            _logger = logger;
//            _cache = cache;
//        }

//        public async Task PrerenderComponentAsync<TComponent>() where TComponent : IComponent
//        {
//            try
//            {
//                var html = await _renderer.Dispatcher.InvokeAsync(async () =>
//                {
//                    var result = await _renderer.RenderComponentAsync<TComponent>(ParameterView.Empty);
//                    using var writer = new StringWriter();
//                    result.WriteHtmlTo(writer);
//                    return writer.ToString();
//                });

//                _logger.LogInformation($"[Prerender] {typeof(TComponent).Name} HTML: {html.Length} characters.");
//                // Store in Redis or MemoryCache if needed
//                var destinations = new[] { "FreezingCached", "BracingCached", "ChillyCached", "CoolCached", "MildCached", "WarmCached", "BalmyCached", "HotCached", "SwelteringCached", "ScorchingCached" };
//                        _cache.SetData("destinations", destinations);
//                if (html != null) _cache.Set("weather", html);
//                Console.WriteLine("✅ Weather was prerendered after navigating to /home.");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Weather2 prerendering {typeof(TComponent).Name}");
//            }
//        }
//    }

//}


using BlazorServerApp_Server.Components.Pages; // Assuming your Weather and Counter components are here
using BlazorServerApp_Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection; // Needed for CreateScope and GetRequiredService
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BlazorServerApp_Server
{
    public class BackgroundPagePrerenderer
    {
        private readonly HtmlRenderer _renderer;
        private readonly IServiceProvider _services;
        private readonly ILogger<BackgroundPagePrerenderer> _logger;
        private readonly HtmlCache _cache;

        // This dictionary maps a component Type to a function that, given an IServiceProvider,
        // will fetch the specific data for that component.
        private readonly Dictionary<Type, Func<IServiceProvider, Task<object?>>> _prerenderDataFetchers;

        public BackgroundPagePrerenderer(HtmlRenderer renderer, IServiceProvider services, ILogger<BackgroundPagePrerenderer> logger, HtmlCache cache)
        {
            _renderer = renderer;
            _services = services;
            _logger = logger;
            _cache = cache;

            // Initialize the dictionary with your page-specific data fetching logic
            _prerenderDataFetchers = new Dictionary<Type, Func<IServiceProvider, Task<object?>>>
            {
                // Example for Weather component:
                // It fetches WeatherForecast[] data using WeatherForecastService
                {
                    typeof(Weather),
                    async (sp) =>
                    {
                        var weatherService = sp.GetRequiredService<WeatherPrerenderDataService>();
                        // Always fetch the latest data for prerendering
                        return await weatherService.GetPrerenderDataAsync();
                    }
                },
                // Example for Counter component:
                // It doesn't fetch complex data; just returns an initial state or null
                {
                    typeof(Weather2),
                    (sp) => Task.FromResult<object?>(0) // Returns an initial integer value
                },
                {
                    typeof(ReportPrerendered),
                    async (sp) =>
                    {
                        var reportService = sp.GetRequiredService<ReportDataService>(); // Resolve your new service
                        var data = await reportService.GetComplexReportDataAsync(); // Fetch the data
                        data.DataSource = "Server Prerendered (fetched)"; // Mark source for the prerendered data
                        return data;
                    }
                }
                // Add more pages here as you expand your application:
                // {
                //     typeof(YourNewPage),
                //     async (sp) =>
                //     {
                //         var newPageService = sp.GetRequiredService<YourNewPageService>();
                //         return await newPageService.GetSpecificDataAsync();
                //     }
                // }
            };
        }

        public async Task PrerenderComponentAsync<TComponent>() where TComponent : IComponent
        {
            var pageName = typeof(TComponent).Name;
            string? html = null;
            object? pageData = null; // Use nullable object to store generic data

            try
            {
                // --- Step 1: Render the HTML of the component ---
                html = await _renderer.Dispatcher.InvokeAsync(async () =>
                {
                    // RenderComponentAsync handles injecting services into the component itself
                    var result = await _renderer.RenderComponentAsync<TComponent>(ParameterView.Empty);
                    using var writer = new StringWriter();
                    result.WriteHtmlTo(writer);
                    return writer.ToString();
                });

                // --- Step 2: Fetch Page-Specific Data using the predefined mapping ---
                // We fetch data in a separate scope to ensure services are correctly resolved
                // and avoid potential lifecycle issues with the main application scope.
                if (_prerenderDataFetchers.TryGetValue(typeof(TComponent), out var dataFetcher))
                {
                    await _renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        // Create a new service scope for fetching data
                        // This is crucial to ensure that any services resolved by the dataFetcher
                        // are correctly scoped and isolated from the main request.
                        using var scope = _services.CreateScope();
                        var serviceProviderInScope = scope.ServiceProvider;

                        pageData = await dataFetcher(serviceProviderInScope);
                    });
                }
                else
                {
                    _logger.LogWarning($"[Prerender] No data fetcher registered for {pageName}. Only HTML will be cached.");
                }

                _logger.LogInformation($"[Prerender] {pageName} HTML: {html?.Length ?? 0} characters. Data fetched: {pageData != null}.");

                // --- Step 3: Store HTML and Data in Cache ---
                if (html != null)
                {
                    _cache.Set(pageName, html); // Cache key is the page's component name
                }
                if (pageData != null)
                {
                    _cache.Set($"{pageName}_Data", pageData); // This now works!
                }

                Console.WriteLine($"✅ {pageName} was prerendered and cached.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error prerendering {pageName}. HTML Length: {html?.Length ?? 0}.");
            }
        }
    }
}