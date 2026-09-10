using BarengIn.Domain.Enums;
using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain.Entities;

/// <summary>
/// A journey a driver has published, together with the seats still on offer. Seat counts and
/// status are only ever changed through the methods below, so an illegal transition throws
/// rather than leaving the trip in a state the rest of the system has to defend against.
/// </summary>
public class Trip
{
    private readonly List<Guid> _acceptedPassengerIds = [];

    public Trip(
        Guid driverId,
        GeoPoint origin,
        GeoPoint destination,
        Faculty destinationFaculty,
        DateTimeOffset departureTime,
        int totalSeats,
        Money costPerSeat)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(destinationFaculty);
        ArgumentNullException.ThrowIfNull(costPerSeat);

        if (driverId == Guid.Empty)
        {
            throw new ArgumentException("A trip must have a driver.", nameof(driverId));
        }

        if (totalSeats < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalSeats), totalSeats, "A trip must offer at least one seat.");
        }

        Id = Guid.NewGuid();
        DriverId = driverId;
        Origin = origin;
        Destination = destination;
        DestinationFaculty = destinationFaculty;
        DepartureTime = departureTime;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        CostPerSeat = costPerSeat;
        Status = TripStatus.Open;
    }

    public Guid Id { get; private set; }

    public Guid DriverId { get; private set; }

    public GeoPoint Origin { get; private set; }

    public GeoPoint Destination { get; private set; }

    public Faculty DestinationFaculty { get; private set; }

    public DateTimeOffset DepartureTime { get; private set; }

    public int TotalSeats { get; private set; }

    public int AvailableSeats { get; private set; }

    public Money CostPerSeat { get; private set; }

    public TripStatus Status { get; private set; }

    /// <summary>
    /// Road distance from the routing provider, set once when the trip is created. It is
    /// stored rather than recalculated so that listing a trip never triggers a route lookup.
    /// </summary>
    public double? DistanceKm { get; private set; }

    /// <summary>Encoded route geometry, stored alongside <see cref="DistanceKm"/> and set once.</summary>
    public string? PolylineJson { get; private set; }

    public IReadOnlyCollection<Guid> AcceptedPassengerIds => _acceptedPassengerIds.AsReadOnly();

    /// <summary>
    /// Records the route returned by the routing provider. It can only be set once: the
    /// figures shown to passengers must not drift after the trip has been advertised.
    /// </summary>
    public void SetRoute(double distanceKm, string polylineJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(polylineJson);

        if (double.IsNaN(distanceKm) || distanceKm <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distanceKm), distanceKm, "Route distance must be greater than zero.");
        }

        if (DistanceKm.HasValue || PolylineJson is not null)
        {
            throw new InvalidOperationException(
                "The route for this trip has already been set and cannot be recalculated.");
        }

        DistanceKm = distanceKm;
        PolylineJson = polylineJson;
    }

    /// <summary>Takes a seat for a passenger, closing the trip automatically when the last seat goes.</summary>
    public void AcceptPassenger(Guid passengerId)
    {
        if (passengerId == Guid.Empty)
        {
            throw new ArgumentException("A passenger id is required.", nameof(passengerId));
        }

        if (Status != TripStatus.Open)
        {
            throw new InvalidOperationException(
                $"Cannot accept a passenger onto a trip that is {Status}.");
        }

        if (_acceptedPassengerIds.Contains(passengerId))
        {
            throw new InvalidOperationException("That passenger is already on this trip.");
        }

        _acceptedPassengerIds.Add(passengerId);
        AvailableSeats--;

        if (AvailableSeats == 0)
        {
            Status = TripStatus.Full;
        }
    }

    /// <summary>Gives a seat back, reopening a full trip.</summary>
    public void ReleaseSeat(Guid passengerId)
    {
        if (Status is not (TripStatus.Open or TripStatus.Full))
        {
            throw new InvalidOperationException(
                $"Cannot release a seat on a trip that is {Status}.");
        }

        if (!_acceptedPassengerIds.Remove(passengerId))
        {
            throw new InvalidOperationException("That passenger is not on this trip.");
        }

        AvailableSeats++;

        if (Status == TripStatus.Full)
        {
            Status = TripStatus.Open;
        }
    }

    /// <summary>Marks the trip as under way; no further seats can be taken or released.</summary>
    public void Start()
    {
        if (Status is not (TripStatus.Open or TripStatus.Full))
        {
            throw new InvalidOperationException($"Cannot start a trip that is {Status}.");
        }

        Status = TripStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != TripStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Only a trip that is in progress can be completed, but this trip is {Status}.");
        }

        Status = TripStatus.Completed;
    }

    /// <summary>Cancels the trip. Only allowed before it has started.</summary>
    public void Cancel()
    {
        if (Status is not (TripStatus.Open or TripStatus.Full))
        {
            throw new InvalidOperationException($"Cannot cancel a trip that is {Status}.");
        }

        Status = TripStatus.Cancelled;
    }
}
