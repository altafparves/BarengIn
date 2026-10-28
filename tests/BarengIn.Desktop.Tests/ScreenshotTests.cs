using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using BarengIn.Desktop.Models;
using BarengIn.Desktop.Views;

namespace BarengIn.Desktop.Tests;

// Regenerates the PNGs used in the Module 7 worksheet.
// Run with: BARENGIN_SCREENSHOTS=1 dotnet test --filter ScreenshotTests
public class ScreenshotTests
{
    private static string OutputDir()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "BarengIn.sln")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        string path = Path.Combine(dir ?? ".", "docs", "modules", "module-7", "screenshots");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Shot(Window window, string name)
    {
        window.Show();
        window.Layout();
        window.CaptureRenderedFrame()!.Save(Path.Combine(OutputDir(), name + ".png"));
    }

    [AvaloniaFact]
    public void CaptureAllScreens()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("BARENGIN_SCREENSHOTS") == "1",
            "Set BARENGIN_SCREENSHOTS=1 to regenerate screenshots.");

        AppSession.Reset();
        var login = new LoginWindow();
        login.Find<TextBox>("tbUsername").Text = "24/536741";
        login.Find<TextBox>("tbPassword").Text = "secret";
        Shot(login, "01-login");

        var register = new RegisterWindow();
        register.Find<TextBox>("tbName").Text = "Altaf Parves";
        register.Find<TextBox>("tbNim").Text = "24/536741";
        Shot(register, "02-register");

        AppSession.Reset();
        AppSession.SignIn("24/536741", "Passenger");
        AppSession.Name = "Altaf Parves";
        var home = new HomeWindow();
        home.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
        home.Click("btnFind");
        home.Find<ListBox>("lvRides").SelectedIndex = 1;
        Shot(home, "03-passenger-home");

        AppSession.Reset();
        AppSession.SignIn("24/536741", "Driver");
        AppSession.Name = "Altaf Parves";
        var driver = new DriverWindow();
        driver.Show();
        driver.Find<TextBox>("tbOrigin").Text = "Seturan";
        driver.Click("btnPost");
        driver.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
        driver.Click("btnPost");
        driver.Layout();
        driver.FindButton("Approve").Press();
        driver.Layout();
        driver.FindButton("Reject").Press();
        Shot(driver, "04-driver");

        AppSession.Hobbies = "badminton, coffee, indie music";
        AppSession.Bio = "Commutes from Jakal every day. Happy to chat or ride in silence.";
        var profile = new ProfileWindow();
        Shot(profile, "05-profile");
    }
}
