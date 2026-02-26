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

    if (!GeoHelper.TryParseDouble(longitude, out double longitudeValue)
      || !GeoHelper.TryParseDouble(latitude, out double latitudeValue))
    {
      return CommandHelper.BuildErrorAsync("invalid longitude or latitude for 'geoadd'");
    }

    if (!GeoHelper.IsValidCoordinatePair(longitudeValue, latitudeValue))
    {
      return CommandHelper.BuildErrorAsync(GeoHelper.BuildInvalidCoordinatePairError(longitudeValue, latitudeValue));
    }

    var encodedLatLon = GeohashEncoder.Encode(latitudeValue, longitudeValue);

    int added = cacheStore.ZAdd(key, encodedLatLon, member);

    return CommandHelper.FormatIntegerAsync(added);
  }
}
