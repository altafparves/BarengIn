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

    /// <summary>An admin moderates the platform; they never drive.</summary>
    public override bool CanPublishTrip() => false;

    public override string GetRoleName() => "Admin";

    /// <summary>Not implemented here: resolving <paramref name="userId"/> to a User needs a repository lookup.</summary>
    public bool VerifyKtm(Guid userId, VerificationStatus status) =>
        throw new NotImplementedException("Requires a User repository lookup by id, resolved in the Application layer.");

    /// <summary>
    /// Not implemented here: the diagram gives User no suspension flag to set, and resolving
    /// <paramref name="userId"/> needs a repository lookup.
    /// </summary>
    public bool SuspendUser(Guid userId, string reason) =>
        throw new NotImplementedException(
            "Requires a User repository lookup by id and a suspension flag, both Infrastructure/Application concerns.");

    /// <summary>Not implemented here: an emission dashboard needs an aggregate query across all trips in the range.</summary>
    public double ViewEmissionDashboard(DateTime from, DateTime to) =>
        throw new NotImplementedException("Requires a Trip repository aggregate query, resolved in the Application layer.");

    /// <summary>Not implemented here: a report needs an aggregate query across trips and users in the range.</summary>
    public string GenerateReport(DateTime from, DateTime to) =>
        throw new NotImplementedException("Requires repository aggregate queries, resolved in the Application layer.");
}
