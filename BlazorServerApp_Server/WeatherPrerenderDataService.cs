using BlazorServerApp_Server;
using BlazorServerApp_Server.Components.Pages;

public class WeatherPrerenderDataService : IPrerenderDataService<Weather, string[]>
{
    public WeatherPrerenderDataService()
    {
    }

    public Task<string[]> GetPrerenderDataAsync()
    {
        return Task.FromResult(new[] { "FreezingCached", "BracingCached", "ChillyCached", "CoolCached", "MildCached", "WarmCached", "BalmyCached", "HotCached", "SwelteringCached", "ScorchingCached" });
    }
}