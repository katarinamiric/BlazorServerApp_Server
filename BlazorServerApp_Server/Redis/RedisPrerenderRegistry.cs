using Microsoft.AspNetCore.Components;
using StackExchange.Redis;

namespace BlazorServerApp_Server.Redis
{
    public class RedisPrerenderRegistry
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisPrerenderRegistry> _logger;

        private const string ActivePrerenderedPagesRedisKey = "prerender:activepages";

        private static readonly Dictionary<string, Type> _tableToComponentMapping =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "Weather", typeof(BlazorServerApp_Server.Components.Pages.Weather) },
                { "Report", typeof(BlazorServerApp_Server.Components.Pages.ReportPrerendered) },
            };

        public RedisPrerenderRegistry(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisPrerenderRegistry> logger)
        {
            _redisDb = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task RegisterPageForPrerenderingAsync(Type componentType)
        {
            if (componentType == null || !typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException("Provided type must be a Blazor component.", nameof(componentType));
            }

            string typeName = componentType.AssemblyQualifiedName!;

            try
            {
                bool added = await _redisDb.SetAddAsync(ActivePrerenderedPagesRedisKey, typeName);
                if (added)
                {
                    _logger.LogInformation($"[RedisPrerenderRegistry] Registered: {componentType.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[RedisPrerenderRegistry] Error registering {componentType.Name}.");
            }
        }

        public async Task UnregisterPageForPrerenderingAsync(Type componentType)
        {
            string typeName = componentType.AssemblyQualifiedName!;
            try
            {
                bool removed = await _redisDb.SetRemoveAsync(ActivePrerenderedPagesRedisKey, typeName);
                if (removed)
                {
                    _logger.LogInformation($"[RedisPrerenderRegistry] Unregistered: {componentType.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[RedisPrerenderRegistry] Error unregistering {componentType.Name}.");
            }
        }

        public async Task<bool> IsPageActivelyPrerenderedAsync(Type componentType)
        {
            string typeName = componentType.AssemblyQualifiedName!;
            try
            {
                return await _redisDb.SetContainsAsync(ActivePrerenderedPagesRedisKey, typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[RedisPrerenderRegistry] Error checking if {componentType.Name} is actively prerendered.");
                return false; 
            }
        }

        public async Task<IEnumerable<Type>> GetActivePrerenderedPagesAsync()
        {
            try
            {
                RedisValue[] members = await _redisDb.SetMembersAsync(ActivePrerenderedPagesRedisKey);
                var activeTypes = new List<Type>();
                foreach (var member in members)
                {
                    if (!member.IsNullOrEmpty)
                    {
                        try
                        {
                            Type? type = Type.GetType(member.ToString());
                            if (type != null)
                            {
                                activeTypes.Add(type);
                            }
                            else
                            {
                                _logger.LogWarning($"[RedisPrerenderRegistry] Could not load type from Redis: {member}");
                            }
                        }
                        catch (Exception typeEx)
                        {
                            _logger.LogError(typeEx, $"[RedisPrerenderRegistry] Error getting type from string '{member}'.");
                        }
                    }
                }
                return activeTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisPrerenderRegistry] Error getting active prerendered pages.");
                return Enumerable.Empty<Type>();
            }
        }

        public Type? GetComponentTypeForTable(string tableName)
        {
            _tableToComponentMapping.TryGetValue(tableName, out var componentType);
            return componentType;
        }
    }
}
