using System.ComponentModel;

namespace BarengIn.Desktop.Models;

public class RequestItem : INotifyPropertyChanged
{
    private string status = "Pending";

    public RequestItem(
        string passenger,
        string pickup,
        string faculty,
        Guid? requestId = null,
        int dbRequestId = 0,
        int dbRideId = 0,
        string initialStatus = "Pending")
    {
        Passenger = passenger;
        Pickup = pickup;
        Faculty = faculty;
        RequestId = requestId ?? Guid.NewGuid();
        DbRequestId = dbRequestId;
        DbRideId = dbRideId;
        status = string.IsNullOrWhiteSpace(initialStatus) ? "Pending" : initialStatus;
    }

    public Guid RequestId { get; }
    public int DbRequestId { get; }
    public int DbRideId { get; }
    public string Passenger { get; }
    public string Pickup { get; }
    public string Faculty { get; }

    public string Status
    {
        get => status;
        set
        {
            status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPending)));
        }
    }

    public bool IsPending => status == "Pending";

    public event PropertyChangedEventHandler? PropertyChanged;
}
