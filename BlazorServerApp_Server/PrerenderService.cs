using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Encodings.Web;

namespace BlazorServerApp_Server
{
    public class PrerenderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly HtmlCache _cache;

        public PrerenderService(IServiceProvider serviceProvider, HtmlCache cache)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();

            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var rendered = await renderer.RenderComponentAsync<Components.Pages.Weather>(ParameterView.Empty);
                using var writer = new StringWriter();
                rendered.WriteHtmlTo(writer);
                return writer.ToString();
            });

            _cache.Set("weather", html);
        }
    }
}