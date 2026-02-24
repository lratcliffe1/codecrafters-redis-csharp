using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Persistance;

public sealed class ConfigCommand(IServerOptions serverOptions) : IRedisCommand
{
  public string Name => "CONFIG";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'config'");
    }

    if (!string.Equals(args[1].ToString(), "GET", StringComparison.OrdinalIgnoreCase))
    {
      return CommandHelper.BuildErrorAsync("invalid argument for 'config'");
    }

    List<string> response = [];
    foreach (RespValue arg in args.Skip(2))
    {
      string optionName = arg.ToString();

      if (string.Equals(optionName, "dir", StringComparison.OrdinalIgnoreCase) && serverOptions.DataDirectory is not null)
      {
        response.Add("dir");
        response.Add(serverOptions.DataDirectory);
      }
      else if (string.Equals(optionName, "dbfilename", StringComparison.OrdinalIgnoreCase) && serverOptions.Dbfilename is not null)
      {
        response.Add("dbfilename");
        response.Add(serverOptions.Dbfilename);
      }
    }

    return CommandHelper.FormatArrayAsync(response);
  }
}
