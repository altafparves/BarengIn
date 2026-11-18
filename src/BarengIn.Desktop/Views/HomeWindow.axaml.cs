using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;
using BarengIn.Domain;
using BarengIn.Domain.ValueObjects;

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
        if (AppDb.IsAvailable)
        {
            var dbRides = AppDb.Rides.FindRides(faculty)
                .Select(r => new RideRow(r.DriverName, r.Faculty, r.Departs, r.Seats, Display.Rupiah((int)r.Cost), r.RideId))
                .ToList();
            if (dbRides.Count > 0)
            {
                lvRides.ItemsSource = dbRides;
                return;
            }
        }

        lvRides.ItemsSource = RideLogic.Find(faculty, dtpTime.SelectedTime ?? TimeSpan.Zero);
    }

    private async void btnRequest_Click(object? sender, RoutedEventArgs e)
    {
        if (lvRides.SelectedItem is not RideRow ride)
        {
            await MessageBox.Show(this, "Please select a ride first.");
            return;
        }

        string pickup = string.IsNullOrWhiteSpace(tbOrigin.Text) ? "Jakal Km 5" : tbOrigin.Text!.Trim();
        if (AppSession.CurrentUser is Passenger passenger)
        {
            var req = new RideRequest(Guid.NewGuid(), passenger.UserId, pickup, new GeoPoint(-7.75, 110.38), "Joining ride");
            passenger.RecordSubmittedRequest(req);
        }

        if (AppDb.IsAvailable && ride.RideId > 0)
        {
            AppDb.Rides.InsertRequest(ride.RideId, AppSession.Nim, pickup);
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
