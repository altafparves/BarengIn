-- ================================================================
-- BarengIn (Campus Carpool) - PostgreSQL Database Schema
-- Module 5: Pembuatan Basis Data
-- ================================================================
-- HOW TO USE:
--   1. CREATE DATABASE "BarengIn";        (run once, as postgres)
--   2. \c "BarengIn"
--   3. \i 'path/to/schema.sql'            (or paste into pgAdmin Query Tool)
--
-- The script is re-runnable: it drops every table and type first,
-- so teammates can reset their local database by running it again.
-- WARNING: this DELETES all existing data in these tables.
-- ================================================================

-- ---------------------------------------------------------------
-- RESET (children first, then parents, then types)
-- ---------------------------------------------------------------
DROP TABLE IF EXISTS rating           CASCADE;
DROP TABLE IF EXISTS report           CASCADE;
DROP TABLE IF EXISTS co2_log          CASCADE;
DROP TABLE IF EXISTS location_log     CASCADE;
DROP TABLE IF EXISTS payment          CASCADE;
DROP TABLE IF EXISTS ride_request     CASCADE;
DROP TABLE IF EXISTS ride             CASCADE;
DROP TABLE IF EXISTS vehicle          CASCADE;
DROP TABLE IF EXISTS schedule         CASCADE;
DROP TABLE IF EXISTS ktm_verification CASCADE;
DROP TABLE IF EXISTS admin            CASCADE;
DROP TABLE IF EXISTS driver           CASCADE;
DROP TABLE IF EXISTS passenger        CASCADE;
DROP TABLE IF EXISTS users            CASCADE;
DROP TABLE IF EXISTS faculty          CASCADE;

DROP TYPE IF EXISTS report_status_type;
DROP TYPE IF EXISTS report_reason_type;
DROP TYPE IF EXISTS payment_status_type;
DROP TYPE IF EXISTS payment_method_type;
DROP TYPE IF EXISTS request_status_type;
DROP TYPE IF EXISTS ride_status_type;
DROP TYPE IF EXISTS admin_level_type;
DROP TYPE IF EXISTS ktm_status_type;
DROP TYPE IF EXISTS verification_status_type;
DROP TYPE IF EXISTS user_role_type;

-- ---------------------------------------------------------------
-- ENUM TYPES
-- ---------------------------------------------------------------
CREATE TYPE user_role_type           AS ENUM ('passenger', 'driver', 'admin');
CREATE TYPE verification_status_type AS ENUM ('unverified', 'pending', 'verified', 'rejected');
CREATE TYPE ktm_status_type          AS ENUM ('pending', 'approved', 'rejected');
CREATE TYPE admin_level_type         AS ENUM ('staff', 'super_admin');
CREATE TYPE ride_status_type         AS ENUM ('scheduled', 'ongoing', 'completed', 'cancelled');
CREATE TYPE request_status_type      AS ENUM ('pending', 'accepted', 'rejected', 'cancelled');
CREATE TYPE payment_method_type      AS ENUM ('cash', 'e_wallet', 'bank_transfer');
CREATE TYPE payment_status_type      AS ENUM ('unpaid', 'paid', 'refunded');
CREATE TYPE report_reason_type       AS ENUM ('harassment', 'reckless_driving', 'no_show', 'other');
CREATE TYPE report_status_type       AS ENUM ('open', 'investigating', 'resolved', 'dismissed');

-- ---------------------------------------------------------------
-- FACULTY (no dependencies)
-- ---------------------------------------------------------------
CREATE TABLE faculty (
    faculty_id  SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL UNIQUE,
    short_name  VARCHAR(20),
    latitude    DECIMAL(10,7) CHECK (latitude  BETWEEN -90  AND 90),
    longitude   DECIMAL(10,7) CHECK (longitude BETWEEN -180 AND 180)
);

