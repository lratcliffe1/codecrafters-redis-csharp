namespace codecrafters_redis.src.Commands.Multi;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class ExecCommand
{
  public static async Task<string> ProcessAsync(List<RespValue> args, long clientId, CancellationToken cancellationToken)
  {
    List<string> results = [];
    foreach (RespValue arg in args)
    {
      string result = await RespExecutor.ExecuteAsync(arg, clientId, cancellationToken);
      results.Add(result);
    }

    return CommandHepler.FormatArrayOfResp(results);
  }
}