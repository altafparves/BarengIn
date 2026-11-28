using System.Collections.ObjectModel;
using BarengIn.Domain;
using BarengIn.Domain.Enums;
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
    public static ObservableCollection<AdminUserItem> AdminUsers { get; private set; } = new();
    public static ObservableCollection<LeaderboardItem> Leaderboard { get; private set; } = new();
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
        VerificationStatus = user.IsSuspended
            ? "Suspended"
            : user.Status switch
            {
                BarengIn.Domain.Enums.VerificationStatus.Verified => "Verified",
                BarengIn.Domain.Enums.VerificationStatus.Rejected => "Rejected",
                _ => "Pending",
            };
        InitializeDriverStateIfNeeded();
        InitializeAdminStateIfNeeded();
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
        InitializeAdminStateIfNeeded();
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

            var req1 = new RideRequest(SeededDriverTrip.TripId, Guid.NewGuid(), "Pogung Dalangan", new GeoPoint(-7.76, 110.37), "Morning class");
            var req2 = new RideRequest(SeededDriverTrip.TripId, Guid.NewGuid(), "Sendowo", new GeoPoint(-7.77, 110.37), "Morning class");
            var req3 = new RideRequest(SeededDriverTrip.TripId, Guid.NewGuid(), "Seturan", new GeoPoint(-7.77, 110.41), "Morning class");
            SeededDriverTrip.ReceiveRequest(req1);
            SeededDriverTrip.ReceiveRequest(req2);
            SeededDriverTrip.ReceiveRequest(req3);

            DriverRequests.Add(new RequestItem("Vino", req1.PickupAddress, "Engineering (FT)", req1.RequestId, passengerNim: "24/520001/TK/59001", hobbies: "UI/UX, coffee, reading", bio: "Engineering student commuting from Pogung Dalangan"));
            DriverRequests.Add(new RequestItem("Farhan", req2.PickupAddress, "Engineering (FT)", req2.RequestId, passengerNim: "24/520002/TK/59002", hobbies: "robotics, cycling", bio: "Morning lecture at Engineering (FT)"));
            DriverRequests.Add(new RequestItem("Sari Dewi", req3.PickupAddress, "Medicine", req3.RequestId, passengerNim: "24/534012/KU/25019", hobbies: "running, classical music", bio: "Always punctual at pickup point"));
        }
    }

    public static void InitializeAdminStateIfNeeded()
    {
        if (AdminUsers.Count == 0)
        {
            SeedDefaultAdminData();
        }

        foreach (var kv in AppDb.GetOfflineRegisteredUsers())
        {
            string nim = kv.Key;
            var reg = kv.Value;
            var existing = AdminUsers.FirstOrDefault(u => u.Nim.Equals(nim, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                AdminUsers.Insert(0, new AdminUserItem(nim, reg.FullName, reg.Role, reg.Faculty, reg.KtmFile, reg.Status));
            }
            else
            {
                existing.Status = reg.Status;
            }
        }
    }

    private static void SeedDefaultAdminData()
    {
        AdminUsers = new ObservableCollection<AdminUserItem>
        {
            new AdminUserItem("24/520002/TK/59002", "Farhan", "Passenger", "Engineering (FT)", "ktm_farhan_pending.png", "Pending"),
            new AdminUserItem("24/534012/KU/25019", "Sari Dewi", "Passenger", "Medicine", "ktm_sari_dewi.png", "Pending"),
            new AdminUserItem("24/520194/TK/58901", "Fajar Nugroho", "Driver", "Engineering (FT)", "ktm_fajar.jpg", "Pending"),
            new AdminUserItem("24/510001/TK/58001", "Satrio", "Driver", "Engineering (FT)", "ktm_satrio_ft.jpg", "Verified"),
            new AdminUserItem("24/510002/EK/58002", "Nalen", "Driver", "Economics and Business (FEB)", "ktm_nalen_feb.jpg", "Verified"),
            new AdminUserItem("24/520001/TK/59001", "Vino", "Passenger", "Engineering (FT)", "ktm_vino_ft.png", "Verified"),
            new AdminUserItem("24/510003/KU/58003", "Raka Wijaya", "Driver", "Medicine", "ktm_raka.jpg", "Verified"),
        };

        Leaderboard = new ObservableCollection<LeaderboardItem>
        {
            new LeaderboardItem(1, "Satrio", "24/510001/TK/58001", "Engineering (FT)", "Driver", 14, 16.8),
            new LeaderboardItem(2, "Nalen", "24/510002/EK/58002", "Economics and Business (FEB)", "Driver", 11, 13.2),
            new LeaderboardItem(3, "Vino", "24/520001/TK/59001", "Engineering (FT)", "Passenger", 9, 10.8),
            new LeaderboardItem(4, "Raka Wijaya", "24/510003/KU/58003", "Medicine", "Driver", 7, 8.4),
            new LeaderboardItem(5, "Farhan", "24/520002/TK/59002", "Engineering (FT)", "Passenger", 6, 7.2),
        };
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
        AdminUsers = new ObservableCollection<AdminUserItem>();
        Leaderboard = new ObservableCollection<LeaderboardItem>();
        SeededDriverTrip = null;
    }
}
