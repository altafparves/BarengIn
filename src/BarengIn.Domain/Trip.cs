using BarengIn.Domain.Enums;
using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain;

/// <summary>A trip a driver offers, from an origin to a destination faculty, with seats to share.</summary>
public class Trip
{
    private readonly Guid tripId;
    private readonly Guid driverId;
    private readonly Guid vehicleId;
    private readonly string originAddress;
    private readonly GeoPoint originLocation;
    private readonly Guid destinationFacultyId;
    private readonly DateTime departureTime;
    private int availableSeats;
    private Money costPerSeat;
    private readonly double distanceKm;
    private readonly string polylineJson;
    private TripStatus status;
    private readonly DateTime createdAt;

    /// <summary>Backs the "Trip receives RideRequest" composition; not an attribute in the diagram.</summary>
    private readonly List<RideRequest> rideRequests = [];

    /// <summary>
    /// Ids of passengers currently holding a seat. The diagram gives Trip no such attribute, but
    /// AddPassenger/RemovePassenger need it to be more than a stub: without tracking who is
    /// seated, a passenger could be removed (freeing a seat) without ever having been added.
    /// </summary>
    private readonly HashSet<Guid> passengerIds = [];

    public Trip(
        Guid driverId,
        Guid vehicleId,
        string originAddress,
        GeoPoint originLocation,
        Guid destinationFacultyId,
        DateTime departureTime,
        int availableSeats,
        double distanceKm,
        string polylineJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originAddress);
        ArgumentNullException.ThrowIfNull(originLocation);
        if (availableSeats <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableSeats), availableSeats, "Available seats must be greater than zero.");
        }

        if (distanceKm < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceKm), distanceKm, "Distance cannot be negative.");
        }

        tripId = Guid.NewGuid();
        this.driverId = driverId;
        this.vehicleId = vehicleId;
        this.originAddress = originAddress;
        this.originLocation = originLocation;
        this.destinationFacultyId = destinationFacultyId;
        this.departureTime = departureTime;
        this.availableSeats = availableSeats;
        costPerSeat = new Money(0, "IDR");
        this.distanceKm = distanceKm;
        this.polylineJson = polylineJson ?? string.Empty;
        // A trip constructed here is already offered to passengers, matching Driver.PublishTrip's
        // naming. TripStatus.Draft stays reachable for a future save-without-publishing flow.
        status = TripStatus.Published;
        createdAt = DateTime.UtcNow;
    }

    /// <summary>Identity, exposed read-only so a <see cref="Driver"/> can match trips by id.</summary>
    public Guid TripId => tripId;

    /// <summary>Exposed read-only so <see cref="ClassSchedule.MatchesTrip"/> can compare against it.</summary>
    public Guid DestinationFacultyId => destinationFacultyId;

    /// <summary>Exposed read-only so <see cref="ClassSchedule.MatchesTrip"/> can compare against it.</summary>
    public DateTime DepartureTime => departureTime;

    /// <summary>Requests received for this trip, exposed read-only so a <see cref="Driver"/> can review and act on them.</summary>
    public IReadOnlyList<RideRequest> RideRequests => rideRequests;

    public bool HasAvailableSeat() => availableSeats > 0;

    public bool AddPassenger(Guid passengerId)
    {
        if (!HasAvailableSeat() || !passengerIds.Add(passengerId))
        {
            return false;
        }

        availableSeats--;
        if (availableSeats == 0)
        {
            status = TripStatus.Full;
        }

        return true;
    }

    public bool RemovePassenger(Guid passengerId)
    {
        if (!passengerIds.Remove(passengerId))
        {
            return false;
        }

        availableSeats++;
        if (status == TripStatus.Full)
        {
            status = TripStatus.Published;
        }

        return true;
    }

    /// <summary>
    /// Splits the fuel cost across every seat originally offered (seats currently free plus
    /// seats already taken), so the figure stays stable as passengers join.
    /// </summary>
    public Money CalculateCostPerSeat(decimal fuelPrice)
    {
        if (fuelPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fuelPrice), fuelPrice, "Fuel price cannot be negative.");
        }

        var totalSeats = availableSeats + passengerIds.Count;
        var totalCost = fuelPrice * (decimal)distanceKm;
        costPerSeat = new Money(totalCost, "IDR").Split(totalSeats);
        return costPerSeat;
    }

    /// <summary>
    /// Not implemented here: computing CO2 saved needs the vehicle's EmissionFactor, which
    /// <see cref="Vehicle.GetEmissionFactor"/> cannot resolve inside the Domain layer either.
    /// </summary>
    public double CalculateCo2Saved() =>
        throw new NotImplementedException(
            "Requires the trip's Vehicle EmissionFactor, resolved via a repository lookup in the Application layer.");

    public bool Start()
    {
        if (status != TripStatus.Published && status != TripStatus.Full)
        {
            return false;
        }

        status = TripStatus.InProgress;
        return true;
    }

    public bool Complete()
    {
        if (status != TripStatus.InProgress)
        {
            return false;
        }

        status = TripStatus.Completed;
        return true;
    }

    /// <summary>The cancellation reason is validated but not stored: the diagram gives Trip no field for it.</summary>
    public bool Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (status == TripStatus.Completed || status == TripStatus.Cancelled)
        {
            return false;
        }

        status = TripStatus.Cancelled;
        return true;
    }

    public bool IsMatchingSchedule(ClassSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        return schedule.MatchesTrip(this);
    }

    /// <summary>
    /// Adds a request this trip has received. Not a diagram method: it exists to make the
    /// "Trip receives RideRequest" composition a real collection instead of an unreachable one.
    /// </summary>
    public void ReceiveRequest(RideRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TripId != tripId)
        {
            throw new ArgumentException("Request does not belong to this trip.", nameof(request));
        }

        rideRequests.Add(request);
    }
}
