using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.EasyCaching;
using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Caching.RabbitMq;
using Grand.Infrastructure.Caching.Redis;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(config =>
{
    config.AddConsole();
});

#region EasyCaching
var dbIndex = 0;
//Console.WriteLine("Db Index:");
//dbIndex = Convert.ToInt32(Console.ReadLine());
services.AddEasyCaching(new EasyCachingOptions
{
    DefaultCacheMinutes = 60,
    EnableHybridMode = false,
    EnableLogging = false,
    EnableMemoryDeepClone = false,
    HybridBusType = HybridBusType.RabbitMQ,
    HybridTopicName = "easycaching.hybrid",
    RedisConnection = new RedisConnection
    {
        Database = dbIndex,
        Nodes = new() { "127.0.0.1:6379" },
        UserName = null,
        Password = null,
    },
    RedisBusConnection = new RedisConnection
    {
        Database = 0,
        Nodes = new() { "127.0.0.1:6379" },
        UserName = null,
        Password = null,
    },
    RabbitConnection = new RabbitConnection
    {
        HostName = "localhost",
        Port = 5672,
        Password = "guest",
        UserName = "guest",
        QueueName = "rmq.queue.easycaching",
        TopicExchangeName = "rmq.exchange.topic.easycaching",
        VirtualHost = "/"
    }
});
#endregion

#region GrandCache
//var pubsubType = HybridBusType.RabbitMQ;
//services.AddSingleton<ICacheBase, MemoryCacheBase>();
//services.AddMemoryCache();
//switch (pubsubType)
//{
//    case HybridBusType.RabbitMQ:
//        services.AddSingleton<ICacheBase, RabbitMqMessageCacheManager>();
//        services.AddMassTransit(massTransitConfigure =>
//        {
//            massTransitConfigure.AddConsumer<CacheMessageEventConsumer>();

//            massTransitConfigure.UsingRabbitMq((context, rabbitMqConfigure) =>
//            {
//                rabbitMqConfigure.Host("localhost", "/", h =>
//                {
//                    h.Password("guest");
//                    h.Username("guest");
//                });

//                rabbitMqConfigure.ConfigureEndpoints(context);
//            });
//        });
//        //for automaticly start/stop bus
//        services.AddMassTransitHostedService();
//        break;
//    case HybridBusType.Redis:
//        var configurationOption = new ConfigurationOptions
//        {
//            EndPoints = { { "127.0.0.1", 6379 } },
//            KeepAlive = 180,
//            Password = null,
//            User = null,
//            //Ssl = true,
//        };
//        var redis = ConnectionMultiplexer.Connect(configurationOption);
//        services.AddSingleton(c => redis.GetSubscriber());
//        services.AddSingleton<IMessageBus, RedisMessageBus>();
//        services.AddSingleton<ICacheBase, RedisMessageCacheManager>();
//        break;
//}
#endregion

var serviceProvider = services.BuildServiceProvider();

var cacheBase = serviceProvider.GetRequiredService<ICacheBase>();


await DoCommandAsync();


async Task DoCommandAsync()
{
    Console.WriteLine("...");
    var command = Console.ReadLine();

    switch (command)
    {
        case "set":
            await cacheBase.SetAsync("testkey", "salam", 10);
            Console.WriteLine("SetAsync");
            break;
        case "get":
            while (true)
            {
                var value = await cacheBase.GetAsync("testkey");
                await Task.Delay(500);
                Console.WriteLine("GetAsync: " + value);
            }
        case "remove":
            await cacheBase.RemoveAsync("testkey");
            Console.WriteLine("RemoveAsync");
            break;
        case "clear":
            await cacheBase.Clear();
            Console.WriteLine("RemoveAsync");
            break;
        default:
            Console.WriteLine("Exit");
            return;
    }

    await DoCommandAsync();
}