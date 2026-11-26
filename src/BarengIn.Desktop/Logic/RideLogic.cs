using BarengIn.Desktop.Models;

namespace BarengIn.Desktop.Logic;

public static class RideLogic
{
    public const double Co2PerRequest = 1.2;

    private static readonly (string Driver, string Pickup, int MinutesBefore, int Seats, string Cost, string Nim, string Hobbies, string Bio)[] Sample =
    {
        ("Reza Pratama", "Jakal Km 5", 30, 3, "Rp 5.000", "24/512301/TK/58101", "badminton, indie music, coffee", "Morning commute via Jakal, chill playlist!"),
        ("Bagas Saputra", "Seturan", 45, 2, "Rp 7.000", "24/514890/TK/58210", "photography, futsal, gaming", "Leaving on time from Seturan, helmets sanitized."),
        ("Nadia Kusuma", "Condongcatur", 60, 1, "Rp 6.000", "24/519204/TK/58432", "podcasts, UI design, cycling", "Quiet morning ride from Concat straight to FT."),
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