-- ---------------------------------------------------------------
-- USERS  (named "users": "user" is a reserved word in PostgreSQL)
-- Design note: one user has exactly one role (users.role). The
-- passenger / driver / admin tables are 1-to-(0 or 1) profile
-- extensions that share the user's primary key.
-- ---------------------------------------------------------------
CREATE TABLE users (
    user_id             SERIAL PRIMARY KEY,
    faculty_id          INT REFERENCES faculty(faculty_id),
    full_name           VARCHAR(100) NOT NULL,
    email               VARCHAR(100) NOT NULL UNIQUE,
    phone_number        VARCHAR(20)  NOT NULL UNIQUE,
    password_hash       VARCHAR(255) NOT NULL,
    ktm_number          VARCHAR(20)  UNIQUE,
    profile_photo_url   VARCHAR(255),
    bio                 VARCHAR(255),
    hobbies             VARCHAR(255),
    role                user_role_type NOT NULL,
    verification_status verification_status_type NOT NULL DEFAULT 'unverified',
    created_at          TIMESTAMP NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------
-- PASSENGER / DRIVER / ADMIN
-- PK is also the FK back to users, so there is no separate id.
-- ON DELETE CASCADE: removing a user removes the profile row.
-- ---------------------------------------------------------------
CREATE TABLE passenger (
    user_id             INT PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
    home_address        VARCHAR(255),
    default_pickup_lat  DECIMAL(10,7) CHECK (default_pickup_lat BETWEEN -90  AND 90),
    default_pickup_lng  DECIMAL(10,7) CHECK (default_pickup_lng BETWEEN -180 AND 180)
);

-- rating_avg and total_rides_completed are derived values: the
-- application (or a trigger) must keep them in sync.
CREATE TABLE driver (
    user_id               INT PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
    license_number        VARCHAR(50) NOT NULL UNIQUE,
    rating_avg            DECIMAL(3,2) NOT NULL DEFAULT 0 CHECK (rating_avg BETWEEN 0 AND 5),
    total_rides_completed INT NOT NULL DEFAULT 0 CHECK (total_rides_completed >= 0)
);

CREATE TABLE admin (
    user_id      INT PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
    department   VARCHAR(100),
    admin_level  admin_level_type NOT NULL DEFAULT 'staff'
);

-- ---------------------------------------------------------------
-- KTM_VERIFICATION
-- ---------------------------------------------------------------
CREATE TABLE ktm_verification (
    verification_id      SERIAL PRIMARY KEY,
    user_id              INT NOT NULL REFERENCES users(user_id),
    ktm_image_url        VARCHAR(255) NOT NULL,
    status               ktm_status_type NOT NULL DEFAULT 'pending',
    verified_by_admin_id INT REFERENCES admin(user_id),  -- nullable: not reviewed yet
    submitted_at         TIMESTAMP NOT NULL DEFAULT now(),
    verified_at          TIMESTAMP,
    CHECK (verified_at IS NULL OR verified_at >= submitted_at)
);

-- ---------------------------------------------------------------
-- SCHEDULE (0 = Sunday ... 6 = Saturday)
-- ---------------------------------------------------------------
CREATE TABLE schedule (
    schedule_id  SERIAL PRIMARY KEY,
    user_id      INT NOT NULL REFERENCES users(user_id),
    faculty_id   INT NOT NULL REFERENCES faculty(faculty_id),
    day_of_week  SMALLINT NOT NULL CHECK (day_of_week BETWEEN 0 AND 6),
    start_time   TIME NOT NULL,
    end_time     TIME NOT NULL,
    course_name  VARCHAR(100),
    CHECK (end_time > start_time)
);

-- ---------------------------------------------------------------
-- VEHICLE
-- ---------------------------------------------------------------
CREATE TABLE vehicle (
    vehicle_id     SERIAL PRIMARY KEY,
    driver_id      INT NOT NULL REFERENCES driver(user_id),
    plate_number   VARCHAR(15) NOT NULL UNIQUE,
    model          VARCHAR(50),
    color          VARCHAR(30),
    seat_capacity  SMALLINT NOT NULL CHECK (seat_capacity > 0)
);

-- ---------------------------------------------------------------
-- RIDE
-- ---------------------------------------------------------------
CREATE TABLE ride (
    ride_id                SERIAL PRIMARY KEY,
    driver_id              INT NOT NULL REFERENCES driver(user_id),
    vehicle_id             INT NOT NULL REFERENCES vehicle(vehicle_id),
    destination_faculty_id INT NOT NULL REFERENCES faculty(faculty_id),
    origin_address         VARCHAR(255) NOT NULL,
    origin_lat             DECIMAL(10,7) CHECK (origin_lat BETWEEN -90  AND 90),
    origin_lng             DECIMAL(10,7) CHECK (origin_lng BETWEEN -180 AND 180),
    departure_time         TIMESTAMP NOT NULL,
    available_seats        SMALLINT NOT NULL CHECK (available_seats >= 0),
    cost_per_seat          DECIMAL(10,2) NOT NULL CHECK (cost_per_seat >= 0),
    status                 ride_status_type NOT NULL DEFAULT 'scheduled',
    created_at             TIMESTAMP NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------
-- RIDE_REQUEST (one request per passenger per ride)
-- ---------------------------------------------------------------
CREATE TABLE ride_request (
    request_id     SERIAL PRIMARY KEY,
    ride_id        INT NOT NULL REFERENCES ride(ride_id),
    passenger_id   INT NOT NULL REFERENCES passenger(user_id),
    pickup_address VARCHAR(255) NOT NULL,
    pickup_lat     DECIMAL(10,7) CHECK (pickup_lat BETWEEN -90  AND 90),
    pickup_lng     DECIMAL(10,7) CHECK (pickup_lng BETWEEN -180 AND 180),
    status         request_status_type NOT NULL DEFAULT 'pending',
    requested_at   TIMESTAMP NOT NULL DEFAULT now(),
    responded_at   TIMESTAMP,
    UNIQUE (ride_id, passenger_id)
);

-- ---------------------------------------------------------------
-- PAYMENT (1-to-(0 or 1) with ride_request -> UNIQUE the FK)
-- ---------------------------------------------------------------
CREATE TABLE payment (
    payment_id     SERIAL PRIMARY KEY,
    request_id     INT NOT NULL UNIQUE REFERENCES ride_request(request_id),
    amount         DECIMAL(10,2) NOT NULL CHECK (amount >= 0),
    payment_method payment_method_type NOT NULL,
    payment_status payment_status_type NOT NULL DEFAULT 'unpaid',
    paid_at        TIMESTAMP
);

-- ---------------------------------------------------------------
-- LOCATION_LOG
-- ---------------------------------------------------------------
CREATE TABLE location_log (
    location_id  BIGSERIAL PRIMARY KEY,
    ride_id      INT NOT NULL REFERENCES ride(ride_id),
    latitude     DECIMAL(10,7) NOT NULL CHECK (latitude  BETWEEN -90  AND 90),
    longitude    DECIMAL(10,7) NOT NULL CHECK (longitude BETWEEN -180 AND 180),
    recorded_at  TIMESTAMP NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------
-- CO2_LOG (1-to-(0 or 1) with ride -> UNIQUE the FK)
-- ---------------------------------------------------------------
CREATE TABLE co2_log (
    co2_log_id    SERIAL PRIMARY KEY,
    ride_id       INT NOT NULL UNIQUE REFERENCES ride(ride_id),
    distance_km   DECIMAL(6,2) NOT NULL CHECK (distance_km >= 0),
    co2_saved_kg  DECIMAL(6,2) NOT NULL CHECK (co2_saved_kg >= 0),
    calculated_at TIMESTAMP NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------
-- REPORT
-- ---------------------------------------------------------------
CREATE TABLE report (
    report_id            SERIAL PRIMARY KEY,
    reporter_id          INT NOT NULL REFERENCES users(user_id),
    reported_id          INT NOT NULL REFERENCES users(user_id),
    ride_id              INT REFERENCES ride(ride_id),   -- nullable: not every report is ride-specific
    reason               report_reason_type NOT NULL,
    description          VARCHAR(500),
    status               report_status_type NOT NULL DEFAULT 'open',
    resolved_by_admin_id INT REFERENCES admin(user_id),  -- nullable: not resolved yet
    created_at           TIMESTAMP NOT NULL DEFAULT now(),
    CHECK (reporter_id <> reported_id)
);

-- ---------------------------------------------------------------
-- RATING (one rating per rater/rated pair per ride)
-- ---------------------------------------------------------------
CREATE TABLE rating (
    rating_id  SERIAL PRIMARY KEY,
    ride_id    INT NOT NULL REFERENCES ride(ride_id),
    rater_id   INT NOT NULL REFERENCES users(user_id),
    rated_id   INT NOT NULL REFERENCES users(user_id),
    score      SMALLINT NOT NULL CHECK (score BETWEEN 1 AND 5),
    comment    VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    CHECK (rater_id <> rated_id),
    UNIQUE (ride_id, rater_id, rated_id)
);

-- ---------------------------------------------------------------
-- INDEXES on foreign keys / hot lookup columns
-- (PostgreSQL indexes PK and UNIQUE columns automatically, not FKs.
--  ride_request(ride_id) is already covered by its UNIQUE constraint.)
-- ---------------------------------------------------------------
CREATE INDEX idx_users_faculty_id          ON users(faculty_id);
CREATE INDEX idx_ktm_verification_user_id  ON ktm_verification(user_id);
CREATE INDEX idx_ktm_verification_admin_id ON ktm_verification(verified_by_admin_id);
CREATE INDEX idx_schedule_user_id          ON schedule(user_id);
CREATE INDEX idx_schedule_faculty_id       ON schedule(faculty_id);
CREATE INDEX idx_vehicle_driver_id         ON vehicle(driver_id);
CREATE INDEX idx_ride_driver_id            ON ride(driver_id);
CREATE INDEX idx_ride_vehicle_id           ON ride(vehicle_id);
CREATE INDEX idx_ride_destination_faculty  ON ride(destination_faculty_id);
CREATE INDEX idx_ride_status_departure     ON ride(status, departure_time);
CREATE INDEX idx_ride_request_passenger_id ON ride_request(passenger_id);
CREATE INDEX idx_location_log_ride_time    ON location_log(ride_id, recorded_at);
CREATE INDEX idx_report_reporter_id        ON report(reporter_id);
CREATE INDEX idx_report_reported_id        ON report(reported_id);
CREATE INDEX idx_report_ride_id            ON report(ride_id);
CREATE INDEX idx_report_resolved_admin_id  ON report(resolved_by_admin_id);
CREATE INDEX idx_rating_ride_id            ON rating(ride_id);
CREATE INDEX idx_rating_rated_id           ON rating(rated_id);

-- ---------------------------------------------------------------
-- SEED DEFAULT FACULTIES & DECLARED BARENGIN ADMINISTRATORS
-- Password for seeded admin accounts: admin123
-- ---------------------------------------------------------------
INSERT INTO faculty (name, short_name, latitude, longitude) VALUES
    ('Engineering (FT)', 'FT', -7.7654, 110.3723),
    ('MIPA', 'FMIPA', -7.7672, 110.3762),
    ('Economics and Business (FEB)', 'FEB', -7.7706, 110.3792),
    ('Medicine', 'FK-KMK', -7.7689, 110.3736),
    ('Social and Political Sciences', 'FISIPOL', -7.7701, 110.3774),
    ('Fakultas Biologi', 'Biologi', -7.7655, 110.3768),
    ('Fakultas Ekonomika dan Bisnis (FEB)', 'FEB', -7.7708, 110.3794),
    ('Fakultas Farmasi', 'Farmasi', -7.7679, 110.3752),
    ('Fakultas Filsafat', 'Filsafat', -7.7715, 110.3798),
    ('Fakultas Geografi', 'Geografi', -7.7664, 110.3766),
    ('Fakultas Hukum', 'Hukum', -7.7709, 110.3806),
    ('Fakultas Ilmu Budaya (FIB)', 'FIB', -7.7718, 110.3782),
    ('Fakultas Ilmu Sosial dan Ilmu Politik (FISIPOL)', 'FISIPOL', -7.7702, 110.3788),
    ('Fakultas Kedokteran, Kesehatan Masyarakat, dan Keperawatan (FK-KMK)', 'FK-KMK', -7.7691, 110.3738),
    ('Fakultas Kedokteran Gigi (FKG)', 'FKG', -7.7696, 110.3749),
    ('Fakultas Kedokteran Hewan (FKH)', 'FKH', -7.7681, 110.3834),
    ('Fakultas Kehutanan', 'Kehutanan', -7.7674, 110.3805),
    ('Fakultas Matematika dan Ilmu Pengetahuan Alam (FMIPA)', 'FMIPA', -7.7670, 110.3764),
    ('Fakultas Pertanian', 'Pertanian', -7.7695, 110.3818),
    ('Fakultas Peternakan', 'Peternakan', -7.7672, 110.3852),
    ('Fakultas Psikologi', 'Psikologi', -7.7724, 110.3789),
    ('Fakultas Teknik (FT)', 'FT', -7.7653, 110.3725),
    ('Fakultas Teknologi Pertanian (FTP)', 'FTP', -7.7688, 110.3802),
    ('Sekolah Vokasi (SV)', 'SV', -7.7748, 110.3742),
    ('Sekolah Pascasarjana', 'Pascasarjana', -7.7644, 110.3755),
    ('Balairung UGM (Gedung Pusat)', 'Balairung', -7.7686, 110.3785),
    ('Grha Sabha Pramana (GSP)', 'GSP', -7.7704, 110.3777),
    ('RSUP Dr. Sardjito', 'Sardjito', -7.7686, 110.3732),
    ('Rumah Sakit Akademik (RSA) UGM', 'RSA UGM', -7.7426, 110.3508),
    ('Perpustakaan Pusat UGM', 'Perpusat', -7.7697, 110.3782),
    ('Masjid Kampus UGM (Maskam)', 'Maskam', -7.7732, 110.3797),
    ('Gelanggang Inovasi dan Kreativitas (GIK)', 'GIK', -7.7745, 110.3746),
    ('Wisdom Park UGM', 'Wisdom Park', -7.7712, 110.3822),
    ('Stadion Pancasila UGM', 'Pancasila', -7.7705, 110.3831),
    ('Asrama Mahasiswa (Darmaputera / Ratnaningsih)', 'Asrama UGM', -7.7629, 110.3795)
ON CONFLICT (name) DO NOTHING;

INSERT INTO users (faculty_id, full_name, email, phone_number, password_hash, ktm_number, role, verification_status)
VALUES (
    (SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
    'BarengIn Campus Admin',
    'admin@ugm.ac.id',
    '081100000001',
    '$2a$11$0fG3q1Z6p3B0v1xY9yE8O.h8Z5X7W9V1U3T5S7R9Q1P3O5N7M9L1K',
    'ADMIN/01',
    'admin',
    'verified'
)
ON CONFLICT (ktm_number) DO NOTHING;

INSERT INTO admin (user_id, department, admin_level)
SELECT user_id, 'DTETI', 'super_admin'
FROM users
WHERE ktm_number = 'ADMIN/01'
ON CONFLICT (user_id) DO NOTHING;

-- ---------------------------------------------------------------
-- VERIFY (run manually after the script finishes)
--   \dt          -> should list all 15 tables
--   \d ride      -> ride's columns + its 3 foreign keys
-- ---------------------------------------------------------------
