using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Grand.Infrastructure.Caching
{
    /// <summary>
    /// Represents a manager for memory caching
    /// </summary>
    public partial class MemoryCacheBase : ICacheBase, IDisposable
    {
        #region Fields

        private readonly IMemoryCache _cache;

        private bool _disposed;
        private static CancellationTokenSource _resetCacheToken = new();

        #endregion

        #region Ctor

        public MemoryCacheBase(IMemoryCache cache)
        {
            _cache = cache;
        }

        #endregion

        #region Methods
        public virtual Task<string> GetAsync(string key)
        {
            var value = _cache.Get(key);
            return Task.FromResult(value?.ToString());
        }

        public virtual Task<bool> TryGetCache(string key, out string value)
        {
            return Task.FromResult(_cache.TryGetValue(key, out value));
        }

        public Task<bool> TryGetCache<T>(string key, out T value)
        {
            return Task.FromResult(_cache.TryGetValue(key, out value));
        }

        public virtual Task<T> GetAsync<T>(string key, Func<Task<T>> acquire)
        {
            //TODO:Use ValueTask instead of Task for performance reason
            return _cache.GetOrCreateAsync(key, entry =>
            {
                entry.SetOptions(GetMemoryCacheEntryOptions(60));
                return acquire();
            });
        }

        public virtual Task SetAsync(string key, object data, int cacheTime)
        {
            if (data != null)
                _cache.Set(key, data, GetMemoryCacheEntryOptions(cacheTime));

            return Task.CompletedTask;
        }

        public virtual Task RemoveAsync(string key, bool publisher = true)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public virtual Task RemoveByPrefix(string prefix, bool publisher = true)
        {
            var keysToRemove = _cache.GetKeys<string>().Where(x => x.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            foreach (var key in keysToRemove)
                _cache.Remove(key);
            return Task.CompletedTask;
        }

        public virtual Task Clear(bool publisher = true)
        {
            //cancel
            _resetCacheToken.Cancel();
            //dispose
            _resetCacheToken.Dispose();

            _resetCacheToken = new CancellationTokenSource();

            return Task.CompletedTask;
        }

        ~MemoryCacheBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cache.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        #region Utilities

        protected MemoryCacheEntryOptions GetMemoryCacheEntryOptions(int cacheTime)
        {
            var options = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTime) }
                .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token))
                .RegisterPostEvictionCallback(PostEvictionCallback);

            return options;
        }

        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            //if (reason == EvictionReason.Replaced)
            //    return;

            //if (reason == EvictionReason.TokenExpired)
            //    return;
        }

        #endregion
    }

    //public static class InMemoryCacheExtensions
    //{
    //    public static async ValueTask<TItem> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
    //    {
    //        if (!cache.TryGetValue(key, out var result))
    //        {
    //            using var entry = cache.CreateEntry(key);
    //            result = await factory(entry).ConfigureAwait(false);
    //            entry.Value = result;
    //        }
    //        return (TItem)result;
    //    }
    //}
}