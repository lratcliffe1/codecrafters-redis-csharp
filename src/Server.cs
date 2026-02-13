using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Hosting;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
services.AddRedisServer(args);

await using ServiceProvider serviceProvider = services.BuildServiceProvider();
IRedisServerHost serverHost = serviceProvider.GetRequiredService<IRedisServerHost>();
await serverHost.RunAsync();
