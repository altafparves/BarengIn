using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;
using BarengIn.Domain;
using BarengIn.Domain.ValueObjects;
using BarengIn.Infrastructure.Services;

namespace BarengIn.Desktop.Views;

public partial class DriverWindow : Window
{
    private readonly ObservableCollection<TripRow> trips;
    private readonly Driver activeDriver;
    private CancellationTokenSource? routeCts;

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

        try
        {
            RefreshFromDatabase();
        }
        catch
        {
            // Keep offline session collections if database query fails.
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        routeCts?.Cancel();
        routeCts?.Dispose();
        routeCts = null;
        base.OnClosed(e);
    }

    private async void btnCheckRoute_Click(object? sender, RoutedEventArgs e)
    {
        string origin = (tbOrigin.Text ?? "").Trim();
        if (origin == "")
        {
            await MessageBox.Show(this, "Please enter your pickup area.");
            return;
        }

        string faculty = cbFaculty.SelectedItem as string ?? Faculties.All[0];
        await UpdateRoutePreviewAsync(origin, faculty);
    }

    private async Task UpdateRoutePreviewAsync(string origin, string faculty)
    {
        routeCts?.Cancel();
        routeCts?.Dispose();
        var cts = new CancellationTokenSource();
        routeCts = cts;

        lblRouteInfo.Text = "Checking route...";
        try
        {
            RouteEstimate estimate = await AppDb.RouteEstimator(origin, faculty, cts.Token);
            if (cts.IsCancellationRequested || !ReferenceEquals(routeCts, cts))
            {
                return;
            }

            string prefix = estimate.IsLiveApi ? "Route: " : "Fallback: ";
            lblRouteInfo.Text = prefix + estimate.SummaryText;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer check or window closure.
        }
        catch
        {
            if (!cts.IsCancellationRequested && ReferenceEquals(routeCts, cts))
            {
                lblRouteInfo.Text = "Fallback: " + RouteApiService.CreateFallbackEstimate().SummaryText;
            }
        }
    }

    public void RefreshFromDatabase()
    {
        if (!AppDb.IsAvailable || string.IsNullOrWhiteSpace(AppSession.Nim))
        {
            return;
        }

        var dbTrips = AppDb.Rides.GetDriverRides(AppSession.Nim);
        trips.Clear();
        foreach (var r in dbTrips)
        {
            trips.Add(new TripRow(
                r.Origin,
                r.Faculty,
                r.Departs,
                r.Seats,
                Display.Rupiah((int)r.Cost),
                dbRideId: r.RideId));
        }

        var dbRequests = AppDb.Rides.GetIncomingRequests(AppSession.Nim);
        AppSession.DriverRequests.Clear();
        foreach (var req in dbRequests)
        {
            string uiStatus = req.Status.ToLowerInvariant() switch
            {
                "accepted" => "Approved",
                "rejected" => "Rejected",
                _ => "Pending",
            };
            AppSession.DriverRequests.Add(new RequestItem(
                req.PassengerName,
                req.PickupAddress,
                req.Faculty,
                dbRequestId: req.RequestId,
                dbRideId: req.RideId,
                initialStatus: uiStatus));
        }
    }

    private void btnRefresh_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            RefreshFromDatabase();
        }
        catch
        {
            // Keep current list items if DB refresh fails.
        }
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

        bool refreshedFromDb = false;
        if (AppDb.IsAvailable)
        {
            try
            {
                AppDb.Rides.EnsureSeedData(Faculties.All);
                AppDb.Rides.InsertRide(AppSession.Nim, origin, faculty, DateTime.Today.Add(time), seats, cost);
                RefreshFromDatabase();
                refreshedFromDb = true;
            }
            catch
            {
                refreshedFromDb = false;
            }
        }

        if (!refreshedFromDb)
        {
            trips.Add(new TripRow(
                trip.OriginAddress,
                faculty,
                time.ToString(@"hh\:mm"),
                trip.AvailableSeats,
                Display.Rupiah(cost),
                trip.TripId));
        }
        tbOrigin.Text = "";
        await UpdateRoutePreviewAsync(origin, faculty);
    }

    private void btnApprove_Click(object? sender, RoutedEventArgs e)
    {
        if (((Button)sender!).DataContext is not RequestItem item)
        {
            return;
        }

        if (AppDb.IsAvailable && item.DbRequestId > 0)
        {
            try
            {
                if (AppDb.Rides.ApproveRequest(item.DbRequestId))
                {
                    RefreshFromDatabase();
                }
            }
            catch
            {
                // Ignore transient DB errors cleanly.
            }
            return;
        }

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

    private void btnReject_Click(object? sender, RoutedEventArgs e)
    {
        if (((Button)sender!).DataContext is not RequestItem item)
        {
            return;
        }

        if (AppDb.IsAvailable && item.DbRequestId > 0)
        {
            try
            {
                if (AppDb.Rides.RejectRequest(item.DbRequestId))
                {
                    RefreshFromDatabase();
                }
            }
            catch
            {
                // Ignore transient DB errors cleanly.
            }
            return;
        }

        // Domain Encapsulation: Driver.RejectRequest transitions RideRequest status to Rejected.
        if (activeDriver.RejectRequest(item.RequestId, "Declined by driver"))
        {
            item.Status = "Rejected";
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
