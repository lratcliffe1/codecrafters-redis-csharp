using System.Globalization;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public sealed class ReplconfCommand(IServerOptions serverOptions) : IRedisCommand
{
  public string Name => "REPLCONF";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count > 1 && string.Equals(args[1].ToString(), "GETACK", StringComparison.OrdinalIgnoreCase))
    {
      var ackBytes = serverOptions.GetAckBytes();
      return Task.FromResult(CommandHelper.FormatArray(["REPLCONF", "ACK", ackBytes.ToString(CultureInfo.InvariantCulture)]));
    }

    return Task.FromResult(CommandHelper.FormatSimple("OK"));
  }
}
