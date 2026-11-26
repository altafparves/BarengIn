using System.ComponentModel;
using BarengIn.Desktop.Logic;

namespace BarengIn.Desktop.Models;

public class AdminUserItem : INotifyPropertyChanged
{
    private string status;

    public AdminUserItem(
        string nim,
        string fullName,
        string role,
        string faculty,
        string ktmFile,
        string initialStatus = "Pending",
        int dbUserId = 0,
        Guid? userId = null)
    {
        Nim = nim;
        FullName = fullName;
        Role = role;
        Faculty = faculty;
        KtmFile = string.IsNullOrWhiteSpace(ktmFile) ? "ktm_card.jpg" : ktmFile;
        status = string.IsNullOrWhiteSpace(initialStatus) ? "Pending" : initialStatus;
        DbUserId = dbUserId;
        UserId = userId ?? Guid.NewGuid();
    }

    public Guid UserId { get; }
    public int DbUserId { get; }
    public string Nim { get; }
    public string FullName { get; }
    public string Role { get; }
    public string Faculty { get; }
    public string KtmFile { get; }
    public string Initials => Display.Initials(FullName);
    public string RoleFacultySummary => $"{Nim}  ·  {Role}  ·  {Faculty}";
    public string KtmSummary => $"KTM file: {KtmFile}";

    public string Status
    {
        get => status;
        set
        {
            if (status != value)
            {
                status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPending)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVerified)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuspended)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanSuspend)));
            }
        }
    }

    public bool IsPending => status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
    public bool IsVerified => status.Equals("Verified", StringComparison.OrdinalIgnoreCase);
    public bool IsSuspended => status.Equals("Suspended", StringComparison.OrdinalIgnoreCase);
    public bool CanSuspend => !IsSuspended && !Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;
}

