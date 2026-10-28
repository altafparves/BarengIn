using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;

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

        AppSession.SignIn(tbUsername.Text!, cbRole.SelectedIndex == 1 ? "Driver" : "Passenger");
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
