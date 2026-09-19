using BarengIn.Domain.Enums;

namespace BarengIn.Domain.Entities;

/// <summary>
/// Base type for everyone with an account. Credentials are deliberately absent: password
/// hashing is ASP.NET Core Identity's responsibility and lives in the infrastructure layer,
/// so no plaintext or hashed secret is ever modelled here.
/// </summary>
public abstract class User
{
    protected User(
        string email,
        string fullName,
        Faculty faculty,
        string department,
        string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentNullException.ThrowIfNull(faculty);
        ArgumentException.ThrowIfNullOrWhiteSpace(department);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        Id = Guid.NewGuid();
        Email = email;
        FullName = fullName;
        Faculty = faculty;
        Department = department;
        PhoneNumber = phoneNumber;
        VerificationStatus = VerificationStatus.PendingVerification;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string FullName { get; private set; }

    public Faculty Faculty { get; private set; }

    public string Department { get; private set; }

    public string PhoneNumber { get; private set; }

    public VerificationStatus VerificationStatus { get; private set; }

    /// <summary>Storage key of the uploaded student ID card image; null until one is submitted.</summary>
    public string? KtmImageReference { get; private set; }

    /// <summary>Whether this user may publish a trip and drive other students.</summary>
    public abstract bool CanPublishTrip();

    /// <summary>Whether this user may ask to join someone else's trip.</summary>
    public abstract bool CanRequestRide();

    /// <summary>Attaches a student ID image and puts the account back in the review queue.</summary>
    public void SubmitKtm(string imageReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageReference);

        KtmImageReference = imageReference;
        VerificationStatus = VerificationStatus.PendingVerification;
    }

    public void Verify()
    {
        if (KtmImageReference is null)
        {
            throw new InvalidOperationException(
                "Cannot verify a user who has not submitted a student ID image.");
        }

        VerificationStatus = VerificationStatus.Verified;
    }

    public void RejectVerification() => VerificationStatus = VerificationStatus.Rejected;

    public void UpdateContactDetails(string fullName, string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        FullName = fullName;
        PhoneNumber = phoneNumber;
    }

    public void MoveToFaculty(Faculty faculty, string department)
    {
        ArgumentNullException.ThrowIfNull(faculty);
        ArgumentException.ThrowIfNullOrWhiteSpace(department);

        Faculty = faculty;
        Department = department;
    }
}
