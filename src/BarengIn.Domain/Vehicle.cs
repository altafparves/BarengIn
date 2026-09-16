using BarengIn.Domain.Enums;

namespace BarengIn.Domain;

/// <summary>A vehicle a driver has registered and can offer trips with.</summary>
public class Vehicle
{
    private readonly Guid vehicleId;
    private readonly Guid driverId;
    private readonly string plateNumber;
    private string brand;
    private string model;
    private string color;
    private readonly VehicleType type;
    private readonly int seatCapacity;

    public Vehicle(
        Guid driverId,
        string plateNumber,
        string brand,
        string model,
        string color,
        VehicleType type,
        int seatCapacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plateNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        if (seatCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatCapacity), seatCapacity, "Seat capacity must be greater than zero.");
        }

        vehicleId = Guid.NewGuid();
        this.driverId = driverId;
        this.plateNumber = plateNumber;
        this.brand = brand;
        this.model = model;
        this.color = color;
        this.type = type;
        this.seatCapacity = seatCapacity;
    }

    public int GetSeatCapacity() => seatCapacity;

    public bool UpdateDetails(string brand, string model, string color)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);

        this.brand = brand;
        this.model = model;
        this.color = color;
        return true;
    }

    /// <summary>
    /// Not implemented here: the diagram gives Vehicle no reference to an EmissionFactor, only
    /// the "rated by" relationship. Resolving the EmissionFactor for this vehicle's <see cref="VehicleType"/>
    /// requires a repository lookup, which belongs to the Application layer.
    /// </summary>
    public EmissionFactor GetEmissionFactor() =>
        throw new NotImplementedException(
            "Requires an EmissionFactor repository lookup by VehicleType, resolved in the Application layer.");
}
