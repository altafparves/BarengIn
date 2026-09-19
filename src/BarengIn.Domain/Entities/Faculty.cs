using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain.Entities;

/// <summary>
/// A campus faculty. Matching is done against the faculty a trip ends at, so the
/// faculty carries its own map location.
/// </summary>
public class Faculty
{
    public Faculty(string name, string shortName, GeoPoint location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(shortName);
        ArgumentNullException.ThrowIfNull(location);

        Id = Guid.NewGuid();
        Name = name;
        ShortName = shortName;
        Location = location;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string ShortName { get; private set; }

    public GeoPoint Location { get; private set; }

    public void Rename(string name, string shortName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(shortName);

        Name = name;
        ShortName = shortName;
    }

    public void MoveTo(GeoPoint location)
    {
        ArgumentNullException.ThrowIfNull(location);
        Location = location;
    }
}
