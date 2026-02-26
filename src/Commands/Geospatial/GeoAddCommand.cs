namespace codecrafters_redis.src.Commands.Geospatial;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class GeoAddCommand(ICacheStore cacheStore) : IRedisCommand
{
  private readonly double minLongitude = -180;
  private readonly double maxLongitude = 180;
  private readonly double minLatitude = -85.05112878;
  private readonly double maxLatitude = 85.05112878;

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

    if (longitudeValue < minLongitude || longitudeValue > maxLongitude || latitudeValue < minLatitude || latitudeValue > maxLatitude)
    {
      return CommandHelper.BuildErrorAsync($"ERR invalid longitude, latitude pair {longitudeValue},{latitudeValue} is not a valid geospatial key");
    }

    var encodedLatLon = GeohashEncoder.Encode(latitudeValue, longitudeValue);

    int added = cacheStore.ZAdd(key, encodedLatLon, member);

    return CommandHelper.FormatIntegerAsync(added);
  }
}