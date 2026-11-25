using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using BarengIn.Desktop.Models;
using BarengIn.Desktop.Views;
using BarengIn.Infrastructure.Services;

namespace BarengIn.Desktop.Tests;

// Regenerates the PNGs used in the Module 7, Module 9, and Module 10 worksheets.
// Run with: BARENGIN_SCREENSHOTS=1 dotnet test --filter CaptureAllScreens
//       or: BARENGIN_M10_SCREENSHOTS=1 dotnet test --filter CaptureModule10Screens
[Collection("AppDbSerial")]
public class ScreenshotTests
{
    private static string RepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "BarengIn.sln")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir ?? ".";
    }

    private static string OutputDir(string moduleName = "module-9")
    {
        string path = Path.Combine(RepoRoot(), "docs", "modules", moduleName, "screenshots");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Module10OutputDir()
    {
        string path = Path.Combine(RepoRoot(), "docs", "module-10", "screenshots");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Shot(Window window, string name, string moduleName = "module-9")
    {
        ShotToDir(window, OutputDir(moduleName), name);
    }

    private static void ShotToDir(Window window, string outputDir, string name)
    {
        window.Show();
        window.Layout();
        window.CaptureRenderedFrame()!.Save(Path.Combine(outputDir, name + ".png"));
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

    [AvaloniaFact]
    public void CaptureModule10Screens()
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("BARENGIN_M10_SCREENSHOTS") == "1",
            "Set BARENGIN_M10_SCREENSHOTS=1 to regenerate Module 10 screenshots.");

        var prevEstimator = BarengIn.Desktop.AppDb.RouteEstimator;
        string? prevEnv = Environment.GetEnvironmentVariable("BARENGIN_DB");
        string m10Dir = Module10OutputDir();

        try
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", null);
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            BarengIn.Desktop.AppDb.RouteEstimator = (_, _, _) =>
                Task.FromResult(new RouteEstimate(5.4, 13, 0.65, IsLiveApi: true, Source: "OSRM + Nominatim"));

            AppSession.Reset();
            var register = new RegisterWindow();
            register.Find<TextBox>("tbName").Text = "Altaf Parves";
            register.Find<TextBox>("tbNim").Text = "24/536741";
            register.Find<ComboBox>("cbFaculty").SelectedIndex = 0;
            register.Find<TextBox>("tbPassword").Text = "secret123";
            register.Find<TextBox>("tbConfirm").Text = "secret123";
            ShotToDir(register, m10Dir, "01-register");

            var login = new LoginWindow();
            login.Find<ComboBox>("cbRole").SelectedIndex = 0;
            login.Find<TextBox>("tbUsername").Text = "24/536741";
            login.Find<TextBox>("tbPassword").Text = "secret123";
            ShotToDir(login, m10Dir, "02-login");

            AppSession.Reset();
            AppSession.SignIn("24/536741", "Passenger");
            AppSession.Name = "Altaf Parves";
            AppSession.Co2Saved = 1.2;
            var home = new HomeWindow();
            home.Show();
            home.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
            home.Click("btnFind");
            Dispatcher.UIThread.RunJobs();
            home.Find<ListBox>("lvRides").SelectedIndex = 1;
            ShotToDir(home, m10Dir, "03-passenger-home");

            AppSession.Reset();
            AppSession.SignIn("24/536741", "Driver");
            AppSession.Name = "Altaf Parves";
            var driver = new DriverWindow();
            driver.Show();
            driver.Find<TextBox>("tbOrigin").Text = "Seturan";
            driver.Click("btnPost");
            Dispatcher.UIThread.RunJobs();
            driver.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
            driver.Click("btnPost");
            Dispatcher.UIThread.RunJobs();
            driver.Layout();
            driver.FindButton("Approve").Press();
            driver.Layout();
            driver.FindButton("Reject").Press();
            ShotToDir(driver, m10Dir, "04-driver");

            AppSession.Faculty = "Engineering (FT)";
            AppSession.Hobbies = "badminton, coffee, indie music";
            AppSession.Bio = "Commutes from Jakal every day. Happy to chat or ride in silence.";
            var profile = new ProfileWindow();
            ShotToDir(profile, m10Dir, "05-profile");
        }
        finally
        {
            AppSession.Reset();
            BarengIn.Desktop.AppDb.RouteEstimator = prevEstimator;
            Environment.SetEnvironmentVariable("BARENGIN_DB", prevEnv);
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
        }
    }
}
