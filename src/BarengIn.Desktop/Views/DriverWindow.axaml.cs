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
    private CancellationTokenSource? suggestCts;
    private bool suppressSuggestions;

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
        CancelAndClear(ref routeCts);
        CancelAndClear(ref suggestCts);
        base.OnClosed(e);
    }

    private static void CancelAndClear(ref CancellationTokenSource? target)
    {
        var old = Interlocked.Exchange(ref target, null);
        if (old is null)
        {
            return;
        }

        try
        {
            old.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            old.Dispose();
        }
    }

    private async void tbOrigin_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (suppressSuggestions)
        {
            return;
        }

        string query = (tbOrigin.Text ?? "").Trim();
        if (query.Length == 0)
        {
            bdrSuggestions.IsVisible = false;
            lvSuggestions.ItemsSource = null;
            return;
        }

        var localMatches = RouteApiService.SearchLocalPlaceSuggestions(query, maxResults: 5);
        lvSuggestions.ItemsSource = localMatches;
        bdrSuggestions.IsVisible = localMatches.Count > 0;

        CancelAndClear(ref suggestCts);
        if (query.Length < 3 || localMatches.Count >= 5)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        suggestCts = cts;
        try
        {
            await Task.Delay(250, cts.Token);
            var suggestions = await AppDb.PlaceSuggester(query, cts.Token);
            if (!cts.IsCancellationRequested && ReferenceEquals(suggestCts, cts))
            {
                lvSuggestions.ItemsSource = suggestions;
                bdrSuggestions.IsVisible = suggestions.Count > 0;
            }
        }
        catch
        {
            // Ignore cancellation or network errors during live typing.
        }
    }

    private async void lvSuggestions_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (lvSuggestions.SelectedItem is not PlaceSuggestion selected)
        {
            return;
        }

        CancelAndClear(ref suggestCts);

        suppressSuggestions = true;
        tbOrigin.Text = selected.Title;
        bdrSuggestions.IsVisible = false;
        lvSuggestions.SelectedItem = null;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            bdrSuggestions.IsVisible = false;
            suppressSuggestions = false;
        });

        string faculty = cbFaculty.SelectedItem as string ?? Faculties.All[0];
        await UpdateRoutePreviewAsync(selected.Title, faculty);
    }

    private async void btnCheckRoute_Click(object? sender, RoutedEventArgs e)
    {
        bdrSuggestions.IsVisible = false;
        string origin = (tbOrigin.Text ?? "").Trim();
        if (origin == "" && lvTrips.SelectedItem is TripRow selectedTrip)
        {
            origin = selectedTrip.Pickup;
        }
        else if (origin == "" && trips.Count > 0)
        {
            origin = trips[^1].Pickup;
        }

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
        CancelAndClear(ref routeCts);
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

        var dbRequests = AppDb.Rides.GetIncomingRequestsWithProfile(AppSession.Nim);
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
                initialStatus: uiStatus,
                passengerNim: req.PassengerNim,
                hobbies: req.Hobbies,
                bio: req.Bio));
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
        bdrSuggestions.IsVisible = false;
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
        suppressSuggestions = true;
        tbOrigin.Text = "";
        bdrSuggestions.IsVisible = false;
        suppressSuggestions = false;
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
