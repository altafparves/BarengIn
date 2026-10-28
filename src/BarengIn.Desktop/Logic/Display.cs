using System.Globalization;

namespace BarengIn.Desktop.Logic;

public static class Display
{
    public static string Rupiah(int amount)
    {
        return "Rp " + amount.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".");
    }

    public static string Co2(double kg)
    {
        return "Estimated CO2 saved: " + kg.ToString("0.0", CultureInfo.InvariantCulture) + " kg";
    }

    public static string Initials(string name)
    {
        var letters = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => char.IsLetter(w[0]))
            .Take(2)
            .Select(w => char.ToUpperInvariant(w[0]));
        string result = string.Concat(letters);
        return result == "" ? "U" : result;
    }
}
