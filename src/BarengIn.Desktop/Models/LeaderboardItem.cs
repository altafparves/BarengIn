using BarengIn.Desktop.Logic;

namespace BarengIn.Desktop.Models;

public record LeaderboardItem(
    int Rank,
    string Name,
    string Nim,
    string Faculty,
    string Role,
    int SharedRides,
    double Co2SavedKg)
{
    public string RankBadge => $"#{Rank}";
    public string Initials => Display.Initials(Name);
    public string Subtitle => $"{Faculty}  ·  {Role}  ·  {SharedRides} shared rides";
    public string Co2Text => $"{Co2SavedKg:0.0} kg CO2";
}

