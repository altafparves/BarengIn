using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;

namespace BarengIn.Desktop.Views;

public partial class HomeWindow : Window
{
    public HomeWindow()
    {
        InitializeComponent();
        lblWelcome.Text = "Hi, " + AppSession.Name + " (" + AppSession.Role + ")";
        cbFaculty.ItemsSource = Faculties.All;
        cbFaculty.SelectedItem = AppSession.Faculty;
        UpdateCo2Label();
    }

    private void UpdateCo2Label()
    {
        lblCo2.Text = Display.Co2(AppSession.Co2Saved);
    }

    private async void btnFind_Click(object? sender, RoutedEventArgs e)
    {
        if ((tbOrigin.Text ?? "").Trim() == "")
        {
            await MessageBox.Show(this, "Please enter your pickup area.");
            return;
        }

        string faculty = cbFaculty.SelectedItem as string ?? Faculties.All[0];
        lvRides.ItemsSource = RideLogic.Find(faculty, dtpTime.SelectedTime ?? TimeSpan.Zero);
    }

    private async void btnRequest_Click(object? sender, RoutedEventArgs e)
    {
        if (lvRides.SelectedItem is not RideRow ride)
        {
            await MessageBox.Show(this, "Please select a ride first.");
            return;
        }

        AppSession.Co2Saved += RideLogic.Co2PerRequest;
        UpdateCo2Label();
        await MessageBox.Show(this, "Request sent to " + ride.Driver + ". Waiting for driver approval.");
    }

    private void btnProfile_Click(object? sender, RoutedEventArgs e)
    {
        Navigator.Go<ProfileWindow>(this);
    }

    private void btnLogout_Click(object? sender, RoutedEventArgs e)
    {
        Navigator.Logout(this);
    }
}
