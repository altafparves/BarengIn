using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;
using BarengIn.Domain;
using BarengIn.Domain.ValueObjects;
using BarengIn.Infrastructure.Services;

namespace BarengIn.Desktop.Views;

public partial class HomeWindow : Window
{
    private CancellationTokenSource? routeCts;

    public HomeWindow()
    {
        InitializeComponent();
        lblWelcome.Text = "Hi, " + AppSession.Name + " (" + AppSession.Role + ")";
        cbFaculty.ItemsSource = Faculties.All;
        cbFaculty.SelectedItem = AppSession.Faculty;
        UpdateCo2Label();
    }

    protected override void OnClosed(EventArgs e)
    {
        routeCts?.Cancel();
        routeCts?.Dispose();
        routeCts = null;
        base.OnClosed(e);
    }

    private void UpdateCo2Label()
    {
        lblCo2.Text = Display.Co2(AppSession.Co2Saved);
    }

    private async void btnFind_Click(object? sender, RoutedEventArgs e)
    {
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
            var dbRides = AppDb.Rides.FindRides(faculty)
                .Select(r => new RideRow(r.DriverName, r.Faculty, r.Departs, r.Seats, Display.Rupiah((int)r.Cost), r.RideId))
                .ToList();
            if (dbRides.Count > 0)
            {
                lvRides.ItemsSource = dbRides;
                populatedFromDb = true;
            }
        }

        if (!populatedFromDb)
        {
            lvRides.ItemsSource = RideLogic.Find(faculty, dtpTime.SelectedTime ?? TimeSpan.Zero);
        }

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
