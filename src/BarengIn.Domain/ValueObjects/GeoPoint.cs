namespace BarengIn.Domain.ValueObjects;

/// <summary>
/// An immutable WGS-84 coordinate. Compared by value, so two points with the same
/// latitude and longitude are the same point.
/// </summary>
public sealed record GeoPoint
{
    private const double EarthRadiusKm = 6371.0;

    public GeoPoint(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude), latitude, "Latitude must be between -90 and 90 degrees.");
        }

        if (double.IsNaN(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude), longitude, "Longitude must be between -180 and 180 degrees.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    /// <summary>
    /// Great-circle distance to <paramref name="other"/> in kilometres, using the haversine
    /// formula. This is a straight-line estimate; road distance for a published trip comes
    /// from the routing provider and is stored on the trip itself.
    /// </summary>
    public double DistanceKmTo(GeoPoint other)
    {
        ArgumentNullException.ThrowIfNull(other);

        double deltaLatitude = ToRadians(other.Latitude - Latitude);
        double deltaLongitude = ToRadians(other.Longitude - Longitude);

        double a = (Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2))
            + (Math.Cos(ToRadians(Latitude))
                * Math.Cos(ToRadians(other.Latitude))
                * Math.Sin(deltaLongitude / 2)
                * Math.Sin(deltaLongitude / 2));

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    public override string ToString() =>
        FormattableString.Invariant($"({Latitude:F6}, {Longitude:F6})");

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
