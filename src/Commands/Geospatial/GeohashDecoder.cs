namespace codecrafters_redis.src.Commands.Geospatial;

public class GeohashDecoder
{
  private const double MIN_LATITUDE = -85.05112878;
  private const double MAX_LATITUDE = 85.05112878;
  private const double MIN_LONGITUDE = -180;
  private const double MAX_LONGITUDE = 180;
  private const double LATITUDE_RANGE = MAX_LATITUDE - MIN_LATITUDE;
  private const double LONGITUDE_RANGE = MAX_LONGITUDE - MIN_LONGITUDE;

  /// <summary>
  /// Decode converts geo code (WGS84) to tuple of (latitude, longitude)
  /// </summary>
  /// <param name="geoCode">The encoded geographic code</param>
  /// <returns>Tuple containing (latitude, longitude)</returns>
  public static (double latitude, double longitude) Decode(long geoCode)
  {
    // Align bits of both latitude and longitude to take even-numbered position
    long y = geoCode >> 1;
    long x = geoCode;

    // Compact bits back to 32-bit ints
    int gridLatitudeNumber = CompactInt64ToInt32(x);
    int gridLongitudeNumber = CompactInt64ToInt32(y);

    return ConvertGridNumbersToCoordinates(gridLatitudeNumber, gridLongitudeNumber);
  }

  /// <summary>
  /// Compact a 64-bit integer with interleaved bits back to a 32-bit integer.
  /// This is the reverse operation of spread_int32_to_int64.
  /// </summary>
  /// <param name="v">The 64-bit integer with interleaved bits</param>
  /// <returns>Compacted 32-bit integer</returns>
  private static int CompactInt64ToInt32(long v)
  {
    v = v & 0x5555555555555555;
    v = (v | (v >> 1)) & 0x3333333333333333;
    v = (v | (v >> 2)) & 0x0F0F0F0F0F0F0F0F;
    v = (v | (v >> 4)) & 0x00FF00FF00FF00FF;
    v = (v | (v >> 8)) & 0x0000FFFF0000FFFF;
    v = (v | (v >> 16)) & 0x00000000FFFFFFFF;
    return (int)v;
  }

  /// <summary>
  /// Convert grid numbers back to geographic coordinates
  /// </summary>
  /// <param name="gridLatitudeNumber">Grid latitude number</param>
  /// <param name="gridLongitudeNumber">Grid longitude number</param>
  /// <returns>Tuple containing (latitude, longitude)</returns>
  private static (double latitude, double longitude) ConvertGridNumbersToCoordinates(int gridLatitudeNumber, int gridLongitudeNumber)
  {
    // Calculate the grid boundaries
    double gridLatitudeMin = MIN_LATITUDE + LATITUDE_RANGE * (gridLatitudeNumber / Math.Pow(2, 26));
    double gridLatitudeMax = MIN_LATITUDE + LATITUDE_RANGE * ((gridLatitudeNumber + 1) / Math.Pow(2, 26));
    double gridLongitudeMin = MIN_LONGITUDE + LONGITUDE_RANGE * (gridLongitudeNumber / Math.Pow(2, 26));
    double gridLongitudeMax = MIN_LONGITUDE + LONGITUDE_RANGE * ((gridLongitudeNumber + 1) / Math.Pow(2, 26));

    // Calculate the center point of the grid cell
    double latitude = (gridLatitudeMin + gridLatitudeMax) / 2;
    double longitude = (gridLongitudeMin + gridLongitudeMax) / 2;

    return (latitude, longitude);
  }
}