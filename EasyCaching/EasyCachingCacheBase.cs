using EasyCaching.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Infrastructure.Caching.EasyCaching
{
    public class EasyCachingCacheBase : ICacheBase
    {
        private readonly int _defaultCacheMinutes;
        private readonly bool _enableHybridMode;
        private readonly IEasyCachingProviderBase _cachingProvider;

        public EasyCachingCacheBase(EasyCachingOptions options, IServiceProvider serviceProvider)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            _defaultCacheMinutes = options.DefaultCacheMinutes;
            _enableHybridMode = options.EnableHybridMode;
            _cachingProvider = options.EnableHybridMode
                ? serviceProvider.GetRequiredService<IHybridCachingProvider>()
                : serviceProvider.GetRequiredService<IEasyCachingProvider>();
        }

        public async Task<string> GetAsync(string key)
        {
            var cacheValue = await _cachingProvider.GetAsync<string>(key);
            return cacheValue.Value;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire)
        {
            var timeSpan = TimeSpan.FromMinutes(_defaultCacheMinutes);
            var cacheValue = await _cachingProvider.GetAsync(key, acquire, timeSpan);
            return cacheValue.Value;
        }

        public Task<bool> TryGetCache(string key, out string value)
        {
            var cacheValue = _cachingProvider.Get<string>(key);
            value = cacheValue.Value;
            return Task.FromResult(cacheValue.HasValue);
        }

        public Task<bool> TryGetCache<T>(string key, out T value)
        {
            var cacheValue = _cachingProvider.Get<T>(key);
            value = cacheValue.Value;
            return Task.FromResult(cacheValue.HasValue);
        }

        public Task SetAsync(string key, object data, int cacheTime)
        {
            var timeSpan = TimeSpan.FromMinutes(_defaultCacheMinutes);
            return _cachingProvider.SetAsync(key, data, timeSpan);
        }

        public Task RemoveAsync(string key, bool publisher = true)
        {
            return _cachingProvider.RemoveAsync(key);
        }

        public Task RemoveByPrefix(string prefix, bool publisher = true)
        {
            return _cachingProvider.RemoveByPrefixAsync(prefix);
        }


        public Task Clear(bool publisher = true)
        {
            if (_enableHybridMode)
                throw new NotSupportedException("Clear is not supported in hybrid mode.");

            return ((IEasyCachingProvider)_cachingProvider).FlushAsync();

            #region Workaround
            //private static readonly FieldInfo _serversField = typeof(DefaultRedisCachingProvider).GetField("_servers", BindingFlags.Instance | BindingFlags.NonPublic);

            //var prefix = "*";

            ////HybridCachingProvider
            //_cachingProvider.RemoveByPrefix(prefix);
            ////DefaultRedisCachingProvider //RedisKey[] SearchRedisKeys(string pattern)
            //var servers = (IEnumerable<IServer>)_serversField.GetValue(_redisCachingProvider);

            //var allCount = 0;
            //foreach (var server in servers)
            //{
            //    //KYES command
            //    var result1 = await server.ExecuteAsync("keys", prefix);
            //    var count1 = ((RedisValue[])result1).Length;
            //    //SCAN command
            //    var result2 = server.Execute("SCAN", 0, "MATCH", prefix, "COUNT", 2147483647);
            //    var count2 = ((RedisValue[])((RedisResult[])result2)[1]).Length;
            //    //SCAN using Lua scripting
            //    var result3 = server.Execute("eval", $"return #redis.call('SCAN', 0, 'MATCH', '{prefix}', 'COUNT', 2147483647)", 0);

            //    //KEYS using Lua scripting
            //    var result4 = server.Execute("eval", $"return #redis.call('KEYS', '{prefix}')", 0);
            //    allCount += (int)result4;

            //    //redis-cli --scan --pattern '*'

            //    var result5 = _redisCachingProvider.SearchKeys(prefix, 250);
            //}
            #endregion
        }
    }
}
