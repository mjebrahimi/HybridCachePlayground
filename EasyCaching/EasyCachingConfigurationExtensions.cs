using EasyCaching.Core.Configurations;
using MessagePack.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Infrastructure.Caching.EasyCaching
{
    public static class EasyCachingConfigurationExtensions
    {
        private const string _serializerName = "msgpack";
        private const string _redisName = "redis";
        private const string _inMemoryName = "memory";
        private const string _hybridName = "hybrid";

        /// <summary>
        /// Add and configure easy caching
        /// </summary>
        /// <param name="services">services</param>
        /// <param name="options">options</param>
        /// <returns>services</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static IServiceCollection AddEasyCaching(this IServiceCollection services, EasyCachingOptions options)
        {
            #region Validations
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (options is null)
                throw new ArgumentNullException(nameof(services));
            if (options.RedisConnection is null)
                throw new ArgumentNullException(nameof(options.RedisConnection));
            if (options.RedisConnection.Nodes is null || options.RedisConnection.Nodes.Count == 0)
                throw new ArgumentException("Easy caching redis nodes is null or empty", nameof(options.RedisConnection.Nodes));
            if (options.RedisConnection.Nodes.Any(p => string.IsNullOrWhiteSpace(p)))
                throw new ArgumentException("One of easy caching redis nodes is null or empty", nameof(options.RedisConnection.Nodes));

            switch (options.HybridBusType)
            {
                case HybridBusType.RabbitMQ:
                    if (options.RabbitConnection is null)
                        throw new ArgumentNullException(nameof(options.RabbitConnection));
                    if (string.IsNullOrWhiteSpace(options.RabbitConnection.HostName))
                        throw new ArgumentException("EasyCaching rabbitmq bus host is null or empty", nameof(options.RabbitConnection.HostName));
                    break;
                case HybridBusType.Redis:
                    if (options.RedisBusConnection is null)
                        throw new ArgumentNullException(nameof(options.RedisBusConnection));
                    if (options.RedisBusConnection.Nodes is null || options.RedisBusConnection.Nodes.Count == 0)
                        throw new ArgumentException("EasyCaching redis bus nodes is null or empty", nameof(options.RedisBusConnection.Nodes));
                    if (options.RedisBusConnection.Nodes.Any(p => string.IsNullOrWhiteSpace(p)))
                        throw new ArgumentException("One of easy caching redis bus nodes is null or empty", nameof(options.RedisBusConnection.Nodes));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(options.HybridBusType));
            }
            #endregion

            services.AddEasyCaching(easyCachingOptions =>
            {
                #region Config Redis and MessagePack
                //Set binary serializer
                easyCachingOptions.WithMessagePack(config =>
                {
                    //Resolve datetime issue (https://easycaching.readthedocs.io/en/latest/MessagePack/)
                    config.EnableCustomResolver = true;
                    config.CustomResolvers = CompositeResolver.Create(
                        // This can solve DateTime time zone problem
                        NativeDateTimeResolver.Instance,
                        ContractlessStandardResolver.Instance
                    );
                }, _serializerName);

                //Set redis cache
                easyCachingOptions.UseRedis(config =>
                {
                    config.EnableLogging = options.EnableLogging;
                    config.SerializerName = _serializerName;
                    config.DBConfig.Database = options.RedisConnection.Database;
                    config.DBConfig.AllowAdmin = true; //Required for redis FLUSHDB command

                    foreach (var redisNode in options.RedisConnection.Nodes)
                    {
                        var hostPort = redisNode.Split(':');
                        var host = hostPort[0];
                        var port = Convert.ToInt32(hostPort[1]);
                        config.DBConfig.Endpoints.Add(new ServerEndPoint(host, port));
                    }

                    if (options.RedisConnection.UserName is not null)
                        config.DBConfig.Username = options.RedisConnection.UserName;

                    if (options.RedisConnection.Password is not null)
                        config.DBConfig.Password = options.RedisConnection.Password;

                    ////Optional options
                    //config.DBConfig.AsyncTimeout = 5000;
                    //config.DBConfig.SyncTimeout = 5000;
                }, _redisName);
                #endregion

                #region Config Hybrid Mode
                if (options.EnableHybridMode)
                {
                    easyCachingOptions.UseInMemory(config =>
                    {
                        config.EnableLogging = options.EnableLogging;
                        config.DBConfig.EnableReadDeepClone = options.EnableMemoryDeepClone;
                        config.DBConfig.EnableWriteDeepClone = options.EnableMemoryDeepClone;

                        ////Optional options
                        //// scan time, default value is 60s
                        //config.DBConfig.ExpirationScanFrequency = 60;
                        //// total count of cache items, default value is 10000
                        //config.DBConfig.SizeLimit = 100;
                        //// the max random second will be added to cache's expiration, default value is 120
                        //config.MaxRdSecond = 120;
                        //// mutex key's alive time(ms), default is 5000
                        //config.LockMs = 5000;
                        //// when mutex key alive, it will sleep some time, default is 300
                        //config.SleepMs = 300;
                    }, _inMemoryName);

                    easyCachingOptions.UseHybrid(config =>
                    {
                        config.TopicName = options.HybridTopicName; //Equals to routing key in rabbitmq

                        config.EnableLogging = options.EnableLogging;
                        config.LocalCacheProviderName = _inMemoryName;
                        config.DistributedCacheProviderName = _redisName;

                        ////Optional options
                        //config.BusRetryCount = 3;
                        //config.DefaultExpirationForTtlFailed = 60;
                    }, _hybridName);

                    switch (options.HybridBusType)
                    {
                        case HybridBusType.RabbitMQ:
                            easyCachingOptions.WithRabbitMQBus(config =>
                            {
                                config.HostName = options.RabbitConnection.HostName;
                                config.Port = Convert.ToInt32(options.RabbitConnection.Port);
                                config.UserName = options.RabbitConnection.UserName;
                                config.Password = options.RabbitConnection.Password;
                                config.VirtualHost = options.RabbitConnection.VirtualHost;
                                config.TopicExchangeName = options.RabbitConnection.TopicExchangeName;
                                config.QueueName = $"{options.RabbitConnection.QueueName}.{Guid.NewGuid():N}";

                                //Optional options
                                //config.SocketWriteTimeout = 30000;
                                //config.SocketReadTimeout = 30000;
                                //config.RequestedConnectionTimeout = 30000;
                            });
                            break;

                        case HybridBusType.Redis:
                            easyCachingOptions.WithRedisBus(config =>
                            {
                                // do not forget to set the SerializerName for the bus here !!
                                config.SerializerName = _serializerName;
                                config.Database = options.RedisBusConnection.Database;

                                foreach (var redisNode in options.RedisBusConnection.Nodes)
                                {
                                    var hostPort = redisNode.Split(':');
                                    var host = hostPort[0];
                                    var port = Convert.ToInt32(hostPort[1]);
                                    config.Endpoints.Add(new ServerEndPoint(host, port));
                                }

                                if (options.RedisBusConnection.UserName is not null)
                                    config.Username = options.RedisBusConnection.UserName;

                                if (options.RedisBusConnection.Password is not null)
                                    config.Password = options.RedisBusConnection.Password;
                            });
                            break;
                    }
                }
                #endregion
            });

            services.AddSingleton(options);
            services.AddSingleton<ICacheBase, EasyCachingCacheBase>();

            return services;
        }
    }
}
