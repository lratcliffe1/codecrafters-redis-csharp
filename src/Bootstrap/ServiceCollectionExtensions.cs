using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Commands.General;
using codecrafters_redis.src.Commands.Lists;
using codecrafters_redis.src.Commands.Multi;
using codecrafters_redis.src.Commands.Replication;
using codecrafters_redis.src.Commands.Streams;
using codecrafters_redis.src.Commands.Strings;
using codecrafters_redis.src.Hosting;
using codecrafters_redis.src.Resp;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.src.Bootstrap;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddRedisServer(this IServiceCollection services, string[] args)
  {
    ParseAndAddServerOptions(services, args);

    AddStores(services);

    AddKeyedCommands(services);

    AddEventLoop(services);

    return services;
  }

  private static void ParseAndAddServerOptions(IServiceCollection services, string[] args)
  {
    ServerOptionsParser serverOptionsParser = new();
    ServerOptions options = serverOptionsParser.Parse(args);

    services.AddSingleton(options);
    services.AddSingleton(serverOptionsParser);
  }

  private static void AddStores(IServiceCollection services)
  {
    services.AddSingleton<IReplicaStore, ReplicaStore>();
    services.AddSingleton<IClientMultiStore, ClientMultiStore>();
    services.AddSingleton<ICacheStore, Cache.Cache>();
  }

  private static void AddKeyedCommands(IServiceCollection services)
  {
    services.AddKeyedSingleton<IRedisCommand, EchoCommand>("ECHO");
    services.AddKeyedSingleton<IRedisCommand, PingCommand>("PING");
    services.AddKeyedSingleton<IRedisCommand, TypeCommand>("TYPE");

    services.AddKeyedSingleton<IRedisCommand, BLPopCommand>("BLPOP");
    services.AddKeyedSingleton<IRedisCommand, LLenCommand>("LLEN");
    services.AddKeyedSingleton<IRedisCommand, LPopCommand>("LPOP");
    services.AddKeyedSingleton<IRedisCommand, LPushCommand>("LPUSH");
    services.AddKeyedSingleton<IRedisCommand, LRangeCommand>("LRANGE");
    services.AddKeyedSingleton<IRedisCommand, RPushCommand>("RPUSH");

    services.AddKeyedSingleton<IRedisCommand, MultiCommand>("MULTI");
    services.AddKeyedSingleton<IRedisCommand, DiscardCommand>("DISCARD");

    services.AddKeyedSingleton<IRedisCommand, InfoCommand>("INFO");
    services.AddKeyedSingleton<IRedisCommand, ReplconfCommand>("REPLCONF");

    services.AddKeyedSingleton<IRedisCommand, XAddCommand>("XADD");
    services.AddKeyedSingleton<IRedisCommand, XRangeCommand>("XRANGE");
    services.AddKeyedSingleton<IRedisCommand, XReadCommand>("XREAD");

    services.AddKeyedSingleton<IRedisCommand, GetCommand>("GET");
    services.AddKeyedSingleton<IRedisCommand, IncrCommand>("INCR");
    services.AddKeyedSingleton<IRedisCommand, SetCommand>("SET");
  }

  private static void AddEventLoop(IServiceCollection services)
  {
    services.AddSingleton<IRedisServerHost, RedisServerHost>();
    services.AddSingleton<IClientIdAllocator, ClientIdAllocator>();
    services.AddSingleton<IHandshakeCoordinator, HandshakeCoordinator>();
    services.AddSingleton<IRespParser, RespParser>();
    services.AddSingleton<ICommandEventLoop, CommandEventLoop>();
    services.AddSingleton<IRespExecutor, RespExecutor>();
  }
}
