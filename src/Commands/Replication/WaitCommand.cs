using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Replication;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public sealed class WaitCommand(IReplicaConnectionRegistry replicaConnectionRegistry) : IRedisCommand
{
  public string Name => "WAIT";
  public async Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 3)
    {
      return CommandHelper.BuildError("wrong number of arguments for 'wait' command");
    }

    if (!int.TryParse(args[1].ToString(), out int replicas))
    {
      return CommandHelper.BuildError("invalid replicas for 'wait' command");
    }

    if (!int.TryParse(args[2].ToString(), out int timeout))
    {
      return CommandHelper.BuildError("invalid timeout for 'wait' command");
    }

    if (timeout < 0)
    {
      return CommandHelper.BuildError("timeout must be greater or equal to 0 for 'wait' command");
    }

    if (replicas < 0)
    {
      return CommandHelper.BuildError("replicas must be greater or equal to 0 for 'wait' command");
    }

    long targetOffset = replicaConnectionRegistry.GetReplicationOffset();

    if (replicaConnectionRegistry.TryMarkWaitOffset(targetOffset))
    {
      await replicaConnectionRegistry.RequestAcknowledgementsAsync(context.CancellationToken);
    }

    int acknowledgedReplicas = await replicaConnectionRegistry.WaitForReplicasAsync(
      replicas,
      targetOffset,
      TimeSpan.FromMilliseconds(timeout),
      context.CancellationToken);

    return CommandHelper.FormatInteger(acknowledgedReplicas);
  }
}
