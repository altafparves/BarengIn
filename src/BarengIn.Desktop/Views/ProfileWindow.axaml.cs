using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;

namespace BarengIn.Desktop.Views;

public partial class ProfileWindow : Window
{
    public ProfileWindow()
    {
        InitializeComponent();
        lblInitials.Text = Display.Initials(AppSession.Name);
        lblName.Text = AppSession.Name;
        lblNimRole.Text = AppSession.Nim + "  ·  " + AppSession.Role;
        cbFaculty.ItemsSource = Faculties.All;
        cbFaculty.SelectedItem = AppSession.Faculty;
        tbHobbies.Text = AppSession.Hobbies;
        tbBio.Text = AppSession.Bio;

        bool verified = AppSession.VerificationStatus == "Verified";
        lblStatus.Text = verified ? "KTM verified" : "KTM pending";
        lblStatus.Foreground = new SolidColorBrush(Color.Parse(verified ? "#14573B" : "#7A4B00"));
        badgeStatus.Background = new SolidColorBrush(Color.Parse(verified ? "#DDF3E8" : "#FFEBC2"));
    }

    private async void btnSave_Click(object? sender, RoutedEventArgs e)
    {
        AppSession.Faculty = cbFaculty.SelectedItem as string ?? AppSession.Faculty;
        AppSession.Hobbies = (tbHobbies.Text ?? "").Trim();
        AppSession.Bio = (tbBio.Text ?? "").Trim();
        await MessageBox.Show(this, "Profile saved.");
    }

    private void btnBack_Click(object? sender, RoutedEventArgs e)
    {
        Navigator.GoHome(this);
    }
}
