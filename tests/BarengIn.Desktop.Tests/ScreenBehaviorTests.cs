using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using BarengIn.Desktop.Models;
using BarengIn.Desktop.Views;
using BarengIn.Infrastructure.Services;

namespace BarengIn.Desktop.Tests;

[Collection("AppDbSerial")]
public class ScreenBehaviorTests
{
    private static HomeWindow NewHome()
    {
        Environment.SetEnvironmentVariable("BARENGIN_DB", "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE");
        BarengIn.Desktop.AppDb.ResetAvailabilityCache();
        AppSession.Reset();
        AppSession.SignIn("24/536741", "Passenger");
        var home = new HomeWindow();
        home.Show();
        return home;
    }

    private static DriverWindow NewDriver()
    {
        Environment.SetEnvironmentVariable("BARENGIN_DB", "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE");
        BarengIn.Desktop.AppDb.ResetAvailabilityCache();
        AppSession.Reset();
        AppSession.SignIn("24/536741", "Driver");
        var driver = new DriverWindow();
        driver.Show();
        return driver;
    }

    [AvaloniaFact]
    public void Home_FindRides_ShowsThreeRows()
    {
        var home = NewHome();
        home.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
        home.Click("btnFind");
        var rides = Assert.IsAssignableFrom<IEnumerable<RideRow>>(home.Find<ListBox>("lvRides").ItemsSource);
        Assert.Equal(3, rides.Count());
    }

    [AvaloniaFact]
    public void Home_FindRides_WithoutPickup_ShowsNothing()
    {
        var home = NewHome();
        home.Click("btnFind");
        Assert.Null(home.Find<ListBox>("lvRides").ItemsSource);
    }

    [AvaloniaFact]
    public void Home_RequestWithoutSelection_KeepsCo2AtZero()
    {
        var home = NewHome();
        home.Click("btnRequest");
        Assert.Equal(0, AppSession.Co2Saved);
        Assert.Equal("Estimated CO2 saved: 0.0 kg", home.Find<TextBlock>("lblCo2").Text);
    }

    [AvaloniaFact]
    public void Home_RequestWithSelection_AddsCo2()
    {
        var home = NewHome();
        home.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
        home.Click("btnFind");
        home.Find<ListBox>("lvRides").SelectedIndex = 0;
        home.Click("btnRequest");
        Assert.Equal("Estimated CO2 saved: 1.2 kg", home.Find<TextBlock>("lblCo2").Text);
    }

    [AvaloniaFact]
    public void Driver_PostTrip_AddsToList_AndRejectsEmptyPickup()
    {
        var driver = NewDriver();
        var trips = driver.Find<ListBox>("lvTrips");

        driver.Click("btnPost");
        Assert.Empty(trips.ItemsSource!.Cast<object>());

        driver.Find<TextBox>("tbOrigin").Text = "Seturan";
        driver.Click("btnPost");
        var trip = Assert.IsType<TripRow>(Assert.Single(trips.ItemsSource!.Cast<object>()));
        Assert.Equal("Seturan", trip.Pickup);
        Assert.Equal("07:30", trip.Departs);
        Assert.Equal("Rp 5.000", trip.Cost);
    }

    [AvaloniaFact]
    public void Driver_ApproveAndReject_ChangeRequestStatus()
    {
        var driver = NewDriver();
        driver.Layout();
        var requests = driver.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().ToList();

        driver.FindButton("Approve").Press();
        Assert.Equal("Approved", requests[0].Status);

        driver.Layout();
        driver.FindButton("Reject").Press();
        Assert.Equal("Rejected", requests[1].Status);
        Assert.Equal("Pending", requests[2].Status);
    }

    [AvaloniaFact]
    public void Profile_Save_KeepsHobbiesAndBio()
    {
        AppSession.Reset();
        AppSession.SignIn("24/536741", "Passenger");
        var profile = new ProfileWindow();
        profile.Show();
        profile.Find<TextBox>("tbHobbies").Text = "badminton";
        profile.Find<TextBox>("tbBio").Text = "Likes early rides.";
        profile.Click("btnSave");
        Assert.Equal("badminton", AppSession.Hobbies);
        Assert.Equal("Likes early rides.", AppSession.Bio);
        Assert.Equal("Likes early rides.", AppSession.CurrentUser?.Bio);
    }

