namespace Grand.Infrastructure.Caching
{
    /// <summary>
    /// Cache manager interface
    /// </summary>
    public interface ICacheBase
    {
        Task<T> GetAsync<T>(string key, Func<Task<T>> acquire);
        Task<string> GetAsync(string key);
        Task<bool> TryGetCache(string key, out string value);
        Task<bool> TryGetCache<T>(string key, out T value);
        Task SetAsync(string key, object data, int cacheTime);
        Task RemoveAsync(string key, bool publisher = true);
        Task RemoveByPrefix(string prefix, bool publisher = true);
        Task Clear(bool publisher = true);
    }
}
