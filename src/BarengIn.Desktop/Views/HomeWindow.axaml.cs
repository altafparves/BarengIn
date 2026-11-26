using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;
using BarengIn.Domain;
using BarengIn.Domain.ValueObjects;
using BarengIn.Infrastructure.Services;

namespace BarengIn.Desktop.Views;

public partial class HomeWindow : Window
{
    private CancellationTokenSource? routeCts;
    private CancellationTokenSource? suggestCts;
    private bool suppressSuggestions;

    public HomeWindow()
    {
        InitializeComponent();
        lblWelcome.Text = "Hi, " + AppSession.Name + " (" + AppSession.Role + ")";
        cbFaculty.ItemsSource = Faculties.All;
        cbFaculty.SelectedItem = AppSession.Faculty;
        RefreshApprovedCo2FromDb();
        UpdateCo2Label();
    }

    private void RefreshApprovedCo2FromDb()
    {
        if (AppDb.IsAvailable && !string.IsNullOrWhiteSpace(AppSession.Nim))
        {
            try
            {
                AppSession.Co2Saved = AppDb.Rides.GetApprovedPassengerCo2Saved(AppSession.Nim);
            }
            catch
            {
                // Keep current Co2Saved if DB lookup fails.
            }
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
        Dispatcher.UIThread.Post(() =>
        {
            bdrSuggestions.IsVisible = false;
            suppressSuggestions = false;
        });

        string faculty = cbFaculty.SelectedItem as string ?? Faculties.All[0];
        await UpdateRoutePreviewAsync(selected.Title, faculty);
    }

    private void UpdateCo2Label()
    {
        lblCo2.Text = Display.Co2(AppSession.Co2Saved);
    }

    private async void btnFind_Click(object? sender, RoutedEventArgs e)
    {
        bdrSuggestions.IsVisible = false;
        string origin = (tbOrigin.Text ?? "").Trim();
        if (origin == "")
        {
            await MessageBox.Show(this, "Please enter your pickup area.");
            return;
        }

        string faculty = cbFaculty.SelectedItem as string ?? Faculties.All[0];
        bool populatedFromDb = false;
        if (AppDb.IsAvailable)
        {
            try
            {
                var dbRides = AppDb.Rides.FindRidesWithOriginAndProfile(faculty, origin)
                    .Select(r => new RideRow(
                        r.DriverName,
                        r.Faculty,
                        r.Departs,
                        r.Seats,
                        Display.Rupiah((int)r.Cost),
                        r.RideId,
                        r.Origin,
                        r.DriverNim,
                        r.DriverHobbies,
                        r.DriverBio))
                    .ToList();
                lvRides.ItemsSource = dbRides;
                populatedFromDb = true;
            }
            catch
            {
                populatedFromDb = false;
            }
        }

        if (!populatedFromDb)
        {
            lvRides.ItemsSource = RideLogic.Find(faculty, dtpTime.SelectedTime ?? TimeSpan.Zero);
        }

        RefreshApprovedCo2FromDb();
        UpdateCo2Label();
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
            // Superseded by a newer search or window closure.
        }
        catch
        {
            if (!cts.IsCancellationRequested && ReferenceEquals(routeCts, cts))
            {
                lblRouteInfo.Text = "Fallback: " + RouteApiService.CreateFallbackEstimate().SummaryText;
            }
        }
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
            RefreshApprovedCo2FromDb();
        }
        else if (!AppDb.IsAvailable)
        {
            AppSession.Co2Saved += RideLogic.Co2PerRequest;
        }

        UpdateCo2Label();
        await MessageBox.Show(this, "Request sent to " + ride.Driver + ". Waiting for driver approval (CO2 saved updates once your ride is approved).");
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