    [AvaloniaFact]
    public void Driver_PostAndApprove_UpdatesDomainDriverAndTrip()
    {
        var driverWindow = NewDriver();
        var driver = Assert.IsType<BarengIn.Domain.Driver>(AppSession.CurrentUser);
        Assert.True(driver.CanPublishTrip());

        driverWindow.Find<TextBox>("tbOrigin").Text = "Seturan";
        driverWindow.Click("btnPost");
        var postedTrip = Assert.Single(driver.PublishedTrips, t => t.OriginAddress == "Seturan");
        Assert.Equal(3, postedTrip.AvailableSeats);

        driverWindow.Layout();
        var firstReq = driverWindow.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().First();
        driverWindow.FindButton("Approve").Press();
        Assert.Equal("Approved", firstReq.Status);

        // Approving a request decrements both the Domain Trip.AvailableSeats and the visible TripRow.Seats in lvTrips.
        Assert.Equal(2, postedTrip.AvailableSeats);
        var tripRow = driverWindow.Find<ListBox>("lvTrips").ItemsSource!.Cast<TripRow>().First(r => r.Pickup == "Seturan");
        Assert.Equal(2, tripRow.Seats);
    }

    [Fact]
    public void AppSession_SignIn_WithProfile_RestoresFacultyHobbiesBio()
    {
        AppSession.Reset();
        var user = BarengIn.Infrastructure.Persistence.UserRepository.BuildUserForRole(
            "24/536741",
            "Altaf Parves",
            "altaf@mail.ugm.ac.id",
            "hash",
            "08123456789",
            "Passenger");

        AppSession.SignIn(user, "Medicine", "Hiking, Photography", "Med student");

        Assert.Equal("Medicine", AppSession.Faculty);
        Assert.Equal("Hiking, Photography", AppSession.Hobbies);
        Assert.Equal("Med student", AppSession.Bio);

        AppSession.Reset();
        user.UpdateProfile("Altaf Parves", "08123456789", "Updated domain bio");
        AppSession.SignIn(user);
        Assert.Equal("Updated domain bio", AppSession.Bio);
    }

