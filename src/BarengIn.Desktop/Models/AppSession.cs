using BarengIn.Domain;
using BarengIn.Infrastructure.Persistence;

namespace BarengIn.Desktop.Models;

public static class AppSession
{
    public static User? CurrentUser { get; private set; }
    public static string Nim { get; private set; } = "";
    public static string Name { get; set; } = "";
    public static string Role { get; private set; } = "Passenger";
    public static string Faculty { get; set; } = Faculties.All[0];
    public static string Hobbies { get; set; } = "";
    public static string Bio { get; set; } = "";
    public static string VerificationStatus { get; set; } = "Verified";
    public static double Co2Saved { get; set; }

    public static void SignIn(User user)
    {
        CurrentUser = user;
        Nim = user.Niu.Trim();
        Role = user.GetRoleName();
        if (string.IsNullOrWhiteSpace(Name))
        {
            Name = string.IsNullOrWhiteSpace(user.FullName) ? Nim : user.FullName;
        }
    }

    public static void SignIn(string nim, string role)
    {
        Nim = nim.Trim();
        Role = role;
        if (Name == "")
        {
            Name = Nim;
        }

        string safeNim = string.IsNullOrWhiteSpace(Nim) ? "24/536741" : Nim;
        string safeName = string.IsNullOrWhiteSpace(Name) ? safeNim : Name;
        CurrentUser = UserRepository.BuildUserForRole(
            safeNim,
            safeName,
            safeNim + "@mail.ugm.ac.id",
            "session-hash",
            safeNim,
            role);
    }

    public static void Reset()
    {
        CurrentUser = null;
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
