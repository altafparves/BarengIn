using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;
using BarengIn.Domain;
using BarengIn.Domain.ValueObjects;

namespace BarengIn.Desktop.Views;

public partial class DriverWindow : Window
{
    private readonly ObservableCollection<TripRow> trips;
    private readonly Driver activeDriver;

    public DriverWindow()
    {
        InitializeComponent();
        lblWelcome.Text = "Hi, " + AppSession.Name + " (" + AppSession.Role + ")";
        cbFaculty.ItemsSource = Faculties.All;
        cbFaculty.SelectedItem = AppSession.Faculty;

        if (AppSession.CurrentUser is not Driver)
        {
            AppSession.SignIn(
                string.IsNullOrWhiteSpace(AppSession.Nim) ? "24/536741" : AppSession.Nim,
                "Driver");
        }

        activeDriver = (Driver)AppSession.CurrentUser!;
        trips = AppSession.DriverTrips;
        lvTrips.ItemsSource = trips;
        lvRequests.ItemsSource = AppSession.DriverRequests;
    }

    private async void btnPost_Click(object? sender, RoutedEventArgs e)
    {
        string origin = (tbOrigin.Text ?? "").Trim();
        if (origin == "")
        {
            await MessageBox.Show(this, "Please enter your pickup area.");
            return;
        }

        // Polymorphism check: User.CanPublishTrip() is overridden on Driver vs Passenger.
        if (AppSession.CurrentUser is not null && !AppSession.CurrentUser.CanPublishTrip())
        {
            await MessageBox.Show(this, "Only verified drivers with a vehicle can publish a trip.");
            return;
        }

        TimeSpan time = dtpTime.SelectedTime ?? TimeSpan.Zero;
        string faculty = cbFaculty.SelectedItem as string ?? Faculties.All[0];
        int seats = (int)(nudSeats.Value ?? 1);
        int cost = (int)(nudCost.Value ?? 0);

        // Domain Encapsulation: Driver.PublishNewTrip constructs Trip and adds to private publishedTrips.
        var trip = activeDriver.PublishNewTrip(
            activeDriver.Vehicles[0].VehicleId,
            origin,
            new GeoPoint(-7.75, 110.38),
            Guid.Empty,
            DateTime.Today.Add(time),
            seats,
            cost);

        if (AppDb.IsAvailable)
        {
            AppDb.Rides.EnsureSeedData(Faculties.All);
            AppDb.Rides.InsertRide(AppSession.Nim, origin, faculty, DateTime.Today.Add(time), seats, cost);
        }

        trips.Add(new TripRow(
            trip.OriginAddress,
            faculty,
            time.ToString(@"hh\:mm"),
            trip.AvailableSeats,
            Display.Rupiah(cost),
            trip.TripId));
        tbOrigin.Text = "";
    }

    private void btnApprove_Click(object? sender, RoutedEventArgs e)
    {
        if (((Button)sender!).DataContext is RequestItem item)
        {
            // Domain Encapsulation: Driver.ApproveRequest approves the RideRequest and decrements Trip.AvailableSeats.
            if (activeDriver.ApproveRequest(item.RequestId))
            {
                item.Status = "Approved";

                // Sync the decremented seat count to the visible TripRow in lvTrips.
                var targetRow = trips.FirstOrDefault();
                if (targetRow is not null)
                {
                    var targetTrip = activeDriver.PublishedTrips.FirstOrDefault(t => t.TripId == targetRow.TripId);
                    if (targetTrip is not null && targetTrip != AppSession.SeededDriverTrip)
                    {
                        targetTrip.AddPassenger(Guid.NewGuid());
                        targetRow.Seats = targetTrip.AvailableSeats;
                    }
                    else if (AppSession.SeededDriverTrip is not null)
                    {
                        targetRow.Seats = AppSession.SeededDriverTrip.AvailableSeats;
                    }
                }
            }
        }
    }

    private void btnReject_Click(object? sender, RoutedEventArgs e)
    {
        if (((Button)sender!).DataContext is RequestItem item)
        {
            // Domain Encapsulation: Driver.RejectRequest transitions RideRequest status to Rejected.
            if (activeDriver.RejectRequest(item.RequestId, "Declined by driver"))
            {
                item.Status = "Rejected";
            }
        }
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
