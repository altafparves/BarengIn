namespace BarengIn.Domain.ValueObjects;

/// <summary>
/// An immutable latitude/longitude coordinate. Two instances with the same
/// coordinates are considered equal, as expected of a value object.
/// </summary>
public sealed class GeoPoint : IEquatable<GeoPoint>
{
    private const double EarthRadiusKm = 6371.0;

    private readonly double latitude;
    private readonly double longitude;

    public GeoPoint(double lat, double lng)
    {
        if (lat is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(lat), lat, "Latitude must be between -90 and 90 degrees.");
        }

        if (lng is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(lng), lng, "Longitude must be between -180 and 180 degrees.");
        }

        latitude = lat;
        longitude = lng;
    }

    /// <summary>Great-circle distance to another point, in kilometers (haversine formula).</summary>
    public double DistanceTo(GeoPoint other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var lat1 = DegreesToRadians(latitude);
        var lat2 = DegreesToRadians(other.latitude);
        var deltaLat = DegreesToRadians(other.latitude - latitude);
        var deltaLng = DegreesToRadians(other.longitude - longitude);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    public bool IsWithinRadius(GeoPoint other, double km) => DistanceTo(other) <= km;

    public override string ToString() => $"({latitude}, {longitude})";

    public bool Equals(GeoPoint? other) =>
        other is not null && latitude.Equals(other.latitude) && longitude.Equals(other.longitude);

    public override bool Equals(object? obj) => Equals(obj as GeoPoint);

    public override int GetHashCode() => HashCode.Combine(latitude, longitude);

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
