using System.ComponentModel;

namespace BarengIn.Desktop.Models;

public class RequestItem : INotifyPropertyChanged
{
    private string status = "Pending";

    public RequestItem(string passenger, string pickup, string faculty)
    {
        Passenger = passenger;
        Pickup = pickup;
        Faculty = faculty;
    }

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
