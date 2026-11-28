using BarengIn.Desktop.Models;

namespace BarengIn.Desktop.Logic;

public static class RideLogic
{
    public const double Co2PerRequest = 1.2;

    private static readonly (string Driver, string Pickup, int MinutesBefore, int Seats, string Cost, string Nim, string Hobbies, string Bio)[] Sample =
    {
        ("Satrio", "Jakal Km 5.5", 30, 3, "Rp 5.000", "24/510001/TK/58001", "badminton, indie music, coffee", "Driving red Mazda 6 from Jakal Km 5.5 to FT."),
        ("Nalen", "Seturan", 45, 4, "Rp 6.000", "24/510002/EK/58002", "podcasts, running, culinary", "Commuting in Toyota Fortuner from Seturan to FEB."),
        ("Raka Wijaya", "Pogung Dalangan", 60, 1, "Rp 4.000", "24/510003/KU/58003", "basketball, photography", "Quiet morning ride from Pogung to campus."),
    };

    public static string Departure(TimeSpan classStart, int minutesBefore)
    {
        long day = TimeSpan.FromHours(24).Ticks;
        long ticks = (classStart - TimeSpan.FromMinutes(minutesBefore)).Ticks;
        TimeSpan t = new TimeSpan(((ticks % day) + day) % day);
        return $"{t.Hours:00}:{t.Minutes:00}";
    }

    public static List<RideRow> Find(string faculty, TimeSpan classStart)
    {
        var rides = new List<RideRow>();
        foreach (var s in Sample)
        {
            rides.Add(new RideRow(
                s.Driver,
                faculty,
                Departure(classStart, s.MinutesBefore),
                s.Seats,
                s.Cost,
                0,
                s.Pickup,
                s.Nim,
                s.Hobbies,
                s.Bio));
        }
        return rides;
    }
}
