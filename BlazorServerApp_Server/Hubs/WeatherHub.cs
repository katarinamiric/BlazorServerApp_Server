using Microsoft.AspNetCore.SignalR;

namespace BlazorServerApp_Server.Hubs
{

    namespace BlazorServerApp_Server.Hubs
    {
        public class WeatherHub : Hub
        {
            // You can add methods here that clients can call,
            // but for simple notifications, you'll primarily use Clients.All.SendAsync
            public async Task NotifyWeatherUpdate(string message)
            {
                await Clients.All.SendAsync("ReceiveWeatherUpdate", message);
            }
        }
    }
}
