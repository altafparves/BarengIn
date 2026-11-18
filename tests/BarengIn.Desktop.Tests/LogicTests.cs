using BarengIn.Desktop.Logic;
using BarengIn.Domain;
using BarengIn.Domain.Enums;
using BarengIn.Domain.ValueObjects;

namespace BarengIn.Desktop.Tests;

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
