namespace BarengIn.Desktop.Logic;

public static class Validation
{
    public static string? Login(string nim, string password)
    {
        if (string.IsNullOrWhiteSpace(nim) || string.IsNullOrEmpty(password))
        {
            return "Please enter your NIM and password.";
        }
        return null;
    }

    public static string? Register(string name, string nim, string password, string confirm, string ktmFile)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Please enter your full name.";
        if (string.IsNullOrWhiteSpace(nim)) return "Please enter your NIM.";
        if (password.Length < 6) return "Password must be at least 6 characters.";
        if (password != confirm) return "Passwords do not match.";
        if (string.IsNullOrWhiteSpace(ktmFile)) return "Please attach a photo of your KTM.";
        return null;
    }
}
