namespace codecrafters_redis.src.Commands.Auth;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class AclCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "ACL";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'acl'");
    }

    string subcommand = args[1].ToString();

    return subcommand switch
    {
      "WHOAMI" => WhoAmI(args, context),
      _ => CommandHelper.BuildErrorAsync($"invalid subcommand for 'acl': {subcommand}"),
    };
  }

  private static Task<string> WhoAmI(List<RespValue> args, CommandExecutionContext context)
  {
    return CommandHelper.FormatBulkAsync("default");
  }
}