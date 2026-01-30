namespace codecrafters_redis.src.Commands;

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
  public static string Process(List<RespValue> args, PushDirection direction, string commandName)
  {
    if (args.Count < 3)
    {
      return CommandHepler.BuildError($"wrong number of arguments for '{commandName}'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);
    string? val = CommandHepler.ReadBulkOrSimple(args[2]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError($"invalid key for '{commandName}'");
    }
    if (string.IsNullOrEmpty(val))
    {
      return CommandHepler.BuildError($"invalid value for '{commandName}'");
    }

    List<string> vals = args[2..]
      .Select(CommandHepler.ReadBulkOrSimple)
      .Where(value => !string.IsNullOrEmpty(value))
      .Select(value => value!)
      .ToList();

    int count = direction == PushDirection.Left
      ? Cache.Prepend(key, vals)
      : Cache.Append(key, vals);

    return $":{count}\r\n";
  }
}
