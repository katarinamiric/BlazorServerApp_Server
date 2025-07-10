using BlazorServerApp_Server;
using BlazorServerApp_Server.Components.Pages;
using BlazorServerApp_Server.Data;

public class WeatherPrerenderDataService : IPrerenderDataService<Weather, string[]>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WeatherPrerenderDataService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task<string[]> GetPrerenderDataAsync()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reportData = _context.Weather.ToList();

            return Task.FromResult(reportData.Select(w => w.Value).ToArray());
        }
    }
}