using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;
using BarengIn.Domain;

namespace BarengIn.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void btnLogin_Click(object? sender, RoutedEventArgs e)
    {
        string? error = Validation.Login(tbUsername.Text ?? "", tbPassword.Text ?? "");
        if (error != null)
        {
            await MessageBox.Show(this, error);
            return;
        }

        string selectedRole = cbRole.SelectedIndex == 1 ? "Driver" : "Passenger";
        User? user;
        try
        {
            user = AppDb.AuthenticateOrFallback(tbUsername.Text!.Trim(), tbPassword.Text!, selectedRole);
        }
        catch (Exception ex)
        {
            await MessageBox.Show(this, "Database error: " + ex.Message);
            return;
        }

        if (user == null)
        {
            await MessageBox.Show(this, "NIM or password is wrong.");
            return;
        }

        // Polymorphism: same call, different result depending on the real class.
        AppSession.SignIn(user);
        Navigator.GoHome(this);
    }

    private void btnClose_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void btnRegister_Click(object? sender, RoutedEventArgs e)
    {
        Navigator.Go<RegisterWindow>(this);
    }
}
