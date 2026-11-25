using BarengIn.Domain.Enums;

namespace BarengIn.Domain;

/// <summary>Reference CO2 emission rate for a vehicle type, used to score carpooling impact.</summary>
public class EmissionFactor
{
    private readonly Guid emissionFactorId;
    private readonly VehicleType vehicleType;
    private readonly double gramsCo2PerKm;
    private readonly string source;

    public EmissionFactor(VehicleType vehicleType, double gramsCo2PerKm, string source)
    {
        if (gramsCo2PerKm < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gramsCo2PerKm), gramsCo2PerKm, "Emission rate cannot be negative.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        emissionFactorId = Guid.NewGuid();
        this.vehicleType = vehicleType;
        this.gramsCo2PerKm = gramsCo2PerKm;
        this.source = source;
    }

    /// <summary>Grams of CO2 emitted over <paramref name="distanceKm"/> by this vehicle type.</summary>
    public double CalculateEmission(double distanceKm) => distanceKm * gramsCo2PerKm;

    /// <summary>
    /// Grams of CO2 avoided versus each occupant driving separately: every occupant beyond the
    /// first would otherwise have made the same trip alone.
    /// </summary>
    public double CalculateSaving(double distanceKm, int occupants)
    {
        if (occupants < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(occupants), occupants, "There must be at least one occupant.");
        }

        return (occupants - 1) * CalculateEmission(distanceKm);
    }
}
