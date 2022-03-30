namespace Grand.Infrastructure.Caching.EasyCaching
{
    public class EasyCachingOptions
    {
        /// <summary>
        /// Get or set default cache expiration time in minutes. Default is 60
        /// </summary>
        public int DefaultCacheMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets a value indicating whether enable logging. Default is false
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable deep clone when reading/writing object to cache.
        /// </summary>
        public bool EnableMemoryDeepClone { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether enable hybrid mode. Default is true
        /// </summary>
        public bool EnableHybridMode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating hybrid bus mode. Default is HybridBusMode.RabbitMQ
        /// </summary>
        public HybridBusType HybridBusType { get; set; } = HybridBusType.RabbitMQ;

        /// <summary>
        /// Gets or sets a value indicating [Topic] in redis pubsub or [RoutingKey] in rabbitmq
        /// </summary>
        public string HybridTopicName { get; set; } = "easycaching.hybrid";

        /// <summary>
        /// Gets or sets redis connection
        /// </summary>
        public RedisConnection RedisConnection { get; set; } = new RedisConnection();

        /// <summary>
        /// Gets or sets redis bus connection
        /// </summary>
        public RedisConnection RedisBusConnection { get; set; }

        /// <summary>
        /// Gets or sets rabbitmq bus connection
        /// </summary>
        public RabbitConnection RabbitConnection { get; set; }

        //SerializerType: MessagePack, Protobuf, TextJson, JsonNet
        //CompressorType: Disabled, Brotli, GZip, Deflate, LZ4, Zstd
    }

    public enum HybridBusType
    {
        RabbitMQ,
        Redis,
    }

    public class RedisConnection
    {
        /// <summary>
        /// Get or set redis host. Default is new[] { "127.0.0.1:6379" }
        /// </summary>
        public List<string> Nodes { get; set; } = new() { "127.0.0.1:6379" };

        /// <summary>
        /// Gets or sets the username to be used to connect to the Redis server.
        /// </summary>
        public string UserName { get; set; } = null;

        /// <summary>
        /// Gets or sets the password to be used to connect to the Redis server.
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// Gets or sets the Redis database index. Default is 0.
        /// </summary>
        public int Database { get; set; } = 0;
    }

    public class RabbitConnection
    {
        /// <summary>
        /// Get or set rabbitmq host. Default is "localhost"
        /// </summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// Get or set rabbitmq username. Default is "guest"
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// Get or set rabbitmq password. Default is "guest"
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Get or set rabbitmq port. Default is 5672
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// Get or set rabbitmq virtual host. Default is "/"
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Topic exchange name when declare a topic exchange. Default is "rmq.exchange.topic.easycaching".
        /// </summary>
        public string TopicExchangeName { get; set; } = "rmq.exchange.topic.easycaching";

        /// <summary>
        /// Gets or sets the name of the queue. Default is "rmq.queue.easycaching".
        /// </summary>
        public string QueueName { get; set; } = "rmq.queue.easycaching";
    }
}
