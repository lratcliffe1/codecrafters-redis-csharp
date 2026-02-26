namespace codecrafters_redis.src.Commands.Geospatial;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class GeoSearchCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "GEOSEARCH";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 8)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'geosearch'");
    }

    string key = args[1].ToString();
    string fromType = args[2].ToString();
    string searchType = args[5].ToString();
    if (!string.Equals(fromType, "FROMLONLAT", StringComparison.OrdinalIgnoreCase))
    {
      return CommandHelper.BuildErrorAsync("invalid from type for 'geosearch'");
    }

    if (!string.Equals(searchType, "BYRADIUS", StringComparison.OrdinalIgnoreCase))
    {
      return CommandHelper.BuildErrorAsync("invalid search type for 'geosearch'");
    }

    string longitude = args[3].ToString();
    string latitude = args[4].ToString();
    string radius = args[6].ToString();
    string unit = args[7].ToString();

    if (!GeoHelper.TryParseDouble(longitude, out double longitudeValue)
      || !GeoHelper.TryParseDouble(latitude, out double latitudeValue))
    {
      return CommandHelper.BuildErrorAsync("invalid longitude or latitude for 'geosearch'");
    }

    if (!GeoHelper.IsValidCoordinatePair(longitudeValue, latitudeValue))
    {
      return CommandHelper.BuildErrorAsync(GeoHelper.BuildInvalidCoordinatePairError(longitudeValue, latitudeValue));
    }

    if (!GeoHelper.TryParseDouble(radius, out double radiusValue) || radiusValue < 0)
    {
      return CommandHelper.BuildErrorAsync("invalid radius for 'geosearch'");
    }

    if (!TryGetUnitMultiplier(unit, out double unitMultiplier))
    {
      return CommandHelper.BuildErrorAsync("invalid radius for 'geosearch'");
    }

    double radiusInMeters = radiusValue * unitMultiplier;
    var members = FindMembersInRadius(key, latitudeValue, longitudeValue, radiusInMeters);

    return CommandHelper.FormatArrayAsync(members.Select(entry => entry.Member).ToList());
  }

  private List<ZSetEntry> FindMembersInRadius(string key, double centerLatitude, double centerLongitude, double radiusInMeters)
  {
    List<ZSetEntry> entries = cacheStore.ZRange(key, 0, -1);
    List<ZSetEntry> result = [];
    foreach (ZSetEntry entry in entries)
    {
      var (latitude, longitude) = GeohashDecoder.Decode((long)entry.Score);
      double distance = GeoHelper.CalculateDistanceUsingHaversineFormula(centerLatitude, centerLongitude, latitude, longitude);
      if (distance <= radiusInMeters)
      {
        result.Add(entry);
      }
    }

    return result;
  }

  private static bool TryGetUnitMultiplier(string unit, out double multiplier)
  {
    switch (unit.ToLowerInvariant())
    {
      case "m":
        multiplier = 1;
        return true;
      case "km":
        multiplier = 1000;
        return true;
      case "ft":
        multiplier = 0.3048;
        return true;
      case "mi":
        multiplier = 1609.34;
        return true;
      default:
        multiplier = 0;
        return false;
    }
  }
}
