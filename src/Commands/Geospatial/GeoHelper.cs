namespace codecrafters_redis.src.Commands.Geospatial;

using System.Globalization;

public static class GeoHelper
{
  public const double MinLongitude = -180;
  public const double MaxLongitude = 180;
  public const double MinLatitude = -85.05112878;
  public const double MaxLatitude = 85.05112878;
  private const double EarthRadiusInMeters = 6372797.560856;

  public static bool TryParseDouble(string value, out double parsedValue)
  {
    return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue);
  }

  public static bool IsValidCoordinatePair(double longitude, double latitude)
  {
    return longitude >= MinLongitude
      && longitude <= MaxLongitude
      && latitude >= MinLatitude
      && latitude <= MaxLatitude;
  }

  public static string BuildInvalidCoordinatePairError(double longitude, double latitude)
  {
    return $"ERR invalid longitude, latitude pair {longitude.ToString(CultureInfo.InvariantCulture)},{latitude.ToString(CultureInfo.InvariantCulture)} is not a valid geospatial key";
  }

  public static double CalculateDistanceUsingHaversineFormula(double lat1, double lon1, double lat2, double lon2)
  {
    double dLat = ToRadians(lat2 - lat1);
    double dLon = ToRadians(lon2 - lon1);
    double lat1Rad = ToRadians(lat1);
    double lat2Rad = ToRadians(lat2);

    double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1Rad) * Math.Cos(lat2Rad);
    return EarthRadiusInMeters * 2 * Math.Asin(Math.Sqrt(a));
  }

  private static double ToRadians(double angle)
  {
    return Math.PI * angle / 180.0;
  }
}
