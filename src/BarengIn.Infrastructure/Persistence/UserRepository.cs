using BarengIn.Domain;
using BarengIn.Domain.Enums;
using BarengIn.Domain.ValueObjects;
using Npgsql;

namespace BarengIn.Infrastructure.Persistence;

public sealed class UserRepository
{
    private readonly Database db;
    public UserRepository(Database db) => this.db = db;

    public bool Register(string fullName, string email, string phone, string password,
                         string ktmNumber, string facultyName, string role)
    {
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            INSERT INTO faculty (name, short_name, latitude, longitude)
            VALUES (@faculty, LEFT(@faculty, 20), -7.7654, 110.3723)
            ON CONFLICT (name) DO NOTHING;

            INSERT INTO users (faculty_id, full_name, email, phone_number, password_hash,
                               ktm_number, role, verification_status)
            VALUES ((SELECT faculty_id FROM faculty WHERE name = @faculty),
                    @name, @email, @phone, @hash, @ktm,
                    @role::user_role_type, 'pending');", conn);
        cmd.Parameters.AddWithValue("faculty", facultyName);
        cmd.Parameters.AddWithValue("name", fullName);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("phone", phone);
        cmd.Parameters.AddWithValue("hash", BCrypt.Net.BCrypt.HashPassword(password));
        cmd.Parameters.AddWithValue("ktm", ktmNumber);
        cmd.Parameters.AddWithValue("role", role.ToLower());
        return cmd.ExecuteNonQuery() >= 1;
    }

    /// <summary>
    /// Login process (Task 1). Checks the password, then builds the correct subclass of User.
    /// The caller only sees a "User", but the real object is a Passenger, Driver or Admin.
    /// Returns null if the NIM or password is wrong.
    /// </summary>
    public User? Authenticate(string ktmNumber, string password)
    {
        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT u.full_name, u.email, u.phone_number, u.password_hash, u.ktm_number,
                   u.role::text,
                   COALESCE(p.home_address, '-'),
                   COALESCE(d.license_number, '-')
            FROM users u
            LEFT JOIN passenger p ON p.user_id = u.user_id
            LEFT JOIN driver d    ON d.user_id = u.user_id
            WHERE u.ktm_number = @ktm", conn);
        cmd.Parameters.AddWithValue("ktm", ktmNumber);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        string name = r.GetString(0);
        string email = r.GetString(1);
        string phone = r.GetString(2);
        string hash = r.GetString(3);
        string niu = r.GetString(4);
        string role = r.GetString(5);
        string address = r.GetString(6);
        string license = r.GetString(7);

        if (!BCrypt.Net.BCrypt.Verify(password, hash)) return null;

        // Placeholders (Guid.Empty, GeoPoint 0,0) until faculty/location are loaded from the DB.
        User user = BuildUserForRole(niu, name, email, hash, phone, role, address, license);
        return user;
    }

    public static User BuildUserForRole(
        string niu,
        string name,
        string email,
        string hash,
        string phone,
        string role,
        string address = "Jakal Km 5",
        string license = "AB-1234-UGM")
    {
        if (role.Equals("driver", StringComparison.OrdinalIgnoreCase))
        {
            var driver = new Driver(niu, name, email, hash, phone, Guid.Empty,
                                    address, new GeoPoint(-7.75, 110.38), license);
            driver.MarkVerified();
            driver.AddVehicle(new Vehicle(driver.UserId, "AB 1234 XY", "Honda", "Jazz", "White", VehicleType.Car, 4));
            return driver;
        }

        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            var admin = new Admin(niu, name, email, hash, phone, Guid.Empty, "DTETI", 1);
            admin.MarkVerified();
            return admin;
        }

        var passenger = new Passenger(niu, name, email, hash, phone, Guid.Empty,
                                      address, new GeoPoint(-7.75, 110.38));
        passenger.MarkVerified();
        return passenger;
    }
}
