using BarengIn.Domain.Enums;
using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain.Entities;

/// <summary>
/// A passenger asking to join a trip. The driver decides, so the request stays pending until
/// somebody acts on it; every transition out of pending is final.
/// </summary>
public class RideRequest
{
    public RideRequest(Guid tripId, Guid passengerId, GeoPoint pickupPoint, DateTimeOffset requestedAt, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(pickupPoint);

        if (tripId == Guid.Empty)
        {
            throw new ArgumentException("A ride request must reference a trip.", nameof(tripId));
        }

        if (passengerId == Guid.Empty)
        {
            throw new ArgumentException("A ride request must reference a passenger.", nameof(passengerId));
        }

        Id = Guid.NewGuid();
        TripId = tripId;
        PassengerId = passengerId;
        PickupPoint = pickupPoint;
        RequestedAt = requestedAt;
        Message = message;
        Status = RequestStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid TripId { get; private set; }

    public Guid PassengerId { get; private set; }

    public GeoPoint PickupPoint { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Optional note from the passenger to the driver.</summary>
    public string? Message { get; private set; }

    public RequestStatus Status { get; private set; }

    /// <summary>When the request left the pending state; null while it is still pending.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Driver accepts the request.</summary>
    public void Accept(DateTimeOffset resolvedAt) => Resolve(RequestStatus.Accepted, resolvedAt);

    /// <summary>Driver declines the request.</summary>
    public void Reject(DateTimeOffset resolvedAt) => Resolve(RequestStatus.Rejected, resolvedAt);

    /// <summary>Passenger takes the request back.</summary>
    public void Withdraw(DateTimeOffset resolvedAt) => Resolve(RequestStatus.Withdrawn, resolvedAt);

    /// <summary>The trip departed with the request still unanswered.</summary>
    public void Expire(DateTimeOffset resolvedAt) => Resolve(RequestStatus.Expired, resolvedAt);

    private void Resolve(RequestStatus newStatus, DateTimeOffset resolvedAt)
    {
        if (Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException(
                $"A ride request that is already {Status} cannot become {newStatus}.");
        }

        if (resolvedAt < RequestedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resolvedAt), resolvedAt, "A request cannot be resolved before it was made.");
        }

        Status = newStatus;
        ResolvedAt = resolvedAt;
    }
}
