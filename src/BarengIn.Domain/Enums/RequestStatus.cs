namespace BarengIn.Domain.Enums;

/// <summary>Lifecycle state of a passenger's ride request against a trip.</summary>
public enum RequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}
