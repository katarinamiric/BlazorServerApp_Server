using BlazorServerApp_Server;
using BlazorServerApp_Server.Components;
using BlazorServerApp_Server.Data;
using BlazorServerApp_Server.Hubs.BlazorServerApp_Server.Hubs;
using BlazorServerApp_Server.Middleware;
using BlazorServerApp_Server.Services;
using BlazorServerApp_Server.Services.BlazorServerApp_Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<HtmlCache>();
builder.Services.AddScoped<HtmlRenderer>();
//builder.Services.AddScoped<WeatherPrerenderService>();
builder.Services.AddHostedService<PrerenderService>();
builder.Services.AddSingleton<NavigationTracker>();
builder.Services.AddSingleton<NavigationRuleEngine>();
builder.Services.AddSingleton<NavigationPredictorService>();
builder.Services.AddScoped<BrowserHistoryService>();
builder.Services.AddHttpContextAccessor(); // <-- ADD THIS LINE
// Program.cs
builder.Services.AddSingleton<InMemoryPageHistoryService>();

builder.Services.AddScoped<BackgroundPagePrerenderer>();
builder.Services.AddSingleton<WeatherPrerenderDataService>(); // If WeatherForecastService is used for fetching data
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<SqlServiceBrokerListener>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<PrerenderRegistry>();
builder.Services.AddScoped<ReportDataService>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Weather2", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseNavigationHistory(); // <-- ADD THIS LINE

app.UseAntiforgery();
app.MapHub<WeatherHub>("/weatherhub");
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
