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

        string selectedRole = cbRole.SelectedIndex switch
        {
            1 => "Driver",
            2 => "Admin",
            _ => "Passenger",
        };
        string faculty;
        string hobbies;
        string bio;
        User? user;
        try
        {
            user = AppDb.AuthenticateOrFallback(
                tbUsername.Text!.Trim(),
                tbPassword.Text!,
                selectedRole,
                out faculty,
                out hobbies,
                out bio);
        }
        catch (Exception ex)
        {
            await MessageBox.Show(this, "Database error: " + ex.Message);
            return;
        }

        if (user == null)
        {
            string denyMessage = selectedRole == "Admin" && !BarengIn.Infrastructure.Persistence.UserRepository.IsDeclaredAdminIdentifier(tbUsername.Text)
                ? "Access denied: Only declared BarengIn Administrators (e.g. admin@ugm.ac.id) can log in as Admin."
                : "NIM/Email or password is wrong.";
            await MessageBox.Show(this, denyMessage);
            return;
        }

        // Polymorphism: same call, different result depending on the real class.
        AppSession.SignIn(user, faculty, hobbies, bio);
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
