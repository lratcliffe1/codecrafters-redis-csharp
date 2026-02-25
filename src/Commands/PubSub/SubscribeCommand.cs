namespace codecrafters_redis.src.Commands.PubSub;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class SubscribeCommand(IPubSubStore pubSubStore) : IRedisCommand
{
  public string Name => "SUBSCRIBE";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'subscribe'");
    }

    string channel = args[1].ToString();

    var count = pubSubStore.Subscribe(context.ClientId, channel);

    return CommandHelper.FormatArrayAsync(["subscribe", channel, count]);
  }
}
