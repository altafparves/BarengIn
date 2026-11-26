using BarengIn.Domain.Enums;

namespace BarengIn.Domain;

/// <summary>Base type for every account in the system: passengers, drivers, and admins.</summary>
public abstract class User
{
    protected Guid userId;
    protected string niu;
    protected string fullName;
    protected string email;
    protected string passwordHash;
    protected string phoneNumber;
    protected string ktmImageUrl;
    protected VerificationStatus verificationStatus;
    protected Guid facultyId;
    protected string hobbies;
    protected string bio;
    protected DateTime createdAt;

    /// <summary>Backs the "User owns ClassSchedule" composition; not an attribute in the diagram.</summary>
    private readonly List<ClassSchedule> classSchedules = [];

    protected User(
        string niu,
        string fullName,
        string email,
        string passwordHash,
        string phoneNumber,
        Guid facultyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(niu);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        userId = Guid.NewGuid();
        this.niu = niu;
        this.fullName = fullName;
        this.email = email;
        this.passwordHash = passwordHash;
        this.phoneNumber = phoneNumber;
        this.facultyId = facultyId;
        ktmImageUrl = string.Empty;
        verificationStatus = VerificationStatus.Unverified;
        hobbies = string.Empty;
        bio = string.Empty;
        createdAt = DateTime.UtcNow;
    }

    /// <summary>Identity, exposed read-only from the protected userId field.</summary>
    public Guid UserId => userId;

    /// <summary>Student ID (NIU/NIM), exposed read-only from the protected niu field.</summary>
    public string Niu => niu;

    /// <summary>Full name, exposed read-only from the protected fullName field.</summary>
    public string FullName => fullName;

    /// <summary>Email address, exposed read-only from the protected email field.</summary>
    public string Email => email;

    /// <summary>Phone number, exposed read-only from the protected phoneNumber field.</summary>
    public string PhoneNumber => phoneNumber;

    /// <summary>Hobbies, exposed read-only from the protected hobbies field.</summary>
    public string Hobbies => hobbies;

    /// <summary>Short bio, exposed read-only from the protected bio field.</summary>
    public string Bio => bio;

    /// <summary>Current KTM verification status, exposed read-only from the protected verificationStatus field.</summary>
    public VerificationStatus Status => verificationStatus;

    /// <summary>Whether the user account has been suspended by an administrator.</summary>
    public bool IsSuspended { get; private set; }

    /// <summary>Reason recorded when an administrator suspends the account.</summary>
    public string SuspensionReason { get; private set; } = string.Empty;

    /// <summary>Class schedules this user owns, exposed read-only.</summary>
    public IReadOnlyList<ClassSchedule> ClassSchedules => classSchedules;

    /// <summary>
    /// Not implemented here: verifying credentials needs a password hashing algorithm, which is
    /// an external dependency the Domain layer cannot reference. Belongs to Infrastructure/Identity.
    /// </summary>
    public bool Login(string email, string password) =>
        throw new NotImplementedException(
            "Requires a password hashing algorithm, which is an Infrastructure/Identity concern.");

    /// <summary>
    /// Not implemented here: ending a session (invalidating a token) is an Infrastructure/Identity
    /// concern; the Domain layer models no session state to end.
    /// </summary>
    public void Logout() =>
        throw new NotImplementedException("Session/token invalidation is an Infrastructure/Identity concern.");

    public bool UpdateProfile(string name, string phone, string bio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        fullName = name;
        phoneNumber = phone;
        this.bio = bio ?? string.Empty;
        return true;
    }

    public bool UploadKtm(string imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);

        ktmImageUrl = imageUrl;
        verificationStatus = VerificationStatus.Pending;
        return true;
    }

    public bool IsVerified() => verificationStatus == VerificationStatus.Verified && !IsSuspended;

    /// <summary>Marks this user's KTM as verified (encapsulated state change on protected field).</summary>
    public void MarkVerified() => verificationStatus = VerificationStatus.Verified;

    /// <summary>Updates the user's KTM verification status.</summary>
    public void SetVerificationStatus(VerificationStatus status) => verificationStatus = status;

    /// <summary>Suspends the user account with an administrative reason.</summary>
    public void Suspend(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        IsSuspended = true;
        SuspensionReason = reason.Trim();
        verificationStatus = VerificationStatus.Rejected;
    }

    /// <summary>Lifts an administrative suspension from the user account.</summary>
    public void Unsuspend()
    {
        IsSuspended = false;
        SuspensionReason = string.Empty;
    }

    public abstract bool CanPublishTrip();

    public abstract string GetRoleName();

    /// <summary>
    /// Adds a class schedule this user owns. Not a diagram method: it exists to make the
    /// "User owns ClassSchedule" composition a real collection instead of an unreachable one.
    /// </summary>
    public void AddClassSchedule(ClassSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        classSchedules.Add(schedule);
    }
}
