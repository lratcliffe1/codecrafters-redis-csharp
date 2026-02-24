using System.Globalization;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Replication;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public sealed class ReplconfCommand(IServerOptions serverOptions, IReplicaConnectionRegistry replicaConnectionRegistry) : IRedisCommand
{
  public string Name => "REPLCONF";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count > 1 && string.Equals(args[1].ToString(), "GETACK", StringComparison.OrdinalIgnoreCase))
    {
      var ackBytes = serverOptions.GetAckBytes();
      return Task.FromResult(CommandHelper.FormatArray(["REPLCONF", "ACK", ackBytes.ToString(CultureInfo.InvariantCulture)]));
    }

    if (args.Count > 2
      && string.Equals(args[1].ToString(), "ACK", StringComparison.OrdinalIgnoreCase)
      && long.TryParse(args[2].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long ackOffset))
    {
      replicaConnectionRegistry.UpdateReplicaAckOffset(context.ClientId, ackOffset);
    }

    return Task.FromResult(CommandHelper.FormatSimple("OK"));
  }
}
