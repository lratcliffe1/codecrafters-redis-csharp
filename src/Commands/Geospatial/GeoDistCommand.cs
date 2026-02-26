namespace codecrafters_redis.src.Commands.Geospatial;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class GeoDistCommand(ICacheStore cacheStore) : IRedisCommand
{
  private static readonly double earthRadiusInMeters = 6372797.560856;

  public string Name => "GEODIST";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 4)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'geodist'");
    }

    string key = args[1].ToString();
    string member1 = args[2].ToString();
    string member2 = args[3].ToString();
    
    double? encodedLatLon1 = cacheStore.ZScore(key, member1);
    if (encodedLatLon1 == null)
    {
      return CommandHelper.BuildErrorAsync($"ERR member {member1} not found");
    }
    
    double? encodedLatLon2 = cacheStore.ZScore(key, member2);
    if (encodedLatLon2 == null)
    {
      return CommandHelper.BuildErrorAsync($"ERR member {member2} not found");
    }

    var (latitude1, longitude1) = GeohashDecoder.Decode((long)encodedLatLon1.Value);
    var (latitude2, longitude2) = GeohashDecoder.Decode((long)encodedLatLon2.Value);

    double distance = CalculateDistanceUsingHaversineFormula(latitude1, longitude1, latitude2, longitude2);

    return CommandHelper.FormatBulkAsync(distance.ToString());
  }

  private static double CalculateDistanceUsingHaversineFormula(double lat1, double lon1, double lat2, double lon2) {
    var dLat = ToRadians(lat2 - lat1);
    var dLon = ToRadians(lon2 - lon1);
    lat1 = ToRadians(lat1);
    lat2 = ToRadians(lat2);
   
    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
    return earthRadiusInMeters * 2 * Math.Asin(Math.Sqrt(a));
  }
  
  private static double ToRadians(double angle) {
    return Math.PI * angle / 180.0;
  }
}