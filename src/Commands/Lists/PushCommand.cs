namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public enum PushDirection
{
  None = 0,
  Left = 1,
  Right = 2,
}

public static class PushCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args, PushDirection direction, string commandName)
  {
    if (args.Count < 3)
    {
      return CommandHepler.BuildErrorAsync($"wrong number of arguments for '{commandName}'");
    }

    string? key = args[1].ToString();
    string? val = args[2].ToString();

    List<string> vals = args[2..]
      .Select(value => value.ToString())
      .Where(value => !string.IsNullOrEmpty(value))
      .Select(value => value!)
      .ToList();

    int count = direction == PushDirection.Left
      ? Cache.Prepend(key, vals)
      : Cache.Append(key, vals);

    return CommandHepler.FormatIntegerAsync(count);
  }
}
