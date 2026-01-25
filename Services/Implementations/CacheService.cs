using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, byte> _keys = new();

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, int size = 1)
        {
            if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            {
                return cached;
            }

            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                if (_cache.TryGetValue(key, out cached) && cached is not null)
                {
                    return cached;
                }

                var value = await factory();

                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(5),
                    Size = size,
                    Priority = CacheItemPriority.Normal
                };

                options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
                {
                    _keys.TryRemove(evictedKey.ToString()!, out _);
                });

                _cache.Set(key, value, options);
                _keys.TryAdd(key, 0);

                return value;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public T? Get<T>(string key)
        {
            return _cache.TryGetValue(key, out T? value) ? value : default;
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, int size = 1)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(5),
                Size = size,
                Priority = CacheItemPriority.Normal
            };

            options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
            {
                _keys.TryRemove(evictedKey.ToString()!, out _);
            });

            _cache.Set(key, value, options);
            _keys.TryAdd(key, 0);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        public void RemoveByPrefix(string prefix)
        {
            var keysToRemove = _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }
        }
    }
}
