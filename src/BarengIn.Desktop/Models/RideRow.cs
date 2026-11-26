namespace BarengIn.Desktop.Models;

public record RideRow(
    string Driver,
    string Faculty,
    string Departs,
    int Seats,
    string Cost,
    int RideId = 0,
    string Pickup = "Jakal Km 5",
    string DriverNim = "",
    string DriverHobbies = "",
    string DriverBio = "")
{
    public string DriverProfileSummary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(DriverNim))
            {
                parts.Add($"NIM: {DriverNim}");
            }
            if (!string.IsNullOrWhiteSpace(DriverHobbies))
            {
                parts.Add($"Hobbies: {DriverHobbies}");
            }
            if (!string.IsNullOrWhiteSpace(DriverBio))
            {
                parts.Add($"\"{DriverBio}\"");
            }
            return string.Join("  ·  ", parts);
        }
    }

    public bool HasDriverProfile => !string.IsNullOrWhiteSpace(DriverProfileSummary);
}
