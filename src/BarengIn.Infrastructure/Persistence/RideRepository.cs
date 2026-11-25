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
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO faculty (name, short_name, latitude, longitude)
                VALUES (@name, @short, -7.7654, 110.3723)
                ON CONFLICT (name) DO NOTHING;", conn);
            cmd.Parameters.AddWithValue("name", f);
            cmd.Parameters.AddWithValue("short", f.Length > 20 ? f[..20] : f);
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
            VALUES (@uid, @plate, 'Honda Jazz', 'White', 4)
            ON CONFLICT (plate_number) DO UPDATE SET model = EXCLUDED.model
            RETURNING vehicle_id;", conn, tx))
        {
            vehCmd.Parameters.AddWithValue("uid", userId);
            vehCmd.Parameters.AddWithValue("plate", $"AB-{userId}-XY");
            vehicleId = (int)vehCmd.ExecuteScalar()!;
        }

        using var rideCmd = new NpgsqlCommand(@"
            INSERT INTO ride (driver_id, vehicle_id, destination_faculty_id, origin_address,
                              origin_lat, origin_lng, departure_time, available_seats, cost_per_seat, status)
            VALUES (
                @uid,
                @vid,
                (SELECT faculty_id FROM faculty WHERE name = @faculty LIMIT 1),
                @origin, -7.75, 110.38, @departure, @seats, @cost, 'scheduled'
            )
            RETURNING ride_id;", conn, tx);

        rideCmd.Parameters.AddWithValue("uid", userId);
        rideCmd.Parameters.AddWithValue("vid", vehicleId);
        rideCmd.Parameters.AddWithValue("faculty", facultyName);
        rideCmd.Parameters.AddWithValue("origin", originAddress);
        rideCmd.Parameters.AddWithValue("departure", departureTime);
        rideCmd.Parameters.AddWithValue("seats", (short)seats);
        rideCmd.Parameters.AddWithValue("cost", costPerSeat);

        int rideId = rideCmd.ExecuteScalar() is int id ? id : 0;
        tx.Commit();
        return rideId;
    }

    public List<(int RideId, string DriverName, string Faculty, string Departs, int Seats, decimal Cost)> FindRides(string facultyName)
    {
        var list = new List<(int, string, string, string, int, decimal)>();
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT r.ride_id, u.full_name, f.name, r.departure_time, r.available_seats, r.cost_per_seat
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
            string fac = reader.GetString(2);
            DateTime dep = reader.GetDateTime(3);
            int seats = reader.GetInt16(4);
            decimal cost = reader.GetDecimal(5);
            list.Add((rideId, driverName, fac, dep.ToString("HH:mm"), seats, cost));
        }

        return list;
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

    public List<(int RequestId, int RideId, string PassengerName, string PickupAddress, string Faculty, string Status)> GetIncomingRequests(string driverNim)
    {
        var list = new List<(int, int, string, string, string, string)>();
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT rr.request_id,
                   rr.ride_id,
                   pu.full_name,
                   rr.pickup_address,
                   COALESCE(pf.name, rf.name),
                   rr.status::text
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
            list.Add((requestId, rideId, passengerName, pickupAddress, faculty, status));
        }

        return list;
    }

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

        using (var paxCmd = new NpgsqlCommand(@"
            INSERT INTO passenger (user_id, home_address, default_pickup_lat, default_pickup_lng)
            VALUES (@uid, @pickup, -7.75, 110.38)
            ON CONFLICT (user_id) DO NOTHING;", conn, tx))
        {
            paxCmd.Parameters.AddWithValue("uid", userId);
            paxCmd.Parameters.AddWithValue("pickup", pickupAddress);
            paxCmd.ExecuteNonQuery();
        }

        using var reqCmd = new NpgsqlCommand(@"
            INSERT INTO ride_request (ride_id, passenger_id, pickup_address, pickup_lat, pickup_lng, status)
            VALUES (@rideId, @uid, @pickup, -7.75, 110.38, 'pending')
            ON CONFLICT (ride_id, passenger_id) DO UPDATE SET pickup_address = EXCLUDED.pickup_address
            RETURNING request_id;", conn, tx);
        reqCmd.Parameters.AddWithValue("rideId", rideId);
        reqCmd.Parameters.AddWithValue("uid", userId);
        reqCmd.Parameters.AddWithValue("pickup", pickupAddress);

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
}
