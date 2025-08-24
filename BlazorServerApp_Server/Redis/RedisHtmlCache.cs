using System.Text.Json;
using StackExchange.Redis;

namespace BlazorServerApp_Server.Redis
{
    public class RedisHtmlCache
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisHtmlCache> _logger;

        public RedisHtmlCache(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisHtmlCache> logger)
        {
            _redisDb = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task SetHtmlAsync(string key, string html)
        {
            try
            {
                await _redisDb.StringSetAsync($"html:{key}", html);
                _logger.LogInformation($"Cached HTML for key: html:{key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting HTML for key: html:{key}");
            }
        }

        public async Task<string> GetHtmlAsync(string key)
        {
            try
            {
                RedisValue html = await _redisDb.StringGetAsync($"html:{key}");
                    if (html.IsNullOrEmpty)
                {
                    _logger.LogInformation($"HTML cache null for key: html:{key}");
                    return null;
                }
                _logger.LogInformation($"HTML cache for key: html:{key}");
                return html.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting HTML for key: html:{key}");
                return null;
            }
        }

        public async Task SetDataAsync<TItem>(string key, TItem value, TimeSpan? expiry = null)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                    WriteIndented = false
                };
                var json = JsonSerializer.Serialize(value, options);
                await _redisDb.StringSetAsync($"data:{key}", json, expiry);
                _logger.LogInformation($"Cached data for key: data:{key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting data for key: data:{key}");
            }
        }

        public async Task<TItem?> TryGetDataAsync<TItem>(string key)
        {
            try
            {
                RedisValue json = await _redisDb.StringGetAsync($"data:{key}");
                if (json.IsNullOrEmpty)
                {
                    _logger.LogInformation($"Data cache miss for key: data:{key}");
                    return default;
                }
                _logger.LogInformation($"Data cache hit for key: data:{key}");
                return JsonSerializer.Deserialize<TItem>(json!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting data for key: data:{key}");
                return default;
            }
        }
        public async Task ClearHtmlAsync(string key)
        {
            try
            {
                await _redisDb.KeyDeleteAsync($"html:{key}");
                _logger.LogInformation($"Cleared HTML for key: html:{key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clearing HTML for key: html:{key}");
            }
        }

        public async Task SetStringArrayDataAsync(string key, string?[] dataArray)
        {
            try
            {
                var json = JsonSerializer.Serialize(dataArray);
                await _redisDb.StringSetAsync($"stringarray:{key}", json);
                _logger.LogInformation($"Cached string array for key: stringarray:{key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting string array for key: stringarray:{key}");
            }
        }

        public async Task<string?[]?> GetStringArrayDataAsync(string key)
        {
            try
            {
                RedisValue json = await _redisDb.StringGetAsync($"stringarray:{key}");
                if (json.IsNullOrEmpty)
                {
                    _logger.LogInformation($"String array cache miss for key: stringarray:{key}");
                    return null;
                }
                _logger.LogInformation($"String array cache hit for key: stringarray:{key}");
                return JsonSerializer.Deserialize<string?[]>(json!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting string array for key: stringarray:{key}");
                return null;
            }
        }

        public async Task ClearStringArrayDataAsync(string key)
        {
            try
            {
                await _redisDb.KeyDeleteAsync($"stringarray:{key}");
                _logger.LogInformation($"Cleared string array data for key: stringarray:{key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clearing string array data for key: stringarray:{key}");
            }
        }
    }
}
