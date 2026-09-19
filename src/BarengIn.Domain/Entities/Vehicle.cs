using BarengIn.Domain.Enums;

namespace BarengIn.Domain.Entities;

/// <summary>
/// A driver's registered vehicle. Fuel efficiency starts from a per-type default so that a
/// driver can publish a trip without knowing the figure, and can be corrected afterwards.
/// </summary>
public class Vehicle
{
    private const double DefaultMotorcycleKmPerLitre = 45.0;
    private const double DefaultCarKmPerLitre = 12.0;

    public Vehicle(
        string plateNumber,
        VehicleType type,
        string brand,
        int seatCapacity,
        double? fuelEfficiencyKmPerLitre = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plateNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);

        if (seatCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seatCapacity), seatCapacity, "A vehicle must seat at least one passenger.");
        }

        double efficiency = fuelEfficiencyKmPerLitre ?? DefaultEfficiencyFor(type);
        EnsureUsableEfficiency(efficiency);

        Id = Guid.NewGuid();
        PlateNumber = plateNumber.ToUpperInvariant();
        Type = type;
        Brand = brand;
        SeatCapacity = seatCapacity;
        FuelEfficiencyKmPerLitre = efficiency;
    }

    public Guid Id { get; private set; }

    public string PlateNumber { get; private set; }

    public VehicleType Type { get; private set; }

    public string Brand { get; private set; }

    /// <summary>Seats available to passengers, excluding the driver.</summary>
    public int SeatCapacity { get; private set; }

    public double FuelEfficiencyKmPerLitre { get; private set; }

    public static double DefaultEfficiencyFor(VehicleType type) => type switch
    {
        VehicleType.Motorcycle => DefaultMotorcycleKmPerLitre,
        VehicleType.Car => DefaultCarKmPerLitre,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown vehicle type.")
    };

    /// <summary>Lets the driver replace the per-type default with a measured figure.</summary>
    public void OverrideFuelEfficiency(double kmPerLitre)
    {
        EnsureUsableEfficiency(kmPerLitre);
        FuelEfficiencyKmPerLitre = kmPerLitre;
    }

    private static void EnsureUsableEfficiency(double kmPerLitre)
    {
        if (double.IsNaN(kmPerLitre) || kmPerLitre <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kmPerLitre), kmPerLitre, "Fuel efficiency must be greater than zero.");
        }
    }
}
