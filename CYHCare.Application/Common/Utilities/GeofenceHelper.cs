namespace CYHCare.Application.Common.Utilities;

public static class GeofenceHelper
{
    private const double EarthRadiusMeters = 6371000;

    /// <summary>
    /// Calculates the distance in meters between two GPS coordinates using the Haversine formula.
    /// </summary>
    public static double CalculateDistanceMeters(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        var dLat = ToRadians((double)(lat2 - lat1));
        var dLng = ToRadians((double)(lng2 - lng1));

        var rLat1 = ToRadians((double)lat1);
        var rLat2 = ToRadians((double)lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
}
