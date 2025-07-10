using BlazorServerApp_Server.Hubs.BlazorServerApp_Server.Hubs;
using BlazorServerApp_Server.Services.BlazorServerApp_Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Xml.Linq;
using BlazorServerApp_Server.Redis;

namespace BlazorServerApp_Server.Services
{
    public class SqlServiceBrokerListener : BackgroundService
    {
        private readonly IHubContext<WeatherHub> _hubContext;
        private readonly ILogger<SqlServiceBrokerListener> _logger;
        private readonly string _connectionString;
        private readonly string _targetQueueName = "WeatherForecastChange_TargetQueue";
        private readonly IServiceScopeFactory _scopeFactory; 

        public SqlServiceBrokerListener(
            IHubContext<WeatherHub> hubContext,
            ILogger<SqlServiceBrokerListener> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory) 
        {
            _hubContext = hubContext;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException("DefaultConnection connection string not found.");
            _scopeFactory = scopeFactory; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SQL Service Broker Listener starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope()) 
                {

                    var prerenderer = scope.ServiceProvider.GetRequiredService<BackgroundPagePrerenderer>();
                    var prerenderRegistry = scope.ServiceProvider.GetRequiredService<RedisPrerenderRegistry>();

                    try
                    {
                        using (var connection = new SqlConnection(_connectionString))
                        {
                            await connection.OpenAsync(stoppingToken);

                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = $"WAITFOR (RECEIVE TOP(1) message_type_name, message_body FROM {_targetQueueName}), TIMEOUT 60000;";
                                using (var reader = await command.ExecuteReaderAsync(stoppingToken))
                                {
                                    if (reader.Read())
                                    {
                                        var messageType = reader.GetString(0);
                                        var messageBodyBytes = reader.GetSqlBytes(1).Buffer;
                                        var messageBody = System.Text.Encoding.Unicode.GetString(messageBodyBytes);

                                        _logger.LogInformation($"Received message from DB: Type={messageType}, Body={messageBody}");

                                        string? changedTableName = null;
                                        try
                                        {
                                            var xmlDoc = XDocument.Parse(messageBody);
                                            changedTableName = xmlDoc.Root?.Element("Table")?.Value;
                                        }
                                        catch (Exception parseEx)
                                        {
                                            _logger.LogError(parseEx, "Failed to parse messageBody as XML. Ensure trigger sends valid XML.");
                                        }

                                        if (!string.IsNullOrEmpty(changedTableName))
                                        {
                                            var affectedComponentType = prerenderRegistry.GetComponentTypeForTable(changedTableName);

                                            if (affectedComponentType != null)
                                            {
                                                if (await prerenderRegistry.IsPageActivelyPrerenderedAsync(affectedComponentType)) 
                                                {
                                                    _logger.LogInformation($"DB change detected for {changedTableName}. Page {affectedComponentType.Name} is actively prerendered. Re-prerendering...");
                                                    var prerenderMethod = typeof(BackgroundPagePrerenderer)
                                                        .GetMethod(nameof(BackgroundPagePrerenderer.PrerenderComponentAsync))!
                                                        .MakeGenericMethod(affectedComponentType);

                                                    await (Task)prerenderMethod.Invoke(prerenderer, null)!; 

                                                    await _hubContext.Clients.All.SendAsync("ReceivePageUpdate", affectedComponentType.Name, stoppingToken);
                                                    _logger.LogInformation($"Notified clients of '{affectedComponentType.Name}' page update.");
                                                }
                                                else
                                                {
                                                    _logger.LogInformation($"DB change detected for {changedTableName}. Page {affectedComponentType.Name} is NOT actively prerendered. Skipping re-prerender.");
                                                }
                                            }
                                            else
                                            {
                                                _logger.LogInformation($"DB change detected for {changedTableName}. No Blazor page mapping found for this table. Skipping re-prerender.");
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning($"Could not determine changed table from message body: '{messageBody}'. No specific re-prerender triggered.");
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogInformation("No message received from queue within timeout.");
                                    }
                                }
                            }
                        }
                    }
                    catch (SqlException sqlEx) when (sqlEx.Number == 1222)
                    {
                        _logger.LogWarning($"SQL Timeout (1222) while waiting for messages. Retrying... {sqlEx.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in SQL Service Broker Listener.");
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    }
                }
            }
            _logger.LogInformation("SQL Service Broker Listener stopped.");
        }
    }
}