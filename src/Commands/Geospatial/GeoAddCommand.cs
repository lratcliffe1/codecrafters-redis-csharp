namespace codecrafters_redis.src.Commands.Geospatial;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class GeoAddCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "GEOADD";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 5)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'geoadd'");
    }

    string key = args[1].ToString();
    string longitude = args[2].ToString();
    string latitude = args[3].ToString();
    string member = args[4].ToString();

    if (!double.TryParse(longitude, out double longitudeValue) || !double.TryParse(latitude, out double latitudeValue))
    {
      return CommandHelper.BuildErrorAsync("invalid longitude or latitude for 'geoadd'");
    }

    int added = cacheStore.GeoAdd(key, longitudeValue, latitudeValue, member);

    return CommandHelper.FormatIntegerAsync(added);
  }
}