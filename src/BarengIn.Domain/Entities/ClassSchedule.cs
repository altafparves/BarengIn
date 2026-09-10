namespace BarengIn.Domain.Entities;

/// <summary>
/// One recurring weekly class. Matching compares these against trip departure times, so an
/// entry whose end time is not after its start time is rejected outright.
/// </summary>
public class ClassSchedule
{
    public ClassSchedule(
        Guid ownerId,
        string courseName,
        DayOfWeek day,
        TimeOnly startTime,
        TimeOnly endTime,
        Faculty faculty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(courseName);
        ArgumentNullException.ThrowIfNull(faculty);

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A class schedule must belong to a user.", nameof(ownerId));
        }

        EnsureValidTimeRange(startTime, endTime);

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        CourseName = courseName;
        Day = day;
        StartTime = startTime;
        EndTime = endTime;
        Faculty = faculty;
    }

    public Guid Id { get; private set; }

    /// <summary>Identifier of the user this entry belongs to.</summary>
    public Guid OwnerId { get; private set; }

    public string CourseName { get; private set; }

    public DayOfWeek Day { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    /// <summary>The faculty the class is held at, which is where the student needs to be.</summary>
    public Faculty Faculty { get; private set; }

    public void Reschedule(DayOfWeek day, TimeOnly startTime, TimeOnly endTime)
    {
        EnsureValidTimeRange(startTime, endTime);

        Day = day;
        StartTime = startTime;
        EndTime = endTime;
    }

    public void MoveTo(Faculty faculty)
    {
        ArgumentNullException.ThrowIfNull(faculty);
        Faculty = faculty;
    }

    private static void EnsureValidTimeRange(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException(
                $"A class must end after it starts, but {endTime} is not after {startTime}.",
                nameof(endTime));
        }
    }
}
