using Microsoft.Extensions.Caching.Memory;

namespace KhairAPI.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, int size = 1);
        
        T? Get<T>(string key);
        
        void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, int size = 1);
        
        void Remove(string key);
        
        void RemoveByPrefix(string prefix);
    }
}
