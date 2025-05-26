using Microsoft.Extensions.Caching.Memory;

namespace BlazorServerApp_Server
{
    public class HtmlCache
    {
        private readonly Dictionary<string, string> _cache = new();
        private readonly Dictionary<string, string?[]> _data = new();
        private readonly IMemoryCache _memoryCache;

        public HtmlCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void Set(string key, string html)
        {
            _cache[key] = html;
        }
        public void Set<TItem>(object key, TItem value, MemoryCacheEntryOptions? options = null)
        {
            _memoryCache.Set(key, value, options);
        }
        public bool TryGetValue<TItem>(object key, out TItem? value)
        {
            return _memoryCache.TryGetValue(key, out value);
        }
        public string? Get(string key)
        {
            _cache.TryGetValue(key, out var html);
            return html;
        }
        
        public void SetData(string key, string?[] html)
        {
            _data[key] = html;
        }
        public void ClearData(string key)
        {
            _data[key] = null;
        }
        public string?[] GetData(string key)
        {
            _data.TryGetValue(key, out string?[] html);
            return html;
        }
    }

}
