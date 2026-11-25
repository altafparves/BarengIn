using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain;

/// <summary>A UGM faculty: a possible trip destination and a user's home faculty.</summary>
public class Faculty
{
    private readonly Guid facultyId;
    private readonly string name;
    private readonly string shortName;
    private readonly GeoPoint location;

    public Faculty(string name, string shortName, GeoPoint location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(shortName);
        ArgumentNullException.ThrowIfNull(location);

        facultyId = Guid.NewGuid();
        this.name = name;
        this.shortName = shortName;
        this.location = location;
    }

    public double DistanceTo(GeoPoint point) => location.DistanceTo(point);

    public string GetFullName() => $"{name} ({shortName})";
}
