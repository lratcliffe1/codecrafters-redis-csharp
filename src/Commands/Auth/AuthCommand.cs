namespace codecrafters_redis.src.Commands.Auth;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Cache;

public class AuthCommand(IAclUserStore aclUserStore) : IRedisCommand
{
  public string Name => "AUTH";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count is not (2 or 3))
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'auth'");
    }

    string user = args.Count == 2 ? "default" : args[1].ToString();
    string password = args.Count == 2 ? args[1].ToString() : args[2].ToString();

    if (!aclUserStore.VerifyPassword(user, password))
    {
      return CommandHelper.BuildNamedErrorAsync("WRONGPASS", "invalid username-password pair or user is disabled.");
    }

    return CommandHelper.FormatSimpleAsync("OK");
  }
}
