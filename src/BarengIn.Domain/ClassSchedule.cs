using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain;

/// <summary>A recurring weekly class slot that a student's trips must accommodate.</summary>
public class ClassSchedule
{
    /// <summary>Margin before class start by which a trip must arrive at the faculty.</summary>
    private const int ArrivalBufferMinutes = 15;

    private readonly Guid scheduleId;
    private readonly Guid userId;
    private readonly string courseName;
    private readonly Guid facultyId;
    private readonly DayOfWeek dayOfWeek;
    private readonly TimeSpan startTime;
    private readonly TimeSpan endTime;

    public ClassSchedule(
        Guid userId,
        string courseName,
        Guid facultyId,
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);
        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.", nameof(endTime));
        }

        scheduleId = Guid.NewGuid();
        this.userId = userId;
        this.courseName = courseName;
        this.facultyId = facultyId;
        this.dayOfWeek = dayOfWeek;
        this.startTime = startTime;
        this.endTime = endTime;
    }

    /// <summary>True if this schedule and <paramref name="other"/> fall on the same day with overlapping time ranges.</summary>
    public bool OverlapsWith(ClassSchedule other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return dayOfWeek == other.dayOfWeek
            && startTime < other.endTime
            && other.startTime < endTime;
    }

    /// <summary>Latest moment a passenger may arrive at the faculty for the class held on <paramref name="date"/>.</summary>
    public DateTime GetArrivalDeadline(DateTime date) =>
        date.Date.Add(startTime) - TimeSpan.FromMinutes(ArrivalBufferMinutes);

    /// <summary>
    /// True if <paramref name="trip"/> lands its passenger at this class's faculty, on this class's
    /// day, in time for the arrival deadline.
    /// </summary>
    public bool MatchesTrip(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);

        return trip.DestinationFacultyId == facultyId
            && trip.DepartureTime.DayOfWeek == dayOfWeek
            && trip.DepartureTime <= GetArrivalDeadline(trip.DepartureTime.Date);
    }
}