    [AvaloniaFact]
    public void Driver_RefreshButton_InOfflineMode_PreservesRequestsWithoutCrashing()
    {
        string? prevEnv = Environment.GetEnvironmentVariable("BARENGIN_DB");
        try
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE");
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();

            var driverWindow = NewDriver();
            driverWindow.Click("btnRefresh");

            var requests = driverWindow.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().ToList();
            Assert.Equal(3, requests.Count);
        }
        finally
        {
            AppSession.Reset();
            Environment.SetEnvironmentVariable("BARENGIN_DB", prevEnv);
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
        }
    }

    [AvaloniaFact]
    public void Profile_Save_PersistsSelectedFaculty()
    {
        AppSession.Reset();
        AppSession.SignIn("24/536741", "Passenger");
        var profile = new ProfileWindow();
        profile.Show();
        profile.Find<ComboBox>("cbFaculty").SelectedItem = "Medicine";
        profile.Find<TextBox>("tbHobbies").Text = "chess";
        profile.Find<TextBox>("tbBio").Text = "Evening commuter";
        profile.Click("btnSave");

        Assert.Equal("Medicine", AppSession.Faculty);
        Assert.Equal("chess", AppSession.Hobbies);
        Assert.Equal("Evening commuter", AppSession.Bio);
    }

    [AvaloniaFact]
    public void DriverWindow_And_ProfileWindow_WithLivePostgres_LoadApproveReject_AndPersistProfile()
    {
        string cs = Environment.GetEnvironmentVariable("BARENGIN_TEST_DB")
            ?? $"Host=localhost;Port=5432;Database=barengin;Username={Environment.UserName};Timeout=1";

        try
        {
            using var probe = new Npgsql.NpgsqlConnection(cs);
            probe.Open();
        }
        catch
        {
            Assert.Skip("Local PostgreSQL is not running; skipping live DriverWindow/ProfileWindow test.");
            return;
        }

        string? prevEnv = Environment.GetEnvironmentVariable("BARENGIN_DB");
        string? prevPath = BarengIn.Desktop.AppDb.ConfigFilePath;
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string driverNim = $"UID/{suffix}";
        string pax1Nim = $"UP1/{suffix}";
        string pax2Nim = $"UP2/{suffix}";

        try
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", cs);
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.True(BarengIn.Desktop.AppDb.IsAvailable);

            var users = BarengIn.Desktop.AppDb.Users;
            var rides = BarengIn.Desktop.AppDb.Rides;
            rides.EnsureSeedData(Faculties.All);

            Assert.True(users.Register("Calvin Live", $"uid.{suffix}@ugm.ac.id", $"081{suffix}", "pw123", driverNim, "Engineering (FT)", "Driver"));
            Assert.True(users.Register("Rani Live", $"up1.{suffix}@ugm.ac.id", $"082{suffix}", "pw123", pax1Nim, "MIPA", "Passenger"));
            Assert.True(users.Register("Budi Live", $"up2.{suffix}@ugm.ac.id", $"083{suffix}", "pw123", pax2Nim, "Medicine", "Passenger"));

            int rideId = rides.InsertRide(driverNim, "Seturan", "Engineering (FT)", DateTime.Today.AddHours(8), 1, 5000m);
            Assert.True(rideId > 0);
            int req1Id = rides.InsertRequest(rideId, pax1Nim, "Jakal Km 5");
            int req2Id = rides.InsertRequest(rideId, pax2Nim, "Condongcatur");
            Assert.True(req1Id > 0);
            Assert.True(req2Id > 0);

            AppSession.Reset();
            var login = new LoginWindow();
            login.Show();
            Navigator.Current = login;
            login.Find<ComboBox>("cbRole").SelectedIndex = 1;
            login.Find<TextBox>("tbUsername").Text = driverNim;
            login.Find<TextBox>("tbPassword").Text = "pw123";
            login.Click("btnLogin");

            var driverWindow = Assert.IsType<DriverWindow>(Navigator.Current);
            driverWindow.Layout();

            var loadedTrip = Assert.Single(driverWindow.Find<ListBox>("lvTrips").ItemsSource!.Cast<TripRow>(), t => t.DbRideId == rideId);
            Assert.Equal(1, loadedTrip.Seats);

            var loadedRequests = driverWindow.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().ToList();
            Assert.Equal(2, loadedRequests.Count);
            Assert.All(loadedRequests, r => Assert.Equal("Pending", r.Status));

            // Approve first visible pending request -> DB decrements seats to 0 and reloads UI.
            driverWindow.FindButton("Approve").Press();
            driverWindow.Layout();

            var refreshedTrip = Assert.Single(driverWindow.Find<ListBox>("lvTrips").ItemsSource!.Cast<TripRow>(), t => t.DbRideId == rideId);
            Assert.Equal(0, refreshedTrip.Seats);
            var afterApprove = driverWindow.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().ToList();
            Assert.Single(afterApprove, r => r.Status == "Approved");
            Assert.Single(afterApprove, r => r.Status == "Pending");

            // Clicking Refresh preserves DB state.
            driverWindow.Click("btnRefresh");
            driverWindow.Layout();
            var afterRefresh = driverWindow.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().ToList();
            Assert.Single(afterRefresh, r => r.Status == "Approved");
            Assert.Single(afterRefresh, r => r.Status == "Pending");

            // Reject remaining pending request -> DB updates status to rejected and keeps seats at 0.
            driverWindow.FindButton("Reject").Press();
            driverWindow.Layout();
            var afterReject = driverWindow.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().ToList();
            Assert.Single(afterReject, r => r.Status == "Approved");
            Assert.Single(afterReject, r => r.Status == "Rejected");
            Assert.Equal(0, Assert.Single(driverWindow.Find<ListBox>("lvTrips").ItemsSource!.Cast<TripRow>(), t => t.DbRideId == rideId).Seats);

            // Update profile via ProfileWindow and verify persistence across re-login.
            var profile = new ProfileWindow();
            profile.Show();
            profile.Find<ComboBox>("cbFaculty").SelectedItem = "Medicine";
            profile.Find<TextBox>("tbHobbies").Text = "Badminton, Reading";
            profile.Find<TextBox>("tbBio").Text = "UGM driver";
            profile.Click("btnSave");

            AppSession.Reset();
            var login2 = new LoginWindow();
            login2.Show();
            Navigator.Current = login2;
            login2.Find<ComboBox>("cbRole").SelectedIndex = 1;
            login2.Find<TextBox>("tbUsername").Text = driverNim;
            login2.Find<TextBox>("tbPassword").Text = "pw123";
            login2.Click("btnLogin");

            Assert.Equal("Medicine", AppSession.Faculty);
            Assert.Equal("Badminton, Reading", AppSession.Hobbies);
            Assert.Equal("UGM driver", AppSession.Bio);
            Assert.Equal("UGM driver", AppSession.CurrentUser?.Bio);

            var profile2 = new ProfileWindow();
            profile2.Show();
            Assert.Equal("Medicine", profile2.Find<ComboBox>("cbFaculty").SelectedItem);
            Assert.Equal("Badminton, Reading", profile2.Find<TextBox>("tbHobbies").Text);
            Assert.Equal("UGM driver", profile2.Find<TextBox>("tbBio").Text);
        }
        finally
        {
            try
            {
                using var cleanupConn = new Npgsql.NpgsqlConnection(cs);
                cleanupConn.Open();
                using var cleanupCmd = new Npgsql.NpgsqlCommand(@"
                    DELETE FROM ride_request WHERE ride_id IN (SELECT ride_id FROM ride r JOIN users u ON u.user_id = r.driver_id WHERE u.ktm_number = @d);
                    DELETE FROM ride WHERE driver_id IN (SELECT user_id FROM users WHERE ktm_number = @d);
                    DELETE FROM vehicle WHERE driver_id IN (SELECT user_id FROM users WHERE ktm_number = @d);
                    DELETE FROM users WHERE ktm_number IN (@d, @p1, @p2);", cleanupConn);
                cleanupCmd.Parameters.AddWithValue("d", driverNim);
                cleanupCmd.Parameters.AddWithValue("p1", pax1Nim);
                cleanupCmd.Parameters.AddWithValue("p2", pax2Nim);
                cleanupCmd.ExecuteNonQuery();
            }
            catch
            {
                // Ignore test cleanup errors.
            }

            AppSession.Reset();
            Environment.SetEnvironmentVariable("BARENGIN_DB", prevEnv);
            BarengIn.Desktop.AppDb.ConfigFilePath = prevPath;
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
        }
    }

    [AvaloniaFact]
    public void Home_FindRides_ShowsCheckingRouteLoadingState_ThenLiveRouteSummary()
    {
        var prevEstimator = BarengIn.Desktop.AppDb.RouteEstimator;
        var tcs = new TaskCompletionSource<RouteEstimate>();
        HomeWindow? home = null;
        try
        {
            BarengIn.Desktop.AppDb.RouteEstimator = (_, _, _) => tcs.Task;
            home = NewHome();
            home.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
            home.Click("btnFind");

            var rides = Assert.IsAssignableFrom<IEnumerable<RideRow>>(home.Find<ListBox>("lvRides").ItemsSource);
            Assert.Equal(3, rides.Count());
            Assert.Equal("Checking route...", home.Find<TextBlock>("lblRouteInfo").Text);

            tcs.SetResult(new RouteEstimate(7.5, 16, 0.9, IsLiveApi: true, Source: "OSRM + Nominatim"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(
                "Route: ~7.5 km • ~16 min • 0.90 kg CO2 saved (OSRM + Nominatim)",
                home.Find<TextBlock>("lblRouteInfo").Text);
        }
        finally
        {
            home?.Close();
            BarengIn.Desktop.AppDb.RouteEstimator = prevEstimator;
        }
    }

    [AvaloniaFact]
    public void Driver_CheckRoute_And_PostTrip_DisplayRouteFallback_WithoutCrashing()
    {
        var prevEstimator = BarengIn.Desktop.AppDb.RouteEstimator;
        DriverWindow? driverWindow = null;
        try
        {
            BarengIn.Desktop.AppDb.RouteEstimator = (_, _, _) =>
                Task.FromResult(RouteApiService.CreateFallbackEstimate());

            driverWindow = NewDriver();
            driverWindow.Find<TextBox>("tbOrigin").Text = "Seturan";
            driverWindow.Click("btnCheckRoute");
            Dispatcher.UIThread.RunJobs();

            string expectedFallback = "Fallback: " + RouteApiService.CreateFallbackEstimate().SummaryText;
            Assert.Equal(expectedFallback, driverWindow.Find<TextBlock>("lblRouteInfo").Text);

            driverWindow.Click("btnPost");
            Dispatcher.UIThread.RunJobs();

            var trips = driverWindow.Find<ListBox>("lvTrips").ItemsSource!.Cast<TripRow>().ToList();
            Assert.Contains(trips, t => t.Pickup == "Seturan");
            Assert.Equal(expectedFallback, driverWindow.Find<TextBlock>("lblRouteInfo").Text);
        }
        finally
        {
            driverWindow?.Close();
            BarengIn.Desktop.AppDb.RouteEstimator = prevEstimator;
        }
    }

    [AvaloniaFact]
    public void Home_TypingPickupArea_ShowsPlaceSuggestions_AndSelectingOneFillsTextBox()
    {
        var home = NewHome();
        var tbOrigin = home.Find<TextBox>("tbOrigin");
        tbOrigin.Text = "pogu";
        Dispatcher.UIThread.RunJobs();

        var bdr = home.Find<Border>("bdrSuggestions");
        var list = home.Find<ListBox>("lvSuggestions");
        Assert.True(bdr.IsVisible);
        var suggestions = Assert.IsAssignableFrom<IEnumerable<PlaceSuggestion>>(list.ItemsSource).ToList();
        Assert.Contains(suggestions, s => s.Title.Contains("Pogung Lor", StringComparison.OrdinalIgnoreCase));

        list.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(suggestions[0].Title, tbOrigin.Text);
        Assert.False(bdr.IsVisible);
    }

    [AvaloniaFact]
    public void Profile_DisplaysReadOnlyIdentityFields_AndAllowsEditingHobbiesAndBio()
    {
        AppSession.Reset();
        var user = BarengIn.Infrastructure.Persistence.UserRepository.BuildUserForRole(
            "24/536741/TK/59565",
            "Altaf Parves",
            "altaf@mail.ugm.ac.id",
            "hash",
            "081234567890",
            "Passenger");
        AppSession.SignIn(user, "Engineering (FT)", "badminton", "Morning commuter");

        var profile = new ProfileWindow();
        profile.Show();

        Assert.Equal("Altaf Parves", profile.Find<TextBlock>("lblFullNameValue").Text);
        Assert.Equal("24/536741/TK/59565", profile.Find<TextBlock>("lblNimValue").Text);
        Assert.Equal("Engineering (FT)", profile.Find<TextBlock>("lblFacultyValue").Text);
        Assert.Equal("altaf@mail.ugm.ac.id", profile.Find<TextBlock>("lblEmailValue").Text);
        Assert.Equal("081234567890", profile.Find<TextBlock>("lblPhoneValue").Text);
        Assert.False(profile.Find<ComboBox>("cbFaculty").IsVisible);

        profile.Find<TextBox>("tbHobbies").Text = "badminton, coffee";
        profile.Find<TextBox>("tbBio").Text = "Updated bio";
        profile.Click("btnSave");

        Assert.Equal("badminton, coffee", AppSession.Hobbies);
        Assert.Equal("Updated bio", AppSession.Bio);
    }

    [AvaloniaFact]
    public void MainWindows_EnableCanResizeWithMinimumDimensions()
    {
        AppSession.Reset();
        AppSession.SignIn("24/536741", "Driver");

        var login = new LoginWindow();
        var register = new RegisterWindow();
        var profile = new ProfileWindow();
        var home = new HomeWindow();
        var driver = new DriverWindow();
        var admin = new AdminWindow();

        Assert.True(login.CanResize);
        Assert.Equal(340, login.MinWidth);
        Assert.Equal(430, login.MinHeight);

        Assert.True(register.CanResize);
        Assert.Equal(380, register.MinWidth);
        Assert.Equal(600, register.MinHeight);

        Assert.True(profile.CanResize);
        Assert.Equal(400, profile.MinWidth);
        Assert.Equal(520, profile.MinHeight);

        Assert.True(home.CanResize);
        Assert.Equal(560, home.MinWidth);
        Assert.Equal(480, home.MinHeight);

        Assert.True(driver.CanResize);
        Assert.Equal(840, driver.MinWidth);
        Assert.Equal(560, driver.MinHeight);

        Assert.True(admin.CanResize);
        Assert.Equal(900, admin.MinWidth);
        Assert.Equal(580, admin.MinHeight);
    }

    [AvaloniaFact]
    public void AdminWindow_VerifyRejectSuspend_And_GenerateReport_WorkAndSyncWithOfflineRegistration()
    {
        Environment.SetEnvironmentVariable("BARENGIN_DB", "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE");
        BarengIn.Desktop.AppDb.ResetAvailabilityCache();
        BarengIn.Desktop.AppDb.ClearOfflineUsers();
        AppSession.Reset();

        // Register a new user in offline mode -> status starts as Pending
        Assert.True(BarengIn.Desktop.AppDb.RegisterOrFallback(
            "Nadia Kusuma",
            "nadia@mail.ugm.ac.id",
            "081299998888",
            "secret123",
            "24/599999/TK/59999",
            "Engineering (FT)",
            "Passenger",
            "ktm_nadia.png"));

        // Log in as Nadia -> Profile shows KTM pending, but she can still use the app
        var nadiaUser = BarengIn.Desktop.AppDb.AuthenticateOrFallback("24/599999/TK/59999", "secret123", "Passenger");
        Assert.NotNull(nadiaUser);
        AppSession.SignIn(nadiaUser!, "Engineering (FT)", "", "");
        Assert.Equal("Pending", AppSession.VerificationStatus);
        var pendingProfile = new ProfileWindow();
        pendingProfile.Show();
        Assert.Equal("KTM pending", pendingProfile.Find<TextBlock>("lblStatus").Text);

        // Log in as Admin -> Nadia appears in the KTM queue as Pending
        AppSession.Reset();
        AppSession.SignIn("ADMIN/01", "Admin");
        var adminWindow = new AdminWindow();
        adminWindow.Show();
        adminWindow.Layout();

        var users = adminWindow.Find<ListBox>("lvUsers").ItemsSource!.Cast<AdminUserItem>().ToList();
        var nadiaItem = Assert.Single(users, u => u.Nim == "24/599999/TK/59999");
        Assert.Equal("Pending", nadiaItem.Status);

        int initialPendingCount = users.Count(u => u.IsPending);
        Assert.Equal($"{initialPendingCount} pending", adminWindow.Find<TextBlock>("lblPendingKtms").Text);

        // Verify first pending user (Nadia)
        adminWindow.FindButton("Verify KTM").Press();
        adminWindow.Layout();
        Assert.Equal("Verified", nadiaItem.Status);
        Assert.Equal($"{initialPendingCount - 1} pending", adminWindow.Find<TextBlock>("lblPendingKtms").Text);

        // Re-authenticate Nadia -> now Verified!
        var verifiedNadia = BarengIn.Desktop.AppDb.AuthenticateOrFallback("24/599999/TK/59999", "secret123", "Passenger");
        Assert.NotNull(verifiedNadia);
        Assert.True(verifiedNadia!.IsVerified());

        // Reject next pending user
        adminWindow.FindButton("Reject").Press();
        adminWindow.Layout();
        Assert.Equal($"{initialPendingCount - 2} pending", adminWindow.Find<TextBlock>("lblPendingKtms").Text);

        // Suspend a user
        adminWindow.FindButton("Suspend").Press();
        adminWindow.Layout();
        Assert.Contains(users, u => u.Status == "Suspended");

        // Leaderboard and Report summary are populated
        var leaderboard = adminWindow.Find<ListBox>("lvLeaderboard").ItemsSource!.Cast<LeaderboardItem>().ToList();
        Assert.NotEmpty(leaderboard);
        Assert.Contains("Sustainability Report", adminWindow.Find<TextBlock>("lblReportSummary").Text);
    }
}
