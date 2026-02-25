namespace codecrafters_redis.src.Commands.PubSub;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class PublishCommand(IPubSubStore pubSubStore) : IRedisCommand
{
  public string Name => "PUBLISH";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'publish'");
    }

    string channel = args[1].ToString();
    string message = args[2].ToString();

    var count = pubSubStore.Publish(channel, message);

    return CommandHelper.FormatIntegerAsync(count);
  }
}