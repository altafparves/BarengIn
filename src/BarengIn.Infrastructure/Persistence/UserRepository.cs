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
        using var tx = conn.BeginTransaction();

        using (var facCmd = new NpgsqlCommand(@"
            INSERT INTO faculty (name, short_name, latitude, longitude)
            VALUES (@faculty, LEFT(@faculty, 20), -7.7654, 110.3723)
            ON CONFLICT (name) DO NOTHING;", conn, tx))
        {
            facCmd.Parameters.AddWithValue("faculty", facultyName);
            facCmd.ExecuteNonQuery();
        }

        int userId;
        using (var usrCmd = new NpgsqlCommand(@"
            INSERT INTO users (faculty_id, full_name, email, phone_number, password_hash,
                               ktm_number, role, verification_status)
            VALUES ((SELECT faculty_id FROM faculty WHERE name = @faculty),
                    @name, @email, @phone, @hash, @ktm,
                    @role::user_role_type, 'pending')
            RETURNING user_id;", conn, tx))
        {
            usrCmd.Parameters.AddWithValue("faculty", facultyName);
            usrCmd.Parameters.AddWithValue("name", fullName);
            usrCmd.Parameters.AddWithValue("email", email);
            usrCmd.Parameters.AddWithValue("phone", phone);
            usrCmd.Parameters.AddWithValue("hash", BCrypt.Net.BCrypt.HashPassword(password));
            usrCmd.Parameters.AddWithValue("ktm", ktmNumber);
            usrCmd.Parameters.AddWithValue("role", role.ToLowerInvariant());
            userId = (int)usrCmd.ExecuteScalar()!;
        }

        if (role.Equals("driver", StringComparison.OrdinalIgnoreCase))
        {
            using var drvCmd = new NpgsqlCommand(@"
                INSERT INTO driver (user_id, license_number)
                VALUES (@uid, @lic)
                ON CONFLICT (user_id) DO NOTHING;", conn, tx);
            drvCmd.Parameters.AddWithValue("uid", userId);
            drvCmd.Parameters.AddWithValue("lic", $"AB-{userId}-UGM");
            drvCmd.ExecuteNonQuery();
        }
        else if (!role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            using var paxCmd = new NpgsqlCommand(@"
                INSERT INTO passenger (user_id, home_address, default_pickup_lat, default_pickup_lng)
                VALUES (@uid, '-', -7.75, 110.38)
                ON CONFLICT (user_id) DO NOTHING;", conn, tx);
            paxCmd.Parameters.AddWithValue("uid", userId);
            paxCmd.ExecuteNonQuery();
        }

        tx.Commit();
        return userId > 0;
    }

    public bool UpdateProfile(string ktmNumber, string facultyName, string hobbies, string bio)
    {
        if (string.IsNullOrWhiteSpace(ktmNumber))
        {
            return false;
        }

        string trimmedKtm = ktmNumber.Trim();
        string trimmedFaculty = (facultyName ?? string.Empty).Trim();
        string trimmedHobbies = (hobbies ?? string.Empty).Trim();
        string trimmedBio = (bio ?? string.Empty).Trim();

        using var conn = db.Open();
        using var tx = conn.BeginTransaction();

        if (!string.IsNullOrEmpty(trimmedFaculty))
        {
            using var facCmd = new NpgsqlCommand(@"
                INSERT INTO faculty (name, short_name, latitude, longitude)
                VALUES (@faculty, LEFT(@faculty, 20), -7.7654, 110.3723)
                ON CONFLICT (name) DO NOTHING;", conn, tx);
            facCmd.Parameters.AddWithValue("faculty", trimmedFaculty);
            facCmd.ExecuteNonQuery();
        }

        using var usrCmd = new NpgsqlCommand(@"
            UPDATE users
            SET faculty_id = COALESCE((SELECT faculty_id FROM faculty WHERE name = @faculty), faculty_id),
                hobbies = @hobbies,
                bio = @bio
            WHERE ktm_number = @ktm;", conn, tx);
        usrCmd.Parameters.AddWithValue("faculty", trimmedFaculty);
        usrCmd.Parameters.AddWithValue("hobbies", trimmedHobbies);
        usrCmd.Parameters.AddWithValue("bio", trimmedBio);
        usrCmd.Parameters.AddWithValue("ktm", trimmedKtm);

        int rows = usrCmd.ExecuteNonQuery();
        if (rows != 1)
        {
            return false;
        }

        tx.Commit();
        return true;
    }

    /// <summary>
    /// Login process (Task 1). Checks the password, then builds the correct subclass of User.
    /// The caller only sees a "User", but the real object is a Passenger, Driver or Admin.
    /// Returns null if the NIM or password is wrong.
    /// </summary>
    public User? Authenticate(string ktmNumber, string password) =>
        Authenticate(ktmNumber, password, null, out _, out _, out _);

    public User? Authenticate(
        string ktmNumber,
        string password,
        out string facultyName,
        out string hobbies,
        out string bio) =>
        Authenticate(ktmNumber, password, null, out facultyName, out hobbies, out bio);

    public User? Authenticate(
        string ktmNumber,
        string password,
        string? selectedRole,
        out string facultyName,
        out string hobbies,
        out string bio)
    {
        facultyName = string.Empty;
        hobbies = string.Empty;
        bio = string.Empty;

        using var conn = db.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT u.full_name, u.email, u.phone_number, u.password_hash, u.ktm_number,
                   u.role::text,
                   COALESCE(p.home_address, '-'),
                   COALESCE(d.license_number, '-'),
                   u.verification_status::text,
                   COALESCE(f.name, ''),
                   COALESCE(u.hobbies, ''),
                   COALESCE(u.bio, '')
            FROM users u
            LEFT JOIN passenger p ON p.user_id = u.user_id
            LEFT JOIN driver d    ON d.user_id = u.user_id
            LEFT JOIN faculty f   ON f.faculty_id = u.faculty_id
            WHERE u.ktm_number = @ktm", conn);
        cmd.Parameters.AddWithValue("ktm", ktmNumber);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        string name = r.GetString(0);
        string email = r.GetString(1);
        string phone = r.GetString(2);
        string hash = r.GetString(3);
        string niu = r.GetString(4);
        string dbRole = r.GetString(5);
        string address = r.GetString(6);
        string license = r.GetString(7);
        string verificationStatus = r.GetString(8);
        string loadedFaculty = r.GetString(9);
        string loadedHobbies = r.GetString(10);
        string loadedBio = r.GetString(11);

        if (!BCrypt.Net.BCrypt.Verify(password, hash)) return null;

        facultyName = loadedFaculty;
        hobbies = loadedHobbies;
        bio = loadedBio;

        string effectiveRole = !string.IsNullOrWhiteSpace(selectedRole) ? selectedRole : dbRole;

        bool isVerified = !verificationStatus.Equals("unverified", StringComparison.OrdinalIgnoreCase)
                       && !verificationStatus.Equals("rejected", StringComparison.OrdinalIgnoreCase);

        User user = BuildUserForRole(niu, name, email, hash, phone, effectiveRole, address, license, isVerified);
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(phone))
        {
            user.UpdateProfile(name, phone, bio);
        }

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
        string license = "AB-1234-UGM",
        bool verified = true)
    {
        if (role.Equals("driver", StringComparison.OrdinalIgnoreCase))
        {
            var driver = new Driver(niu, name, email, hash, phone, Guid.Empty,
                                    address, new GeoPoint(-7.75, 110.38), license);
            if (verified)
            {
                driver.MarkVerified();
            }

            driver.AddVehicle(new Vehicle(driver.UserId, "AB 1234 XY", "Honda", "Jazz", "White", VehicleType.Car, 4));
            return driver;
        }

        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            var admin = new Admin(niu, name, email, hash, phone, Guid.Empty, "DTETI", 1);
            if (verified)
            {
                admin.MarkVerified();
            }

            return admin;
        }

        var passenger = new Passenger(niu, name, email, hash, phone, Guid.Empty,
                                      address, new GeoPoint(-7.75, 110.38));
        if (verified)
        {
            passenger.MarkVerified();
        }

        return passenger;
    }
}
