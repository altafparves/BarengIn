using System.Collections.ObjectModel;
using BarengIn.Domain;
using BarengIn.Domain.ValueObjects;
using BarengIn.Infrastructure.Persistence;

namespace BarengIn.Desktop.Models;

public static class AppSession
{
    public static User? CurrentUser { get; private set; }
    public static string Nim { get; private set; } = "";
    public static string Name { get; set; } = "";
    public static string Role { get; private set; } = "Passenger";
    public static string Faculty { get; set; } = Faculties.All[0];
    public static string Hobbies { get; set; } = "";
    public static string Bio { get; set; } = "";
    public static string VerificationStatus { get; set; } = "Verified";
    public static double Co2Saved { get; set; }

    public static ObservableCollection<TripRow> DriverTrips { get; private set; } = new();
    public static ObservableCollection<RequestItem> DriverRequests { get; private set; } = new();
    public static Trip? SeededDriverTrip { get; private set; }

    public static void SignIn(User user)
    {
        SignIn(user, null, null, user.Bio);
    }

    public static void SignIn(User user, string? faculty, string? hobbies, string? bio)
    {
        CurrentUser = user;
        Nim = user.Niu.Trim();
        Role = user.GetRoleName();
        Name = string.IsNullOrWhiteSpace(user.FullName) ? Nim : user.FullName;
        if (!string.IsNullOrWhiteSpace(faculty))
        {
            Faculty = faculty.Trim();
        }

        Hobbies = (hobbies ?? string.Empty).Trim();
        Bio = bio is not null ? bio.Trim() : user.Bio;
        VerificationStatus = user.IsVerified() ? "Verified" : "Pending";
        InitializeDriverStateIfNeeded();
    }

    public static void SignIn(string nim, string role)
    {
        Nim = nim.Trim();
        Role = role;
        if (Name == "")
        {
            Name = Nim;
        }

        string safeNim = string.IsNullOrWhiteSpace(Nim) ? "24/536741" : Nim;
        string safeName = string.IsNullOrWhiteSpace(Name) ? safeNim : Name;
        CurrentUser = UserRepository.BuildUserForRole(
            safeNim,
            safeName,
            safeNim + "@mail.ugm.ac.id",
            "session-hash",
            safeNim,
            role);
        InitializeDriverStateIfNeeded();
    }

    private static void InitializeDriverStateIfNeeded()
    {
        DriverTrips = new ObservableCollection<TripRow>();
        DriverRequests = new ObservableCollection<RequestItem>();
        SeededDriverTrip = null;

        if (CurrentUser is Driver driver && !AppDb.IsAvailable)
        {
            SeededDriverTrip = new Trip(
                driver.UserId,
                driver.Vehicles[0].VehicleId,
                "Jakal Km 5",
                new GeoPoint(-7.75, 110.38),
                Guid.Empty,
                DateTime.Today.AddHours(7.5),
                availableSeats: 3,
                distanceKm: 5.0,
                polylineJson: string.Empty);
            driver.RecordPublishedTrip(SeededDriverTrip);

            var req1 = new RideRequest(SeededDriverTrip.TripId, Guid.NewGuid(), "Jakal Km 5", new GeoPoint(-7.75, 110.38), "Morning class");
            var req2 = new RideRequest(SeededDriverTrip.TripId, Guid.NewGuid(), "Condongcatur", new GeoPoint(-7.76, 110.40), "Morning class");
            var req3 = new RideRequest(SeededDriverTrip.TripId, Guid.NewGuid(), "Seturan", new GeoPoint(-7.77, 110.41), "Morning class");
            SeededDriverTrip.ReceiveRequest(req1);
            SeededDriverTrip.ReceiveRequest(req2);
            SeededDriverTrip.ReceiveRequest(req3);

            DriverRequests.Add(new RequestItem("Rani Putri", req1.PickupAddress, "MIPA", req1.RequestId));
            DriverRequests.Add(new RequestItem("Budi Santoso", req2.PickupAddress, "Engineering (FT)", req2.RequestId));
            DriverRequests.Add(new RequestItem("Sari Dewi", req3.PickupAddress, "Medicine", req3.RequestId));
        }
    }

    public static void Reset()
    {
        CurrentUser = null;
        Nim = "";
        Name = "";
        Role = "Passenger";
        Faculty = Faculties.All[0];
        Hobbies = "";
        Bio = "";
        VerificationStatus = "Verified";
        Co2Saved = 0;
        DriverTrips = new ObservableCollection<TripRow>();
        DriverRequests = new ObservableCollection<RequestItem>();
        SeededDriverTrip = null;
    }
}
