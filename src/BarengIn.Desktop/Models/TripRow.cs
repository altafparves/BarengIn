using System.ComponentModel;

namespace BarengIn.Desktop.Models;

public class TripRow : INotifyPropertyChanged
{
    private int seats;

    public TripRow(string pickup, string faculty, string departs, int seats, string cost, Guid? tripId = null)
    {
        Pickup = pickup;
        Faculty = faculty;
        Departs = departs;
        this.seats = seats;
        Cost = cost;
        TripId = tripId ?? Guid.NewGuid();
    }

    public Guid TripId { get; }
    public string Pickup { get; }
    public string Faculty { get; }
    public string Departs { get; }
    public string Cost { get; }

    public int Seats
    {
        get => seats;
        set
        {
            if (seats != value)
            {
                seats = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seats)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
