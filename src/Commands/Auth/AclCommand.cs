namespace codecrafters_redis.src.Commands.Auth;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class AclCommand(IAclUserStore aclUserStore, IClientAuthStore clientAuthStore) : IRedisCommand
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
      "GETUSER" => GetUser(args),
      "SETUSER" => SetUser(args),
      _ => CommandHelper.BuildErrorAsync($"invalid subcommand for 'acl': {subcommand}"),
    };
  }

  private Task<string> WhoAmI(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'acl'");
    }

    if (!aclUserStore.TryGetUser("default", out AclUser aclUser))
    {
      return CommandHelper.BuildErrorAsync($"user 'default' not found");
    }

    if (aclUser.HasNoPassword)
    {
      clientAuthStore.Authenticate(context.ClientId, aclUser.Name);
      return CommandHelper.FormatBulkAsync(aclUser.Name);
    }

    if (!clientAuthStore.TryGetUsername(context.ClientId, out string username))
    {
      return CommandHelper.BuildNamedErrorAsync("NOAUTH", "Authentication required.");
    }

    return CommandHelper.FormatBulkAsync(username);
  }

  private Task<string> GetUser(List<RespValue> args)
  {
    if (args.Count != 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'acl getuser'");
    }

    string user = args[2].ToString();

    if (aclUserStore.TryGetUser(user, out AclUser aclUser))
    {
      List<string> flagItems = aclUser.HasNoPassword
        ? [CommandHelper.FormatBulk("nopass")]
        : [];

      List<string> passwords = aclUser.Passwords
        .Select(CommandHelper.FormatBulk)
        .ToList();

      string flagsArray = CommandHelper.FormatArrayOfResp(flagItems);
      string passwordsArray = CommandHelper.FormatArrayOfResp(passwords);

      string userInfoArray = CommandHelper.FormatArrayOfResp([
        CommandHelper.FormatBulk("flags"), flagsArray,
        CommandHelper.FormatBulk("passwords"), passwordsArray]);

      return Task.FromResult(userInfoArray);
    }

    return CommandHelper.FormatNullAsync(RespType.BulkString);
  }

  private Task<string> SetUser(List<RespValue> args)
  {
    if (args.Count < 4)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'acl setuser'");
    }

    string user = args[2].ToString();

    for (int i = 3; i < args.Count; i++)
    {
      string rule = args[i].ToString();

      if (rule.StartsWith('>'))
      {
        string password = rule[1..];
        aclUserStore.AddPassword(user, password);
      }
    }

    return CommandHelper.FormatSimpleAsync("OK");
  }
}
