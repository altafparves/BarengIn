using BarengIn.Domain.Enums;

namespace BarengIn.Domain.Entities;

/// <summary>
/// A published CO2 emission factor for a vehicle type. The citation is part of the record
/// rather than a comment somewhere: any figure shown to a user has to be traceable to the
/// source and year it came from.
/// </summary>
public class EmissionFactor
{
    public EmissionFactor(VehicleType vehicleType, double gramsCo2PerKm, string source, int year)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        if (double.IsNaN(gramsCo2PerKm) || gramsCo2PerKm <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gramsCo2PerKm), gramsCo2PerKm, "An emission factor must be greater than zero.");
        }

        if (year < 1900)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year), year, "Publication year is not plausible.");
        }

        Id = Guid.NewGuid();
        VehicleType = vehicleType;
        GramsCo2PerKm = gramsCo2PerKm;
        Source = source;
        Year = year;
    }

    public Guid Id { get; private set; }

    public VehicleType VehicleType { get; private set; }

    public double GramsCo2PerKm { get; private set; }

    /// <summary>The publication this factor is taken from.</summary>
    public string Source { get; private set; }

    /// <summary>Publication year of <see cref="Source"/>.</summary>
    public int Year { get; private set; }

    /// <summary>Grams of CO2 a solo trip of the given distance would produce.</summary>
    public double EstimateGramsCo2(double distanceKm)
    {
        if (double.IsNaN(distanceKm) || distanceKm < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distanceKm), distanceKm, "Distance cannot be negative.");
        }

        return GramsCo2PerKm * distanceKm;
    }

    public override string ToString() =>
        FormattableString.Invariant($"{VehicleType}: {GramsCo2PerKm} gCO2/km ({Source}, {Year})");
}
