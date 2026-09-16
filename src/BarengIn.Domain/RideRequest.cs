using BarengIn.Domain.Enums;
using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain;

/// <summary>A passenger's request to join a specific trip at a specific pickup point.</summary>
public class RideRequest
{
    private readonly Guid requestId;
    private readonly Guid tripId;
    private readonly Guid passengerId;
    private readonly string pickupAddress;
    private readonly GeoPoint pickupPoint;
    private readonly string message;
    private RequestStatus status;
    private readonly DateTime requestedAt;

    /// <summary>Unset (<see cref="DateTime.MinValue"/>) until <see cref="Approve"/>, <see cref="Reject"/> or <see cref="Cancel"/> runs.</summary>
    private DateTime respondedAt;

    public RideRequest(Guid tripId, Guid passengerId, string pickupAddress, GeoPoint pickupPoint, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pickupAddress);
        ArgumentNullException.ThrowIfNull(pickupPoint);

        requestId = Guid.NewGuid();
        this.tripId = tripId;
        this.passengerId = passengerId;
        this.pickupAddress = pickupAddress;
        this.pickupPoint = pickupPoint;
        this.message = message ?? string.Empty;
        status = RequestStatus.Pending;
        requestedAt = DateTime.UtcNow;
        respondedAt = DateTime.MinValue;
    }

    /// <summary>Identity, exposed read-only so a <see cref="Trip"/> or <see cref="Driver"/> can match requests by id.</summary>
    public Guid RequestId => requestId;

    /// <summary>Owning trip, exposed read-only so a receiving <see cref="Trip"/> can validate it owns this request.</summary>
    public Guid TripId => tripId;

    /// <summary>Requesting passenger, exposed read-only so a <see cref="Driver"/> can seat them on approval.</summary>
    public Guid PassengerId => passengerId;

    public bool Approve()
    {
        if (status != RequestStatus.Pending)
        {
            return false;
        }

        status = RequestStatus.Approved;
        respondedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>The rejection reason is validated but not stored: the diagram gives RideRequest no field for it.</summary>
    public bool Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (status != RequestStatus.Pending)
        {
            return false;
        }

        status = RequestStatus.Rejected;
        respondedAt = DateTime.UtcNow;
        return true;
    }

    public bool Cancel()
    {
        if (status != RequestStatus.Pending && status != RequestStatus.Approved)
        {
            return false;
        }

        status = RequestStatus.Cancelled;
        respondedAt = DateTime.UtcNow;
        return true;
    }

    public bool IsPending() => status == RequestStatus.Pending;

    /// <summary>
    /// Not implemented here: a correct detour needs the trip's destination coordinates, but Trip
    /// only stores a destination faculty id, not a GeoPoint. Resolving the Faculty's location is
    /// an Application-layer lookup.
    /// </summary>
    public double GetDetourDistance(Trip trip) =>
        throw new NotImplementedException(
            "Requires the destination Faculty's GeoPoint, resolved via a repository lookup in the Application layer.");
}
