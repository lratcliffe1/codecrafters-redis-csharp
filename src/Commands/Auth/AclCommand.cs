namespace codecrafters_redis.src.Commands.Auth;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class AclCommand : IRedisCommand
{
  public string Name => "ACL";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'acl'");
    }

    string subcommand = args[1].ToString();

    return subcommand switch
    {
      "WHOAMI" => WhoAmI(args, context),
      "GETUSER" => GetUser(args, context),
      _ => CommandHelper.BuildErrorAsync($"invalid subcommand for 'acl': {subcommand}"),
    };
  }

  private static Task<string> WhoAmI(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'acl'");
    }

    return CommandHelper.FormatBulkAsync("default");
  }

  private static Task<string> GetUser(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'acl getuser'");
    }

    string user = args[2].ToString();

    if (user == "default")
    {
      var flagsArray = CommandHelper.FormatArrayOfResp([]);
      var flags = CommandHelper.FormatArrayOfResp([CommandHelper.FormatBulk("flags"), flagsArray]);

      return Task.FromResult(flags);
    }

    return CommandHelper.FormatBulkAsync("");
  }
}
