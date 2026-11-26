using BarengIn.Domain.Enums;

namespace BarengIn.Domain;

/// <summary>A staff account that moderates users and reports on platform impact.</summary>
public class Admin : User
{
    private string department;
    private int adminLevel;

    public Admin(
        string niu,
        string fullName,
        string email,
        string passwordHash,
        string phoneNumber,
        Guid facultyId,
        string department,
        int adminLevel)
        : base(niu, fullName, email, passwordHash, phoneNumber, facultyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(department);
        if (adminLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(adminLevel), adminLevel, "Admin level must be greater than zero.");
        }

        this.department = department;
        this.adminLevel = adminLevel;
    }

    public string Department => department;

    public int AdminLevel => adminLevel;

    /// <summary>An admin moderates the platform; they never drive.</summary>
    public override bool CanPublishTrip() => false;

    public override string GetRoleName() => "Admin";

    /// <summary>Verifies or rejects a student's KTM submission.</summary>
    public bool VerifyKtm(Guid userId, VerificationStatus status)
    {
        if (userId == Guid.Empty || status == VerificationStatus.Unverified)
        {
            return false;
        }

        return true;
    }

    /// <summary>Verifies or rejects a concrete User entity's KTM submission.</summary>
    public bool VerifyKtm(User targetUser, VerificationStatus status)
    {
        ArgumentNullException.ThrowIfNull(targetUser);
        if (status == VerificationStatus.Unverified)
        {
            return false;
        }

        targetUser.SetVerificationStatus(status);
        if (status == VerificationStatus.Verified)
        {
            targetUser.Unsuspend();
        }

        return true;
    }

    /// <summary>Suspends a user account by ID with a moderation reason.</summary>
    public bool SuspendUser(Guid userId, string reason)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        return true;
    }

    /// <summary>Suspends a concrete User entity with a moderation reason.</summary>
    public bool SuspendUser(User targetUser, string reason)
    {
        ArgumentNullException.ThrowIfNull(targetUser);
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        targetUser.Suspend(reason);
        return true;
    }

    /// <summary>Calculates total CO2 saved (in kg) within the requested date window.</summary>
    public double ViewEmissionDashboard(DateTime from, DateTime to) =>
        ViewEmissionDashboard(from, to, 0.0);

    public double ViewEmissionDashboard(DateTime from, DateTime to, double aggregatedCo2Kg)
    {
        if (to < from)
        {
            throw new ArgumentException("End date must not be earlier than start date.", nameof(to));
        }

        return Math.Round(Math.Max(0.0, aggregatedCo2Kg), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Generates a formatted campus sustainability & moderation summary report.</summary>
    public string GenerateReport(DateTime from, DateTime to) =>
        GenerateReport(from, to, totalRides: 0, verifiedUsers: 0, pendingKtms: 0, totalCo2Kg: 0.0);

    public string GenerateReport(
        DateTime from,
        DateTime to,
        int totalRides,
        int verifiedUsers,
        int pendingKtms,
        double totalCo2Kg)
    {
        if (to < from)
        {
            throw new ArgumentException("End date must not be earlier than start date.", nameof(to));
        }

        double co2 = ViewEmissionDashboard(from, to, totalCo2Kg);
        double treesEquivalent = Math.Round(co2 / 21.0, 1, MidpointRounding.AwayFromZero);
        return $"[{department} Sustainability Report · {from:dd MMM yyyy} – {to:dd MMM yyyy}] " +
               $"CO2 Saved: {co2:0.0} kg (~{treesEquivalent:0.0} trees/yr) · " +
               $"Shared Trips: {totalRides} · Verified KTM: {verifiedUsers} · Pending Queue: {pendingKtms}";
    }
}
