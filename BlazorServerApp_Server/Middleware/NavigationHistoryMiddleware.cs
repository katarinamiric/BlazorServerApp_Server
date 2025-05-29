using BlazorServerApp_Server.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BlazorServerApp_Server.Middleware
{
    public class NavigationHistoryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<NavigationHistoryMiddleware> _logger;

        public NavigationHistoryMiddleware(RequestDelegate next, ILogger<NavigationHistoryMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context /*, IServiceScopeFactory serviceScopeFactory - no longer needed for Singleton */)
        {
            // Only track actual page requests, not static files, Blazor hub, etc.
            bool isPageRequest = !context.Request.Path.StartsWithSegments("/_framework") &&
                                 !context.Request.Path.StartsWithSegments("/_blazor") &&
                                 !context.Request.Path.StartsWithSegments("/css") &&
                                 !context.Request.Path.StartsWithSegments("/js") &&
                                 !context.Request.Path.Value!.Contains(".");

            if (isPageRequest)
            {
                // Get the Singleton service directly from the HttpContext.RequestServices
                // No need for IServiceScopeFactory for a Singleton service
                var pageHistoryService = context.RequestServices.GetRequiredService<InMemoryPageHistoryService>();

                string userId = context.Connection.Id; // Using Connection.Id for demo
                string pageUrl = context.Request.Path.Value!;
                string deviceType = InMemoryPageHistoryService.GetDeviceTypeFromUserAgent(context.Request.Headers["User-Agent"].ToString());

                try
                {
                    await pageHistoryService.AddPageVisitAsync(userId, pageUrl, deviceType);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error logging page visit in middleware.");
                }
            }

            await _next(context); // Continue processing the request
        }
    }

    // Extension method to easily add the middleware
    public static class NavigationHistoryMiddlewareExtensions
    {
        public static IApplicationBuilder UseNavigationHistory(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<NavigationHistoryMiddleware>();
        }
    }
}