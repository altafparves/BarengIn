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
    private readonly ObservableCollection<TripRow> trips = new();
    private readonly Driver activeDriver;
    private readonly Trip defaultIncomingTrip;

    public DriverWindow()
    {
        InitializeComponent();
        lblWelcome.Text = "Hi, " + AppSession.Name + " (" + AppSession.Role + ")";
        cbFaculty.ItemsSource = Faculties.All;
        cbFaculty.SelectedItem = AppSession.Faculty;
        lvTrips.ItemsSource = trips;

        activeDriver = AppSession.CurrentUser as Driver
            ?? (Driver)BarengIn.Infrastructure.Persistence.UserRepository.BuildUserForRole(
                string.IsNullOrWhiteSpace(AppSession.Nim) ? "24/536741" : AppSession.Nim,
                string.IsNullOrWhiteSpace(AppSession.Name) ? "Driver" : AppSession.Name,
                "driver@mail.ugm.ac.id",
                "hash",
                "08123456789",
                "Driver");

        // Seed an active domain Trip to hold the incoming RideRequests so Approve/Reject run through Domain OOP logic.
        defaultIncomingTrip = new Trip(
            activeDriver.UserId,
            activeDriver.Vehicles[0].VehicleId,
            "Jakal Km 5",
            new GeoPoint(-7.75, 110.38),
            Guid.Empty,
            DateTime.Today.AddHours(7.5),
            availableSeats: 3,
            distanceKm: 5.0,
            polylineJson: string.Empty);
        activeDriver.RecordPublishedTrip(defaultIncomingTrip);

        var req1 = new RideRequest(defaultIncomingTrip.TripId, Guid.NewGuid(), "Jakal Km 5", new GeoPoint(-7.75, 110.38), "Morning class");
        var req2 = new RideRequest(defaultIncomingTrip.TripId, Guid.NewGuid(), "Condongcatur", new GeoPoint(-7.76, 110.40), "Morning class");
        var req3 = new RideRequest(defaultIncomingTrip.TripId, Guid.NewGuid(), "Seturan", new GeoPoint(-7.77, 110.41), "Morning class");
        defaultIncomingTrip.ReceiveRequest(req1);
        defaultIncomingTrip.ReceiveRequest(req2);
        defaultIncomingTrip.ReceiveRequest(req3);

        lvRequests.ItemsSource = new ObservableCollection<RequestItem>
        {
            new("Rani Putri", req1.PickupAddress, "MIPA", req1.RequestId),
            new("Budi Santoso", req2.PickupAddress, "Engineering (FT)", req2.RequestId),
            new("Sari Dewi", req3.PickupAddress, "Medicine", req3.RequestId),
        };
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

        if (AppDb.IsDatabaseConfigured)
        {
            try
            {
                AppDb.Rides.EnsureSeedData(Faculties.All);
                AppDb.Rides.InsertRide(AppSession.Nim, origin, faculty, DateTime.Today.Add(time), seats, cost);
            }
            catch
            {
                // Keep UI responsive if DB insert fails.
            }
        }

        trips.Add(new TripRow(
            trip.OriginAddress,
            faculty,
            time.ToString(@"hh\:mm"),
            trip.AvailableSeats,
            Display.Rupiah(cost)));
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
