using BarengIn.Desktop.Logic;
using BarengIn.Domain;
using BarengIn.Domain.Enums;
using BarengIn.Domain.ValueObjects;

namespace BarengIn.Desktop.Tests;

[Collection("AppDbSerial")]
public class LogicTests
{
    [Fact]
    public void Polymorphism_CanPublishTrip_FalseForPassenger_TrueForVerifiedDriverWithVehicle()
    {
        User passenger = new Passenger("24/536741", "Altaf", "a@ugm.ac.id", "hash", "081", Guid.Empty, "Jakal", new GeoPoint(-7.7, 110.3));
        var driver = new Driver("24/532894", "Calvin", "c@ugm.ac.id", "hash", "082", Guid.Empty, "Seturan", new GeoPoint(-7.7, 110.4), "AB-1234-XX");
        driver.MarkVerified();
        var vehicle = new Vehicle(driver.UserId, "AB 1234 XY", "Honda", "Jazz", "White", VehicleType.Car, 4);
        driver.AddVehicle(vehicle);

        User polymorphicDriver = driver;
        Assert.False(passenger.CanPublishTrip());
        Assert.True(polymorphicDriver.CanPublishTrip());
    }

    [Fact]
    public void Encapsulation_DriverApproveRequest_DecrementsTripAvailableSeats()
    {
        var driver = new Driver("24/532894", "Calvin", "c@ugm.ac.id", "hash", "082", Guid.Empty, "Seturan", new GeoPoint(-7.7, 110.4), "AB-1234-XX");
        driver.MarkVerified();
        var vehicle = new Vehicle(driver.UserId, "AB 1234 XY", "Honda", "Jazz", "White", VehicleType.Car, 4);
        driver.AddVehicle(vehicle);

        var trip = driver.PublishNewTrip(vehicle.VehicleId, "Seturan", new GeoPoint(-7.7, 110.4), Guid.Empty, DateTime.Today.AddHours(7.5), 3, 5000m);
        var req = new RideRequest(trip.TripId, Guid.NewGuid(), "Jakal Km 5", new GeoPoint(-7.75, 110.38), "Seat please");
        trip.ReceiveRequest(req);

        Assert.Equal(3, trip.AvailableSeats);
        Assert.True(driver.ApproveRequest(req.RequestId));
        Assert.Equal(2, trip.AvailableSeats);
        Assert.Equal(RequestStatus.Approved, req.Status);

        // Review Focus #4: Rejecting an already-approved request returns false and keeps seats unchanged.
        Assert.False(driver.RejectRequest(req.RequestId, "Changed mind"));
        Assert.Equal(2, trip.AvailableSeats);
        Assert.Equal(RequestStatus.Approved, req.Status);
    }

    [Fact]
    public void ApproveRequest_WhenTripIsFull_DoesNotCorruptRequestStatusToApproved()
    {
        var driver = new Driver("24/532894", "Calvin", "c@ugm.ac.id", "hash", "082", Guid.Empty, "Seturan", new GeoPoint(-7.7, 110.4), "AB-1234-XX");
        driver.MarkVerified();
        var vehicle = new Vehicle(driver.UserId, "AB 1234 XY", "Honda", "Jazz", "White", VehicleType.Car, 4);
        driver.AddVehicle(vehicle);

        var trip = driver.PublishNewTrip(vehicle.VehicleId, "Seturan", new GeoPoint(-7.7, 110.4), Guid.Empty, DateTime.Today.AddHours(7.5), 1, 5000m);
        var req1 = new RideRequest(trip.TripId, Guid.NewGuid(), "Jakal Km 5", new GeoPoint(-7.75, 110.38), "First");
        var req2 = new RideRequest(trip.TripId, Guid.NewGuid(), "Condongcatur", new GeoPoint(-7.76, 110.40), "Second");
        trip.ReceiveRequest(req1);
        trip.ReceiveRequest(req2);

        Assert.True(driver.ApproveRequest(req1.RequestId));
        Assert.Equal(0, trip.AvailableSeats);

        // Second request must fail because trip is full, and req2 must remain Pending so it can still be rejected.
        Assert.False(driver.ApproveRequest(req2.RequestId));
        Assert.Equal(RequestStatus.Pending, req2.Status);
        Assert.True(driver.RejectRequest(req2.RequestId, "Trip full"));
        Assert.Equal(RequestStatus.Rejected, req2.Status);
    }

