namespace BarengIn.Domain.Enums;

/// <summary>Review state of a user's KTM (student ID card) submission.</summary>
public enum VerificationStatus
{
    Unverified,
    Pending,
    Verified,
    Rejected
}
