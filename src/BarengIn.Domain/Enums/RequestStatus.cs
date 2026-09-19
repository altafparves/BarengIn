namespace BarengIn.Domain.Enums;

/// <summary>Lifecycle of a passenger's request to join a trip.</summary>
public enum RequestStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Withdrawn = 3,
    Expired = 4
}
