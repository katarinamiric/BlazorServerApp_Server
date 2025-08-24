using System.Security.Claims;
using BlazorServerApp_Server.Services;

namespace BlazorServerApp_Server.Middleware
{
    public class NavigationHistoryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<NavigationHistoryMiddleware> _logger;
        private const string AnonymousUserCookieName = "anon-user-id";

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
                string userId;

                if (context.User.Identity?.IsAuthenticated ?? false)
                {
                    userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? context.User.Identity.Name!;
                }
                else
                {
                    if (!context.Request.Cookies.TryGetValue(AnonymousUserCookieName, out userId))
                    {
                        userId = Guid.NewGuid().ToString();

                        context.Response.Cookies.Append(
                            AnonymousUserCookieName,
                            userId,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Expires = DateTimeOffset.UtcNow.AddYears(1),
                                IsEssential = true,
                                Secure = context.Request.IsHttps
                            });
                    }
                }

                var pageHistoryService = context.RequestServices.GetRequiredService<RedisPageHistoryService>();

                string pageUrl = context.Request.Path.Value!;
                string deviceType = RedisPageHistoryService.GetDeviceTypeFromUserAgent(context.Request.Headers["User-Agent"].ToString());

                try
                {
                    await pageHistoryService.AddPageVisitAsync(userId, pageUrl, deviceType);
                }
                catch (Exception ex)
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