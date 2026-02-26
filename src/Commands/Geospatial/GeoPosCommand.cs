namespace codecrafters_redis.src.Commands.Geospatial;

using System.Globalization;
using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class GeoPosCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "GEOPOS";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'geopos'");
    }

    string key = args[1].ToString();

    var output = new List<string>();

    for (int i = 2; i < args.Count; i++)
    {
      string member = args[i].ToString();

      double? encodedLatLon = cacheStore.ZScore(key, member);
      if (encodedLatLon == null)
      {
        output.Add(CommandHelper.FormatNull(RespType.Array));
        continue;
      }

      var (latitude, longitude) = GeohashDecoder.Decode((long)encodedLatLon.Value);
      output.Add(
        CommandHelper.FormatArrayOfResp(
          [
            CommandHelper.FormatBulk(longitude.ToString(CultureInfo.InvariantCulture)),
            CommandHelper.FormatBulk(latitude.ToString(CultureInfo.InvariantCulture))
          ]));
    }

    return Task.FromResult(CommandHelper.FormatArrayOfResp(output));
  }
}
