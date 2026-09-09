namespace BarengIn.Domain.Entities;

/// <summary>
/// A moderator who reviews student ID submissions and manages reference data. An admin
/// account is not a commuter account, so it takes part in no rides in either direction.
/// </summary>
public class Admin : User
{
    public Admin(
        string email,
        string fullName,
        Faculty faculty,
        string department,
        string phoneNumber)
        : base(email, fullName, faculty, department, phoneNumber)
    {
    }

    public override bool CanPublishTrip() => false;

    public override bool CanRequestRide() => false;

    /// <summary>Approves a student ID submission on behalf of the platform.</summary>
    public void ApproveVerification(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.Verify();
    }

    /// <summary>Rejects a student ID submission on behalf of the platform.</summary>
    public void DeclineVerification(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.RejectVerification();
    }
}
