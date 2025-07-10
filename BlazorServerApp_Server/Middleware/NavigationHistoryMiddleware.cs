using BlazorServerApp_Server.Services;

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

        public async Task InvokeAsync(HttpContext context)
        {
            bool isPageRequest = !context.Request.Path.StartsWithSegments("/_framework") &&
                                 !context.Request.Path.StartsWithSegments("/_blazor") &&
                                 !context.Request.Path.StartsWithSegments("/css") &&
                                 !context.Request.Path.StartsWithSegments("/js") &&
                                 !context.Request.Path.Value!.Contains(".");

            if (isPageRequest)
            {
                var pageHistoryService = context.RequestServices.GetRequiredService<RedisPageHistoryService>();

                string userId = context.Connection.Id; //samo za svrhu demonstracije
                string pageUrl = context.Request.Path.Value!;
                string deviceType = RedisPageHistoryService.GetDeviceTypeFromUserAgent(context.Request.Headers["User-Agent"].ToString());

                try
                {
                    await pageHistoryService.AddPageVisitAsync(userId, pageUrl, deviceType);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error logging page visit in middleware.");
                }
            }

            await _next(context); 
        }
    }

    public static class NavigationHistoryMiddlewareExtensions
    {
        public static IApplicationBuilder UseNavigationHistory(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<NavigationHistoryMiddleware>();
        }
    }
}