    [Fact]
    public void AppDb_WhenEnvPointsToUnreachableServer_IsAvailableReturnsFalseAndFallbackWorks()
    {
        string? prev = Environment.GetEnvironmentVariable("BARENGIN_DB");
        try
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", "Host=127.0.0.1;Port=54329;Database=barengin;Username=postgres;Password=secret;Timeout=1");
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.False(BarengIn.Desktop.AppDb.IsAvailable);
            var user = BarengIn.Desktop.AppDb.AuthenticateOrFallback("24/536741", "pw", "Driver");
            Assert.NotNull(user);
            Assert.Equal("Driver", user!.GetRoleName());
        }
        finally
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", prev);
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
        }
    }

    [Fact]
    public void AppDb_ConfigPriority_EnvOverridesAppSettings_AndPasswordHereFallsBackOffline()
    {
        string? prevEnv = Environment.GetEnvironmentVariable("BARENGIN_DB");
        string? prevPath = BarengIn.Desktop.AppDb.ConfigFilePath;
        string tempFile = Path.Combine(Path.GetTempPath(), $"barengin-appsettings-{Guid.NewGuid():N}.json");

        try
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", null);
            BarengIn.Desktop.AppDb.ConfigFilePath = tempFile;

            // 1. Default template with PASSWORDHERE -> reads file, but IsDatabaseConfigured is false (offline fallback).
            File.WriteAllText(tempFile, """
            {
              "ConnectionStrings": {
                "Default": "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE"
              }
            }
            """);
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.Contains("PASSWORDHERE", BarengIn.Desktop.AppDb.ConnectionString);
            Assert.False(BarengIn.Desktop.AppDb.IsDatabaseConfigured);
            Assert.False(BarengIn.Desktop.AppDb.IsAvailable);

            // 2. Configured appsettings.json without BARENGIN_DB -> reads ConnectionStrings:Default and marks configured.
            const string configuredCs = "Host=127.0.0.1;Port=54329;Database=barengin_cfg;Username=postgres;Password=realpass";
            File.WriteAllText(tempFile, $$"""
            {
              "ConnectionStrings": {
                "Default": "{{configuredCs}}"
              }
            }
            """);
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.Equal(configuredCs, BarengIn.Desktop.AppDb.ConnectionString);
            Assert.True(BarengIn.Desktop.AppDb.IsDatabaseConfigured);
            Assert.False(BarengIn.Desktop.AppDb.IsAvailable); // Port 54329 is unreachable -> clean offline fallback

            // 3. BARENGIN_DB env var takes highest priority over appsettings.json.
            const string envCs = "Host=127.0.0.1;Port=54328;Database=barengin_env;Username=postgres;Password=envpass";
            Environment.SetEnvironmentVariable("BARENGIN_DB", envCs);
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.Equal(envCs, BarengIn.Desktop.AppDb.ConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", prevEnv);
            BarengIn.Desktop.AppDb.ConfigFilePath = prevPath;
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void AppSettings_IsCopiedToOutputDirectory()
    {
        string outputConfig = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        Assert.True(File.Exists(outputConfig), $"Expected appsettings.json in {AppContext.BaseDirectory}");
        string? cs = BarengIn.Desktop.AppDb.TryLoadConnectionStringFromAppSettings(outputConfig);
        Assert.False(string.IsNullOrWhiteSpace(cs));
    }

    [Fact]
    public void AppDb_MalformedOrMissingAppSettings_AndWhitespaceEnv_FallBackCleanly()
    {
        string? prevEnv = Environment.GetEnvironmentVariable("BARENGIN_DB");
        string? prevPath = BarengIn.Desktop.AppDb.ConfigFilePath;
        string tempFile = Path.Combine(Path.GetTempPath(), $"barengin-edge-{Guid.NewGuid():N}.json");
        string missingFile = Path.Combine(Path.GetTempPath(), $"barengin-missing-{Guid.NewGuid():N}.json");

        try
        {
            // 1. Whitespace-only BARENGIN_DB falls back to appsettings.json.
            const string configuredCs = "Host=127.0.0.1;Port=54329;Database=barengin_ws;Username=postgres;Password=realpass";
            File.WriteAllText(tempFile, $$"""
            {
              "ConnectionStrings": {
                "Default": "{{configuredCs}}"
              }
            }
            """);
            Environment.SetEnvironmentVariable("BARENGIN_DB", "   ");
            BarengIn.Desktop.AppDb.ConfigFilePath = tempFile;
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.Equal(configuredCs, BarengIn.Desktop.AppDb.ConnectionString);
            Assert.True(BarengIn.Desktop.AppDb.IsDatabaseConfigured);

            // Clear BARENGIN_DB for remaining config file edge-case checks.
            Environment.SetEnvironmentVariable("BARENGIN_DB", null);

            // 2. Malformed JSON falls back cleanly to DefaultConnectionString without throwing.
            File.WriteAllText(tempFile, "{ invalid json");
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.Contains("PASSWORDHERE", BarengIn.Desktop.AppDb.ConnectionString);
            Assert.False(BarengIn.Desktop.AppDb.IsDatabaseConfigured);
            Assert.False(BarengIn.Desktop.AppDb.IsAvailable);

            // 3. Missing appsettings.json path falls back cleanly.
            BarengIn.Desktop.AppDb.ConfigFilePath = missingFile;
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.Contains("PASSWORDHERE", BarengIn.Desktop.AppDb.ConnectionString);
            Assert.False(BarengIn.Desktop.AppDb.IsDatabaseConfigured);
            Assert.False(BarengIn.Desktop.AppDb.IsAvailable);

            // 4. JSON with comments and trailing comma parses ConnectionStrings:Default cleanly.
            const string commentedCs = "Host=127.0.0.1;Port=54329;Database=barengin_jsonc;Username=postgres;Password=jsoncpass";
            File.WriteAllText(tempFile, $$"""
            {
              // Local database configuration
              "ConnectionStrings": {
                "Default": "{{commentedCs}}",
              },
            }
            """);
            BarengIn.Desktop.AppDb.ConfigFilePath = tempFile;
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            Assert.Equal(commentedCs, BarengIn.Desktop.AppDb.ConnectionString);
            Assert.True(BarengIn.Desktop.AppDb.IsDatabaseConfigured);
            Assert.False(BarengIn.Desktop.AppDb.IsAvailable);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", prevEnv);
            BarengIn.Desktop.AppDb.ConfigFilePath = prevPath;
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
            if (File.Exists(missingFile))
            {
                File.Delete(missingFile);
            }
        }
    }

    [Theory]
    [InlineData("approved", "accepted")]
    [InlineData(" Approved ", "accepted")]
    [InlineData("ACCEPTED", "accepted")]
    [InlineData(" rejected ", "rejected")]
    [InlineData("Pending", "pending")]
    [InlineData("CANCELLED", "cancelled")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("unknown", "unknown")]
    public void RideRepository_MapRequestStatus_NormalizesKnownStatuses(string? input, string expected)
    {
        Assert.Equal(expected, BarengIn.Infrastructure.Persistence.RideRepository.MapRequestStatus(input));
    }

    [Fact]
    public void RideRepository_GetDriverRides_GetIncomingRequests_AndApproveRequest_AtomicallyDecrementsSeats_AndPreventsOverbooking()
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
            Assert.Skip("Local PostgreSQL is not running; skipping live RideRepository DB test.");
            return;
        }

        string? prevEnv = Environment.GetEnvironmentVariable("BARENGIN_DB");
        string? prevPath = BarengIn.Desktop.AppDb.ConfigFilePath;
        string tempConfig = Path.Combine(Path.GetTempPath(), $"barengin-live-{Guid.NewGuid():N}.json");
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string driverNim = $"DRV/{suffix}";
        string pax1Nim = $"PX1/{suffix}";
        string pax2Nim = $"PX2/{suffix}";

        try
        {
            // Also verify M10-01 end-to-end: unset BARENGIN_DB, load valid local PostgreSQL credentials from appsettings.json.
            Environment.SetEnvironmentVariable("BARENGIN_DB", null);
            File.WriteAllText(tempConfig, $$"""
            {
              "ConnectionStrings": {
                "Default": "{{cs}}"
              }
            }
            """);
            BarengIn.Desktop.AppDb.ConfigFilePath = tempConfig;
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();

            Assert.True(BarengIn.Desktop.AppDb.IsAvailable);

            var users = BarengIn.Desktop.AppDb.Users;
            var rides = BarengIn.Desktop.AppDb.Rides;
            rides.EnsureSeedData(BarengIn.Desktop.Models.Faculties.All);

            Assert.True(users.Register("Calvin Driver", $"drv.{suffix}@ugm.ac.id", $"081{suffix}", "pw123", driverNim, "Engineering (FT)", "Driver"));
            Assert.True(users.Register("Rani Passenger", $"px1.{suffix}@ugm.ac.id", $"082{suffix}", "pw123", pax1Nim, "MIPA", "Passenger"));
            Assert.True(users.Register("Budi Passenger", $"px2.{suffix}@ugm.ac.id", $"083{suffix}", "pw123", pax2Nim, "Medicine", "Passenger"));

            // Insert 1 driver ride with 1 seat to test both normal decrement and overbooking protection.
            int rideId = rides.InsertRide(driverNim, "Seturan", "Engineering (FT)", DateTime.Today.AddHours(8), 1, 5000m);
            Assert.True(rideId > 0);

            var driverRides = rides.GetDriverRides(driverNim);
            var targetRide = Assert.Single(driverRides, r => r.RideId == rideId);
            Assert.Equal("Seturan", targetRide.Origin);
            Assert.Equal("Engineering (FT)", targetRide.Faculty);
            Assert.Equal(1, targetRide.Seats);

            int req1Id = rides.InsertRequest(rideId, pax1Nim, "Jakal Km 5");
            int req2Id = rides.InsertRequest(rideId, pax2Nim, "Condongcatur");
            Assert.True(req1Id > 0);
            Assert.True(req2Id > 0);

            var incoming = rides.GetIncomingRequests(driverNim);
            Assert.Contains(incoming, r => r.RequestId == req1Id && r.PassengerName == "Rani Passenger" && r.Faculty == "MIPA" && r.PickupAddress == "Jakal Km 5" && r.Status == "pending");
            Assert.Contains(incoming, r => r.RequestId == req2Id && r.PassengerName == "Budi Passenger" && r.Faculty == "Medicine" && r.PickupAddress == "Condongcatur" && r.Status == "pending");

            // Approve first request -> status becomes 'accepted' and available_seats decrements from 1 to 0.
            Assert.True(rides.ApproveRequest(req1Id));

            var updatedRide = Assert.Single(rides.GetDriverRides(driverNim), r => r.RideId == rideId);
            Assert.Equal(0, updatedRide.Seats);

            var incomingAfterFirst = rides.GetIncomingRequests(driverNim);
            Assert.Contains(incomingAfterFirst, r => r.RequestId == req1Id && r.Status == "accepted");

            // Double-approve idempotency check: approving the same request twice returns false and decrements seats only once.
            Assert.False(rides.ApproveRequest(req1Id));
            Assert.Equal(0, Assert.Single(rides.GetDriverRides(driverNim), r => r.RideId == rideId).Seats);

            // Rejecting an already-accepted request must return false and leave status 'accepted'.
            Assert.False(rides.RejectRequest(req1Id));
            Assert.Contains(rides.GetIncomingRequests(driverNim), r => r.RequestId == req1Id && r.Status == "accepted");

            // Approving second request when available_seats == 0 must fail (prevent overbooking) and keep status 'pending'.
            Assert.False(rides.ApproveRequest(req2Id));
            var incomingAfterOverbookAttempt = rides.GetIncomingRequests(driverNim);
            Assert.Contains(incomingAfterOverbookAttempt, r => r.RequestId == req2Id && r.Status == "pending");

            // Rejecting a pending request returns true, updates status to 'rejected', and leaves available_seats unchanged.
            Assert.True(rides.RejectRequest(req2Id));
            Assert.Contains(rides.GetIncomingRequests(driverNim), r => r.RequestId == req2Id && r.Status == "rejected");
            Assert.Equal(0, Assert.Single(rides.GetDriverRides(driverNim), r => r.RideId == rideId).Seats);

            // Non-existent request IDs must return false cleanly.
            Assert.False(rides.ApproveRequest(-1));
            Assert.False(rides.RejectRequest(-1));
        }
        finally
        {
            Environment.SetEnvironmentVariable("BARENGIN_DB", prevEnv);
            BarengIn.Desktop.AppDb.ConfigFilePath = prevPath;
            BarengIn.Desktop.AppDb.ResetAvailabilityCache();
            if (File.Exists(tempConfig))
            {
                File.Delete(tempConfig);
            }
        }
    }

    [Fact]
    public void UserRepository_UpdateProfile_WithBlankKtm_ReturnsFalseWithoutOpeningDb()
    {
        var db = new BarengIn.Infrastructure.Persistence.Database("Host=127.0.0.1;Port=54329;Database=nonexistent;Username=postgres;Password=x;Timeout=1");
        var users = new BarengIn.Infrastructure.Persistence.UserRepository(db);

        Assert.False(users.UpdateProfile("", "MIPA", "Coding", "Bio"));
        Assert.False(users.UpdateProfile("   ", "MIPA", "Coding", "Bio"));
        Assert.False(users.UpdateProfile(null!, "MIPA", "Coding", "Bio"));
    }

    [Fact]
    public void UserRepository_UpdateProfile_PersistsFacultyHobbiesBio_AndAuthenticateRestoresThem()
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
            Assert.Skip("Local PostgreSQL is not running; skipping live UserRepository DB test.");
            return;
        }

        var db = new BarengIn.Infrastructure.Persistence.Database(cs);
        var users = new BarengIn.Infrastructure.Persistence.UserRepository(db);
        var rides = new BarengIn.Infrastructure.Persistence.RideRepository(db);
        rides.EnsureSeedData(BarengIn.Desktop.Models.Faculties.All);

        string suffix = Guid.NewGuid().ToString("N")[..6];
        string ktm = $"PRF/{suffix}";

        Assert.True(users.Register("Altaf Profile", $"prf.{suffix}@ugm.ac.id", $"085{suffix}", "pw123", ktm, "MIPA", "Passenger"));

        // Initial auth returns registered faculty and empty hobbies/bio.
        var initialUser = users.Authenticate(ktm, "pw123", out string fac0, out string hob0, out string bio0);
        Assert.NotNull(initialUser);
        Assert.Equal("MIPA", fac0);
        Assert.Equal(string.Empty, hob0);
        Assert.Equal(string.Empty, bio0);
        Assert.Equal(string.Empty, initialUser!.Bio);

        // UpdateProfile persists faculty, hobbies, and bio.
        Assert.True(users.UpdateProfile(ktm, "Engineering (FT)", "  Cycling, Coffee  ", "  CS student  "));

        // 5-arg Authenticate restores faculty, hobbies, and bio (and populates Domain User.Bio).
        var updatedUser5 = users.Authenticate(ktm, "pw123", out string fac1, out string hob1, out string bio1);
        Assert.NotNull(updatedUser5);
        Assert.Equal("Engineering (FT)", fac1);
        Assert.Equal("Cycling, Coffee", hob1);
        Assert.Equal("CS student", bio1);
        Assert.Equal("CS student", updatedUser5!.Bio);

        // 2-arg backward-compatible Authenticate also restores Domain User.Bio.
        var updatedUser2 = users.Authenticate(ktm, "pw123");
        Assert.NotNull(updatedUser2);
        Assert.Equal("CS student", updatedUser2!.Bio);

        // Clearing hobbies/bio and changing faculty updates cleanly.
        Assert.True(users.UpdateProfile(ktm, "Medicine", "", ""));
        var clearedUser = users.Authenticate(ktm, "pw123", out string fac2, out string hob2, out string bio2);
        Assert.NotNull(clearedUser);
        Assert.Equal("Medicine", fac2);
        Assert.Equal(string.Empty, hob2);
        Assert.Equal(string.Empty, bio2);
        Assert.Equal(string.Empty, clearedUser!.Bio);

        // Non-existent KTM returns false.
        Assert.False(users.UpdateProfile($"MISSING/{suffix}", "MIPA", "Reading", "Bio"));
    }

    [Fact]
    public void Departure_SubtractsMinutes()
    {
        Assert.Equal("07:30", RideLogic.Departure(new TimeSpan(8, 0, 0), 30));
    }

    [Fact]
    public void Departure_WrapsPastMidnight()
    {
        Assert.Equal("23:15", RideLogic.Departure(new TimeSpan(0, 15, 0), 60));
    }

    [Fact]
    public void Find_ReturnsThreeRidesForSelectedFaculty()
    {
        var rides = RideLogic.Find("MIPA", new TimeSpan(8, 0, 0));
        Assert.Equal(3, rides.Count);
        Assert.All(rides, r => Assert.Equal("MIPA", r.Faculty));
        Assert.Equal(new[] { "07:30", "07:15", "07:00" }, rides.Select(r => r.Departs));
    }

    [Theory]
    [InlineData("", "pw")]
    [InlineData("   ", "pw")]
    [InlineData("24/536741", "")]
    public void Login_RejectsEmptyInput(string nim, string pw)
    {
        Assert.NotNull(Validation.Login(nim, pw));
    }

    [Fact]
    public void Login_AcceptsFilledInput()
    {
        Assert.Null(Validation.Login("24/536741", "secret"));
    }

    [Fact]
    public void Register_RejectsMismatchedPasswords()
    {
        Assert.Equal("Passwords do not match.",
            Validation.Register("Altaf", "24/536741", "secret1", "secret2", "ktm.jpg"));
    }

    [Fact]
    public void Register_RejectsMissingKtm()
    {
        Assert.NotNull(Validation.Register("Altaf", "24/536741", "secret1", "secret1", ""));
    }

    [Fact]
    public void Register_AcceptsCompleteForm()
    {
        Assert.Null(Validation.Register("Altaf", "24/536741", "secret1", "secret1", "ktm.jpg"));
    }
}

public class DisplayTests
{
    [Theory]
    [InlineData(5000, "Rp 5.000")]
    [InlineData(1250000, "Rp 1.250.000")]
    [InlineData(0, "Rp 0")]
    public void Rupiah_UsesDotThousandsSeparator(int amount, string expected)
    {
        Assert.Equal(expected, Display.Rupiah(amount));
    }

    [Theory]
    [InlineData(0, "Estimated CO2 saved: 0.0 kg")]
    [InlineData(2.4, "Estimated CO2 saved: 2.4 kg")]
    public void Co2_UsesDotDecimalRegardlessOfLocale(double kg, string expected)
    {
        Assert.Equal(expected, Display.Co2(kg));
    }

    [Theory]
    [InlineData("Altaf Parves", "AP")]
    [InlineData("calvin", "C")]
    [InlineData("24/536741", "U")]
    public void Initials_TakesUpToTwoLetters(string name, string expected)
    {
        Assert.Equal(expected, Display.Initials(name));
    }
}
