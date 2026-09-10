using BarengIn.Domain.Enums;

namespace BarengIn.Domain.Entities;

/// <summary>
/// A student who commutes by private vehicle and offers the empty seats.
/// <para>
/// A driver derives from <see cref="Passenger"/> because the PRD says a student may act as
/// passenger, driver, or both. <see cref="CanRequestRide"/> is therefore intentionally not
/// overridden: a driver whose own vehicle is unavailable is still a passenger and inherits
/// exactly the passenger rule for joining someone else's trip. Only <see cref="CanPublishTrip"/>,
/// which is the genuinely driver-specific capability, is overridden here.
/// </para>
/// </summary>
public class Driver : Passenger
{
    public Driver(
        string email,
        string fullName,
        Faculty faculty,
        string department,
        string phoneNumber,
        Vehicle? vehicle = null)
        : base(email, fullName, faculty, department, phoneNumber)
    {
        Vehicle = vehicle;
    }

    /// <summary>The vehicle used to publish trips; null until the driver registers one.</summary>
    public Vehicle? Vehicle { get; private set; }

    /// <summary>A verified driver with a registered vehicle may publish trips.</summary>
    public override bool CanPublishTrip() =>
        VerificationStatus == VerificationStatus.Verified && Vehicle is not null;

    public void RegisterVehicle(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        Vehicle = vehicle;
    }

    public void RemoveVehicle() => Vehicle = null;
}
