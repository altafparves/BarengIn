namespace BarengIn.Domain.Enums;

/// <summary>Lifecycle of a published trip.</summary>
public enum TripStatus
{
    Open = 0,
    Full = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
