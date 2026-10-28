namespace BarengIn.Desktop.Models;

public static class AppSession
{
    public static string Nim { get; private set; } = "";
    public static string Name { get; set; } = "";
    public static string Role { get; private set; } = "Passenger";
    public static string Faculty { get; set; } = Faculties.All[0];
    public static string Hobbies { get; set; } = "";
    public static string Bio { get; set; } = "";
    public static string VerificationStatus { get; set; } = "Verified";
    public static double Co2Saved { get; set; }

    public static void SignIn(string nim, string role)
    {
        Nim = nim.Trim();
        Role = role;
        if (Name == "")
        {
            Name = Nim;
        }
    }

    public static void Reset()
    {
        Nim = "";
        Name = "";
        Role = "Passenger";
        Faculty = Faculties.All[0];
        Hobbies = "";
        Bio = "";
        VerificationStatus = "Verified";
        Co2Saved = 0;
    }
}
