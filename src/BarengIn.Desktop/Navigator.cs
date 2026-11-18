using Avalonia.Controls;
using BarengIn.Desktop.Models;
using BarengIn.Desktop.Views;

namespace BarengIn.Desktop;

public static class Navigator
{
    public static Window? Current { get; set; }

    public static void Go<T>(Window from) where T : Window, new()
    {
        var next = new T();
        Current = next;
        next.Show();
        from.Close();
    }

    public static void GoHome(Window from)
    {
        if (AppSession.Role == "Driver")
        {
            Go<DriverWindow>(from);
        }
        else
        {
            Go<HomeWindow>(from);
        }
    }

    public static void Logout(Window from)
    {
        AppSession.Reset();
        Go<LoginWindow>(from);
    }
}
