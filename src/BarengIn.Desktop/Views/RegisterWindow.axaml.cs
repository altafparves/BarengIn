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
        string? error = Validation.Register(
            tbName.Text ?? "", tbNim.Text ?? "", tbPassword.Text ?? "", tbConfirm.Text ?? "", ktmFile);
        if (error != null)
        {
            await MessageBox.Show(this, error);
            return;
        }

        AppSession.Reset();
        AppSession.Name = tbName.Text!.Trim();
        AppSession.Faculty = cbFaculty.SelectedItem as string ?? Faculties.All[0];
        AppSession.VerificationStatus = "Pending";

        await MessageBox.Show(this, "Account created. Your KTM is being verified. You can log in now.");
        Navigator.Go<LoginWindow>(this);
    }

    private void btnBack_Click(object? sender, RoutedEventArgs e)
    {
        Navigator.Go<LoginWindow>(this);
    }
}
