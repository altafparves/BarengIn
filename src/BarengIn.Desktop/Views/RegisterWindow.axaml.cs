using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;

namespace BarengIn.Desktop.Views;

public partial class RegisterWindow : Window
{
    private string ktmFile = "";

    public RegisterWindow()
    {
        InitializeComponent();
        cbFaculty.ItemsSource = Faculties.All;
        cbFaculty.SelectedIndex = 0;
    }

    private async void btnKtm_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose your KTM photo",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll },
        });

        if (files.Count > 0)
        {
            ktmFile = files[0].Name;
            lblKtm.Text = ktmFile;
        }
    }

    private async void btnRegister_Click(object? sender, RoutedEventArgs e)
    {
        string name = (tbName.Text ?? "").Trim();
        string nim = (tbNim.Text ?? "").Trim();
        string email = (tbEmail.Text ?? "").Trim();
        string phone = (tbPhone.Text ?? "").Trim();

        string? error = string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone)
            ? Validation.Register(tbName.Text ?? "", tbNim.Text ?? "", tbPassword.Text ?? "", tbConfirm.Text ?? "", ktmFile)
            : Validation.Register(tbName.Text ?? "", tbNim.Text ?? "", tbPassword.Text ?? "", tbConfirm.Text ?? "", ktmFile, email, phone);
        if (error != null)
        {
            await MessageBox.Show(this, error);
            return;
        }

        string effectiveEmail = string.IsNullOrEmpty(email) ? nim + "@mail.ugm.ac.id" : email;
        string effectivePhone = string.IsNullOrEmpty(phone) ? "0812" + Math.Abs(nim.GetHashCode() % 100000000).ToString("D8") : phone;
        string faculty = cbFaculty.SelectedItem as string ?? Faculties.All[0];
        string role = cbRole.SelectedIndex == 1 ? "driver" : "passenger";

        try
        {
            bool ok = AppDb.RegisterOrFallback(
                name,
                effectiveEmail,
                effectivePhone,
                tbPassword.Text!,
                nim,
                faculty,
                role);
            if (!ok)
            {
                await MessageBox.Show(this, "Registration failed.");
                return;
            }
        }
        catch (Exception ex)
        {
            await MessageBox.Show(this, "Database error: " + ex.Message);
            return;
        }

        AppSession.Reset();
        AppSession.Name = name;
        AppSession.Faculty = faculty;
        AppSession.VerificationStatus = "Pending";

        await MessageBox.Show(this, "Account created. Your KTM is being verified. You can log in now.");
        Navigator.Go<LoginWindow>(this);
    }

    private void btnBack_Click(object? sender, RoutedEventArgs e)
    {
        Navigator.Go<LoginWindow>(this);
    }
}
