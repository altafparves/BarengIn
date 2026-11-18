using BarengIn.Domain;
using BarengIn.Domain.Enums;
using BarengIn.Domain.ValueObjects;
using Npgsql;

namespace BarengIn.Infrastructure.Persistence;

public sealed class RideRepository
{
    private readonly Database db;

    public RideRepository(Database db) => this.db = db;

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
        using var cmd = new NpgsqlCommand(@"
            WITH drv AS (
                SELECT u.user_id FROM users u WHERE u.ktm_number = @nim LIMIT 1
            ),
            ins_drv AS (
                INSERT INTO driver (user_id, license_number)
                SELECT user_id, 'AB-' || user_id || '-UGM' FROM drv
                ON CONFLICT (user_id) DO NOTHING
            ),
            ins_veh AS (
                INSERT INTO vehicle (driver_id, plate_number, model, color, seat_capacity)
                SELECT user_id, 'AB-' || user_id || '-XY', 'Honda Jazz', 'White', 4 FROM drv
                ON CONFLICT (plate_number) DO NOTHING
            )
            INSERT INTO ride (driver_id, vehicle_id, destination_faculty_id, origin_address,
                              origin_lat, origin_lng, departure_time, available_seats, cost_per_seat, status)
            VALUES (
                (SELECT user_id FROM drv),
                (SELECT v.vehicle_id FROM vehicle v JOIN drv ON v.driver_id = drv.user_id LIMIT 1),
                (SELECT faculty_id FROM faculty WHERE name = @faculty LIMIT 1),
                @origin, -7.75, 110.38, @departure, @seats, @cost, 'scheduled'
            )
            RETURNING ride_id;", conn);

        cmd.Parameters.AddWithValue("nim", driverNim);
        cmd.Parameters.AddWithValue("faculty", facultyName);
        cmd.Parameters.AddWithValue("origin", originAddress);
        cmd.Parameters.AddWithValue("departure", departureTime);
        cmd.Parameters.AddWithValue("seats", (short)seats);
        cmd.Parameters.AddWithValue("cost", costPerSeat);

        object? result = cmd.ExecuteScalar();
        return result is int id ? id : 0;
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

    public int InsertRequest(int rideId, string passengerNim, string pickupAddress)
    {
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            WITH pax AS (
                SELECT u.user_id FROM users u WHERE u.ktm_number = @nim LIMIT 1
            ),
            ins_pax AS (
                INSERT INTO passenger (user_id, home_address, default_pickup_lat, default_pickup_lng)
                SELECT user_id, @pickup, -7.75, 110.38 FROM pax
                ON CONFLICT (user_id) DO NOTHING
            )
            INSERT INTO ride_request (ride_id, passenger_id, pickup_address, pickup_lat, pickup_lng, status)
            VALUES (@rideId, (SELECT user_id FROM pax), @pickup, -7.75, 110.38, 'pending')
            ON CONFLICT (ride_id, passenger_id) DO UPDATE SET pickup_address = EXCLUDED.pickup_address
            RETURNING request_id;", conn);

        cmd.Parameters.AddWithValue("rideId", rideId);
        cmd.Parameters.AddWithValue("nim", passengerNim);
        cmd.Parameters.AddWithValue("pickup", pickupAddress);

        object? result = cmd.ExecuteScalar();
        return result is int id ? id : 0;
    }

    public bool UpdateRequestStatus(int requestId, string status)
    {
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            UPDATE ride_request
            SET status = @status::request_status_type, responded_at = now()
            WHERE request_id = @id;", conn);
        cmd.Parameters.AddWithValue("id", requestId);
        cmd.Parameters.AddWithValue("status", status.ToLowerInvariant());
        return cmd.ExecuteNonQuery() == 1;
    }
}
