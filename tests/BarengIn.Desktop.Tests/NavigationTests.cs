using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using BarengIn.Desktop.Models;
using BarengIn.Desktop.Views;

namespace BarengIn.Desktop.Tests;

public class NavigationTests
{
    private static LoginWindow NewLogin()
    {
        AppSession.Reset();
        var login = new LoginWindow();
        login.Show();
        Navigator.Current = login;
        return login;
    }

    private static T LogInAs<T>(int roleIndex) where T : Window
    {
        var login = NewLogin();
        login.Find<ComboBox>("cbRole").SelectedIndex = roleIndex;
        login.Find<TextBox>("tbUsername").Text = "24/536741";
        login.Find<TextBox>("tbPassword").Text = "secret";
        login.Click("btnLogin");
        return Assert.IsType<T>(Navigator.Current);
    }

    [AvaloniaFact]
    public void Login_AsPassenger_OpensHome()
    {
        var home = LogInAs<HomeWindow>(0);
        Assert.Contains("(Passenger)", home.Find<TextBlock>("lblWelcome").Text);
    }

    [AvaloniaFact]
    public void Login_AsDriver_OpensDriverScreen()
    {
        var driver = LogInAs<DriverWindow>(1);
        Assert.Contains("(Driver)", driver.Find<TextBlock>("lblWelcome").Text);
    }

    [AvaloniaFact]
    public void Login_WithEmptyFields_StaysOnLogin()
    {
        var login = NewLogin();
        login.Click("btnLogin");
        Assert.Same(login, Navigator.Current);
        Assert.True(login.IsVisible);
    }

    [AvaloniaFact]
    public void Login_CreateAccount_OpensRegister_AndBackReturns()
    {
        var login = NewLogin();
        login.Click("btnRegister");
        var register = Assert.IsType<RegisterWindow>(Navigator.Current);
        register.Click("btnBack");
        Assert.IsType<LoginWindow>(Navigator.Current);
    }

    [AvaloniaFact]
    public void Register_WithMismatchedPasswords_StaysOnRegister()
    {
        var login = NewLogin();
        login.Click("btnRegister");
        var register = Assert.IsType<RegisterWindow>(Navigator.Current);
        register.Find<TextBox>("tbName").Text = "Altaf";
        register.Find<TextBox>("tbNim").Text = "24/536741";
        register.Find<TextBox>("tbPassword").Text = "secret1";
        register.Find<TextBox>("tbConfirm").Text = "secret2";
        register.Click("btnRegister");
        Assert.Same(register, Navigator.Current);
    }

    [AvaloniaFact]
    public void Home_Profile_ThenBack_ReturnsToPassengerHome()
    {
        var home = LogInAs<HomeWindow>(0);
        home.Click("btnProfile");
        var profile = Assert.IsType<ProfileWindow>(Navigator.Current);
        profile.Click("btnBack");
        Assert.IsType<HomeWindow>(Navigator.Current);
    }

    [AvaloniaFact]
    public void Driver_Profile_ThenBack_ReturnsToDriverScreen()
    {
        var driver = LogInAs<DriverWindow>(1);
        driver.Click("btnProfile");
        var profile = Assert.IsType<ProfileWindow>(Navigator.Current);
        profile.Click("btnBack");
        Assert.IsType<DriverWindow>(Navigator.Current);
    }

    [AvaloniaFact]
    public void Logout_ReturnsToLogin_FromBothHomes()
    {
        var home = LogInAs<HomeWindow>(0);
        home.Click("btnLogout");
        Assert.IsType<LoginWindow>(Navigator.Current);

        var driver = LogInAs<DriverWindow>(1);
        driver.Click("btnLogout");
        Assert.IsType<LoginWindow>(Navigator.Current);
    }
}
