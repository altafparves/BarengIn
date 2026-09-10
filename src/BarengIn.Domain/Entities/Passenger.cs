using BarengIn.Domain.Enums;

namespace BarengIn.Domain.Entities;

/// <summary>
/// A student who rides with someone else. Owns the class schedule that schedule-based
/// matching runs against.
/// </summary>
public class Passenger : User
{
    private readonly List<ClassSchedule> _classSchedules = [];

    public Passenger(
        string email,
        string fullName,
        Faculty faculty,
        string department,
        string phoneNumber)
        : base(email, fullName, faculty, department, phoneNumber)
    {
    }

    public IReadOnlyCollection<ClassSchedule> ClassSchedules => _classSchedules.AsReadOnly();

    /// <summary>A passenger has no vehicle, so there is nothing to publish a trip with.</summary>
    public override bool CanPublishTrip() => false;

    /// <summary>Only a verified student may ask to join a trip.</summary>
    public override bool CanRequestRide() => VerificationStatus == VerificationStatus.Verified;

    public void AddClassSchedule(ClassSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (_classSchedules.Any(existing => existing.Id == schedule.Id))
        {
            throw new InvalidOperationException("That class schedule entry is already registered.");
        }

        _classSchedules.Add(schedule);
    }

    public void RemoveClassSchedule(Guid scheduleId)
    {
        ClassSchedule? schedule = _classSchedules.FirstOrDefault(entry => entry.Id == scheduleId);

        if (schedule is null)
        {
            throw new InvalidOperationException(
                $"No class schedule entry with id {scheduleId} is registered.");
        }

        _classSchedules.Remove(schedule);
    }
}
