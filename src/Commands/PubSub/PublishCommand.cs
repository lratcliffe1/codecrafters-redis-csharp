namespace codecrafters_redis.src.Commands.PubSub;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class PublishCommand(IPubSubStore pubSubStore) : IRedisCommand
{
  public string Name => "PUBLISH";
  public async Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 3)
    {
      return CommandHelper.BuildError("wrong number of arguments for 'publish'");
    }

    string channel = args[1].ToString();
    string message = args[2].ToString();

    int count = await pubSubStore.PublishAsync(channel, message, context.CancellationToken);

    return CommandHelper.FormatInteger(count);
  }
}
