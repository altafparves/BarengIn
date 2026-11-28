using Npgsql;

namespace BarengIn.Infrastructure.Persistence;

public sealed class RideRepository
{
    private readonly Database db;

    public RideRepository(Database db) => this.db = db;

    public static string MapRequestStatus(string? status)
    {
        string normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized == "approved" ? "accepted" : normalized;
    }

    public void EnsureSeedData(IEnumerable<string> faculties)
    {
        using var conn = db.Open();
        foreach (string f in faculties)
        {
            var (lat, lon) = Services.RouteApiService.GetFacultyCoordinates(f);
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO faculty (name, short_name, latitude, longitude)
                VALUES (@name, @short, @lat, @lon)
                ON CONFLICT (name) DO UPDATE SET
                    latitude = EXCLUDED.latitude,
                    longitude = EXCLUDED.longitude;", conn);
            cmd.Parameters.AddWithValue("name", f);
            cmd.Parameters.AddWithValue("short", f.Length > 20 ? f[..20] : f);
            cmd.Parameters.AddWithValue("lat", (decimal)lat);
            cmd.Parameters.AddWithValue("lon", (decimal)lon);
            cmd.ExecuteNonQuery();
        }
    }

    public int InsertRide(
        string driverNim,
        string originAddress,
        string facultyName,
        DateTime departureTime,
        int seats,
        decimal costPerSeat)
    {
        using var conn = db.Open();
        using var tx = conn.BeginTransaction();

        using var userCmd = new NpgsqlCommand(
            "SELECT user_id FROM users WHERE ktm_number = @nim LIMIT 1;", conn, tx);
        userCmd.Parameters.AddWithValue("nim", driverNim);
        if (userCmd.ExecuteScalar() is not int userId)
        {
            return 0;
        }

        using (var drvCmd = new NpgsqlCommand(@"
            INSERT INTO driver (user_id, license_number)
            VALUES (@uid, @lic)
            ON CONFLICT (user_id) DO NOTHING;", conn, tx))
        {
            drvCmd.Parameters.AddWithValue("uid", userId);
            drvCmd.Parameters.AddWithValue("lic", $"AB-{userId}-UGM");
            drvCmd.ExecuteNonQuery();
        }

        int vehicleId;
        using (var vehCmd = new NpgsqlCommand(@"
            INSERT INTO vehicle (driver_id, plate_number, model, color, seat_capacity)
            VALUES (@uid, @plate, 'Mazda 6', 'Red', 4)
            ON CONFLICT (plate_number) DO UPDATE SET model = EXCLUDED.model
            RETURNING vehicle_id;", conn, tx))
        {
            vehCmd.Parameters.AddWithValue("uid", userId);
            vehCmd.Parameters.AddWithValue("plate", $"AB-{userId}-XY");
            vehicleId = (int)vehCmd.ExecuteScalar()!;
        }

        (double originLat, double originLng) = Services.RouteApiService.TryGetKnownAreaCoordinates(originAddress, out var originCoords)
            ? originCoords
            : (-7.7523, 110.3812);

        using var rideCmd = new NpgsqlCommand(@"
            INSERT INTO ride (driver_id, vehicle_id, destination_faculty_id, origin_address,
                              origin_lat, origin_lng, departure_time, available_seats, cost_per_seat, status)
            VALUES (
                @uid,
                @vid,
                (SELECT faculty_id FROM faculty WHERE name = @faculty LIMIT 1),
                @origin, @lat, @lng, @departure, @seats, @cost, 'scheduled'
            )
            RETURNING ride_id;", conn, tx);

        rideCmd.Parameters.AddWithValue("uid", userId);
        rideCmd.Parameters.AddWithValue("vid", vehicleId);
        rideCmd.Parameters.AddWithValue("faculty", facultyName);
        rideCmd.Parameters.AddWithValue("origin", originAddress);
        rideCmd.Parameters.AddWithValue("lat", originLat);
        rideCmd.Parameters.AddWithValue("lng", originLng);
        rideCmd.Parameters.AddWithValue("departure", departureTime);
        rideCmd.Parameters.AddWithValue("seats", (short)seats);
        rideCmd.Parameters.AddWithValue("cost", costPerSeat);

        int rideId = rideCmd.ExecuteScalar() is int id ? id : 0;
        tx.Commit();
        return rideId;
    }

    public List<(int RideId, string DriverName, string Origin, string Faculty, string Departs, int Seats, decimal Cost, string DriverNim, string DriverHobbies, string DriverBio)> FindRidesWithOriginAndProfile(
        string facultyName,
        string? pickupQuery = null)
    {
        var list = new List<(int, string, string, string, string, int, decimal, string, string, string)>();
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT r.ride_id,
                   u.full_name,
                   r.origin_address,
                   f.name,
                   r.departure_time,
                   r.available_seats,
                   r.cost_per_seat,
                   u.ktm_number,
                   COALESCE(u.hobbies, ''),
                   COALESCE(u.bio, '')
            FROM ride r
            JOIN users u ON u.user_id = r.driver_id
            JOIN faculty f ON f.faculty_id = r.destination_faculty_id
            WHERE f.name = @faculty AND r.status = 'scheduled' AND r.available_seats > 0
            ORDER BY r.departure_time;", conn);
        cmd.Parameters.AddWithValue("faculty", facultyName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int rideId = reader.GetInt32(0);
            string driverName = reader.GetString(1);
            string origin = reader.GetString(2);
            string fac = reader.GetString(3);
            DateTime dep = reader.GetDateTime(4);
            int seats = reader.GetInt16(5);
            decimal cost = reader.GetDecimal(6);
            string driverNim = reader.GetString(7);
            string driverHobbies = reader.GetString(8);
            string driverBio = reader.GetString(9);
            list.Add((rideId, driverName, origin, fac, dep.ToString("HH:mm"), seats, cost, driverNim, driverHobbies, driverBio));
        }

        if (!string.IsNullOrWhiteSpace(pickupQuery))
        {
            string trimmed = pickupQuery.Trim();
            var matching = list
                .Where(r => r.Item3.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                         || trimmed.Contains(r.Item3, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matching.Count > 0)
            {
                return matching;
            }
        }

        return list;
    }

    public List<(int RideId, string DriverName, string Origin, string Faculty, string Departs, int Seats, decimal Cost)> FindRidesWithOrigin(
        string facultyName,
        string? pickupQuery = null) =>
        FindRidesWithOriginAndProfile(facultyName, pickupQuery)
            .Select(r => (r.RideId, r.DriverName, r.Origin, r.Faculty, r.Departs, r.Seats, r.Cost))
            .ToList();

    public List<(int RideId, string DriverName, string Faculty, string Departs, int Seats, decimal Cost)> FindRides(string facultyName) =>
        FindRidesWithOrigin(facultyName)
            .Select(r => (r.RideId, r.DriverName, r.Faculty, r.Departs, r.Seats, r.Cost))
            .ToList();

    public double GetApprovedPassengerCo2Saved(string passengerNim)
    {
        if (string.IsNullOrWhiteSpace(passengerNim))
        {
            return 0.0;
        }

        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*)
            FROM ride_request rr
            JOIN users u ON u.user_id = rr.passenger_id
            WHERE u.ktm_number = @nim
              AND rr.status = 'accepted'::request_status_type;", conn);
        cmd.Parameters.AddWithValue("nim", passengerNim.Trim());

        long approvedCount = cmd.ExecuteScalar() is long c ? c : 0L;
        return Math.Round(approvedCount * 1.2, 2, MidpointRounding.AwayFromZero);
    }

    public List<(int RideId, string Origin, string Faculty, string Departs, int Seats, decimal Cost)> GetDriverRides(string driverNim)
    {
        var list = new List<(int, string, string, string, int, decimal)>();
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT r.ride_id, r.origin_address, f.name, r.departure_time, r.available_seats, r.cost_per_seat
            FROM ride r
            JOIN users u ON u.user_id = r.driver_id
            JOIN faculty f ON f.faculty_id = r.destination_faculty_id
            WHERE u.ktm_number = @nim AND r.status = 'scheduled'
            ORDER BY r.departure_time;", conn);
        cmd.Parameters.AddWithValue("nim", driverNim);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int rideId = reader.GetInt32(0);
            string origin = reader.GetString(1);
            string fac = reader.GetString(2);
            DateTime dep = reader.GetDateTime(3);
            int seats = reader.GetInt16(4);
            decimal cost = reader.GetDecimal(5);
            list.Add((rideId, origin, fac, dep.ToString("HH:mm"), seats, cost));
        }

        return list;
    }

    public List<(int RequestId, int RideId, string PassengerName, string PickupAddress, string Faculty, string Status, string PassengerNim, string Hobbies, string Bio)> GetIncomingRequestsWithProfile(string driverNim)
    {
        var list = new List<(int, int, string, string, string, string, string, string, string)>();
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT rr.request_id,
                   rr.ride_id,
                   pu.full_name,
                   rr.pickup_address,
                   COALESCE(pf.name, rf.name),
                   rr.status::text,
                   pu.ktm_number,
                   COALESCE(pu.hobbies, ''),
                   COALESCE(pu.bio, '')
            FROM ride_request rr
            JOIN ride r ON r.ride_id = rr.ride_id
            JOIN users du ON du.user_id = r.driver_id
            JOIN users pu ON pu.user_id = rr.passenger_id
            LEFT JOIN faculty pf ON pf.faculty_id = pu.faculty_id
            JOIN faculty rf ON rf.faculty_id = r.destination_faculty_id
            WHERE du.ktm_number = @nim
              AND rr.status IN ('pending', 'accepted', 'rejected')
            ORDER BY rr.requested_at DESC, rr.request_id DESC;", conn);
        cmd.Parameters.AddWithValue("nim", driverNim);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int requestId = reader.GetInt32(0);
            int rideId = reader.GetInt32(1);
            string passengerName = reader.GetString(2);
            string pickupAddress = reader.GetString(3);
            string faculty = reader.GetString(4);
            string status = reader.GetString(5);
            string paxNim = reader.GetString(6);
            string hobbies = reader.GetString(7);
            string bio = reader.GetString(8);
            list.Add((requestId, rideId, passengerName, pickupAddress, faculty, status, paxNim, hobbies, bio));
        }

        return list;
    }

    public List<(int RequestId, int RideId, string PassengerName, string PickupAddress, string Faculty, string Status)> GetIncomingRequests(string driverNim) =>
        GetIncomingRequestsWithProfile(driverNim)
            .Select(r => (r.RequestId, r.RideId, r.PassengerName, r.PickupAddress, r.Faculty, r.Status))
            .ToList();

    public int InsertRequest(int rideId, string passengerNim, string pickupAddress)
    {
        using var conn = db.Open();
        using var tx = conn.BeginTransaction();

        using var userCmd = new NpgsqlCommand(
            "SELECT user_id FROM users WHERE ktm_number = @nim LIMIT 1;", conn, tx);
        userCmd.Parameters.AddWithValue("nim", passengerNim);
        if (userCmd.ExecuteScalar() is not int userId)
        {
            return 0;
        }

        (double pickupLat, double pickupLng) = Services.RouteApiService.TryGetKnownAreaCoordinates(pickupAddress, out var pickupCoords)
            ? pickupCoords
            : (-7.7523, 110.3812);

        using (var paxCmd = new NpgsqlCommand(@"
            INSERT INTO passenger (user_id, home_address, default_pickup_lat, default_pickup_lng)
            VALUES (@uid, @pickup, @lat, @lng)
            ON CONFLICT (user_id) DO UPDATE SET
                home_address = EXCLUDED.home_address,
                default_pickup_lat = EXCLUDED.default_pickup_lat,
                default_pickup_lng = EXCLUDED.default_pickup_lng;", conn, tx))
        {
            paxCmd.Parameters.AddWithValue("uid", userId);
            paxCmd.Parameters.AddWithValue("pickup", pickupAddress);
            paxCmd.Parameters.AddWithValue("lat", pickupLat);
            paxCmd.Parameters.AddWithValue("lng", pickupLng);
            paxCmd.ExecuteNonQuery();
        }

        using var reqCmd = new NpgsqlCommand(@"
            INSERT INTO ride_request (ride_id, passenger_id, pickup_address, pickup_lat, pickup_lng, status)
            VALUES (@rideId, @uid, @pickup, @lat, @lng, 'pending')
            ON CONFLICT (ride_id, passenger_id) DO UPDATE SET
                pickup_address = EXCLUDED.pickup_address,
                pickup_lat = EXCLUDED.pickup_lat,
                pickup_lng = EXCLUDED.pickup_lng
            RETURNING request_id;", conn, tx);
        reqCmd.Parameters.AddWithValue("rideId", rideId);
        reqCmd.Parameters.AddWithValue("uid", userId);
        reqCmd.Parameters.AddWithValue("pickup", pickupAddress);
        reqCmd.Parameters.AddWithValue("lat", pickupLat);
        reqCmd.Parameters.AddWithValue("lng", pickupLng);

        int reqId = reqCmd.ExecuteScalar() is int id ? id : 0;
        tx.Commit();
        return reqId;
    }

    public bool ApproveRequest(int requestId) =>
        UpdateRequestStatus(requestId, "accepted");

    public bool RejectRequest(int requestId) =>
        UpdateRequestStatus(requestId, "rejected");

    public bool UpdateRequestStatus(int requestId, string status)
    {
        string mappedStatus = MapRequestStatus(status);
        if (mappedStatus is not ("pending" or "accepted" or "rejected" or "cancelled"))
        {
            return false;
        }

        using var conn = db.Open();

        if (mappedStatus == "accepted")
        {
            using var tx = conn.BeginTransaction();

            int rideId;
            string currentStatus;
            short availableSeats;

            using (var lockCmd = new NpgsqlCommand(@"
                SELECT rr.ride_id, rr.status::text, r.available_seats
                FROM ride_request rr
                JOIN ride r ON r.ride_id = rr.ride_id
                WHERE rr.request_id = @id
                FOR UPDATE;", conn, tx))
            {
                lockCmd.Parameters.AddWithValue("id", requestId);
                using var reader = lockCmd.ExecuteReader();
                if (!reader.Read())
                {
                    return false;
                }

                rideId = reader.GetInt32(0);
                currentStatus = reader.GetString(1);
                availableSeats = reader.GetInt16(2);
            }

            if (currentStatus != "pending" || availableSeats <= 0)
            {
                return false;
            }

            using (var seatCmd = new NpgsqlCommand(@"
                UPDATE ride
                SET available_seats = available_seats - 1
                WHERE ride_id = @rideId AND available_seats > 0;", conn, tx))
            {
                seatCmd.Parameters.AddWithValue("rideId", rideId);
                if (seatCmd.ExecuteNonQuery() != 1)
                {
                    return false;
                }
            }

            using (var reqCmd = new NpgsqlCommand(@"
                UPDATE ride_request
                SET status = 'accepted'::request_status_type, responded_at = now()
                WHERE request_id = @id AND status = 'pending'::request_status_type;", conn, tx))
            {
                reqCmd.Parameters.AddWithValue("id", requestId);
                if (reqCmd.ExecuteNonQuery() != 1)
                {
                    return false;
                }
            }

            tx.Commit();
            return true;
        }

        using var cmd = new NpgsqlCommand(@"
            UPDATE ride_request
            SET status = @status::request_status_type, responded_at = now()
            WHERE request_id = @id AND status = 'pending'::request_status_type;", conn);
        cmd.Parameters.AddWithValue("id", requestId);
        cmd.Parameters.AddWithValue("status", mappedStatus);
        return cmd.ExecuteNonQuery() == 1;
    }

    public (int TotalRides, int ApprovedRequests, double TotalCo2SavedKg, List<(string Name, string Nim, string Faculty, string Role, int SharedRides, double Co2SavedKg)> Leaderboard) GetAdminAnalytics()
    {
        using var conn = db.Open();

        int totalRides = 0;
        using (var rideCmd = new NpgsqlCommand("SELECT COUNT(*) FROM ride;", conn))
        {
            if (rideCmd.ExecuteScalar() is long rc)
            {
                totalRides = (int)rc;
            }
        }

        int approvedRequests = 0;
        using (var reqCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM ride_request WHERE status = 'accepted'::request_status_type;", conn))
        {
            if (reqCmd.ExecuteScalar() is long ac)
            {
                approvedRequests = (int)ac;
            }
        }

        double loggedCo2 = 0.0;
        using (var co2Cmd = new NpgsqlCommand("SELECT COALESCE(SUM(co2_saved_kg), 0) FROM co2_log;", conn))
        {
            object? val = co2Cmd.ExecuteScalar();
            if (val is decimal dec)
            {
                loggedCo2 = (double)dec;
            }
            else if (val is double dbl)
            {
                loggedCo2 = dbl;
            }
        }

        double totalCo2 = Math.Round(Math.Max(loggedCo2, (totalRides + approvedRequests) * 1.2), 2, MidpointRounding.AwayFromZero);

        var leaderboard = new List<(string Name, string Nim, string Faculty, string Role, int SharedRides, double Co2SavedKg)>();
        using (var lbCmd = new NpgsqlCommand(@"
            SELECT u.full_name,
                   COALESCE(u.ktm_number, '-'),
                   COALESCE(f.name, 'Engineering (FT)'),
                   u.role::text,
                   (SELECT COUNT(*) FROM ride r WHERE r.driver_id = u.user_id)
                   + (SELECT COUNT(*) FROM ride_request rr WHERE rr.passenger_id = u.user_id AND rr.status = 'accepted'::request_status_type) AS shared_count
            FROM users u
            LEFT JOIN faculty f ON f.faculty_id = u.faculty_id
            WHERE u.role IN ('driver'::user_role_type, 'passenger'::user_role_type)
            ORDER BY shared_count DESC, u.full_name ASC
            LIMIT 10;", conn))
        {
            using var r = lbCmd.ExecuteReader();
            while (r.Read())
            {
                string name = r.GetString(0);
                string nim = r.GetString(1);
                string faculty = r.GetString(2);
                string rawRole = r.GetString(3);
                string role = rawRole.Length > 0
                    ? char.ToUpperInvariant(rawRole[0]) + rawRole[1..].ToLowerInvariant()
                    : "Passenger";
                int shared = Convert.ToInt32(r.GetValue(4));
                double co2 = Math.Round(shared * 1.2, 2, MidpointRounding.AwayFromZero);
                leaderboard.Add((name, nim, faculty, role, shared, co2));
            }
        }

        return (totalRides, approvedRequests, totalCo2, leaderboard);
    }
}
