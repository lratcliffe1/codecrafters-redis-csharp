namespace codecrafters_redis.src.Commands.PubSub;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class UnsubscribeCommand(IPubSubStore pubSubStore) : IRedisCommand
{
  public string Name => "UNSUBSCRIBE";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'unsubscribe'");
    }

    string channel = args[1].ToString();

    var count = pubSubStore.Unsubscribe(context.ClientId, channel);

    return CommandHelper.FormatArrayAsync(["unsubscribe", channel, count]);
  }
}