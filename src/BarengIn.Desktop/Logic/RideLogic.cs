using BarengIn.Desktop.Models;

namespace BarengIn.Desktop.Logic;

public static class RideLogic
{
    public const double Co2PerRequest = 1.2;

    private static readonly (string Driver, int MinutesBefore, int Seats, string Cost)[] Sample =
    {
        ("Altaf", 30, 3, "Rp 5.000"),
        ("Dimas", 45, 2, "Rp 7.000"),
        ("Diffie", 60, 1, "Rp 6.000"),
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
            rides.Add(new RideRow(s.Driver, faculty, Departure(classStart, s.MinutesBefore), s.Seats, s.Cost));
        }
        return rides;
    }
}
