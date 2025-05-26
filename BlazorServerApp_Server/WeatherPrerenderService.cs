using BlazorServerApp_Server.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorServerApp_Server
    {
    public class WeatherPrerenderService
    {
        private readonly HtmlRenderer _renderer;
        private readonly HtmlCache _cache;

        public WeatherPrerenderService(HtmlRenderer renderer, HtmlCache cache)
        {
            _renderer = renderer;
            _cache = cache;
        }

        //public async Task PrerenderWeatherAsync()
        //{
        //    var html = await _renderer.Dispatcher.InvokeAsync(async () =>
        //    {
        //        var destinations = new[] { "FreezingCached", "BracingCached", "ChillyCached", "CoolCached", "MildCached", "WarmCached", "BalmyCached", "HotCached", "SwelteringCached", "ScorchingCached" };
        //        _cache.SetData("destinations", destinations);

        //        var result = await _renderer.RenderComponentAsync<Weather>(ParameterView.Empty);
        //        using var writer = new StringWriter();
        //        result.WriteHtmlTo(writer);
        //        return writer.ToString();
        //    });

        //    if (html != null) _cache.Set("/weather", html);
        //    Console.WriteLine("✅ Weather was prerendered after navigating to /home.");
        //}
    }

}
