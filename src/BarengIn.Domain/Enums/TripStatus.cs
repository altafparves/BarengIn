namespace BarengIn.Domain.Enums;

/// <summary>Lifecycle state of a published carpool trip.</summary>
public enum TripStatus
{
    Draft,
    Published,
    Full,
    InProgress,
    Completed,
    Cancelled
}
