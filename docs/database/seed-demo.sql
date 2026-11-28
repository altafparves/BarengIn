-- ================================================================
-- BarengIn (Campus Carpool) - Cloud Demo Seed Data
-- Run AFTER docs/database/schema.sql on Neon / Cloud PostgreSQL:
--   psql "$DATABASE_URL_UNPOOLED" -f docs/database/schema.sql -f docs/database/seed-demo.sql
--
-- Idempotent: safe to re-run before every demo video take.
--
-- Demo Credentials:
--   Admin      (password: admin123)
--     - admin@ugm.ac.id
--
--   Drivers    (password: demo123)
--     - Driver 1: Satrio  | 24/510001/TK/58001 | satrio.driver@mail.ugm.ac.id  (Verified, Mazda 6 Red, active rides to Engineering (FT))
--     - Driver 2: Nalen   | 24/510002/EK/58002 | nalen.driver@mail.ugm.ac.id   (Verified, Toyota Fortuner, Seturan to Economics and Business (FEB))
--
--   Passengers (password: demo123)
--     - Passenger 1: Vino   | 24/520001/TK/59001 | vino.pax@mail.ugm.ac.id       (Verified, Engineering (FT), pickup at Pogung Dalangan)
--     - Passenger 2: Farhan | 24/520002/TK/59002 | farhan.pending@mail.ugm.ac.id (Pending KTM -> ready for Admin verification!)
-- ================================================================

BEGIN;

-- ----------------------------------------------------------------
-- 1. DECLARED ADMINISTRATORS (password: admin123)
-- ----------------------------------------------------------------
INSERT INTO users (faculty_id, full_name, email, phone_number, password_hash, ktm_number, bio, hobbies, role, verification_status)
VALUES
    ((SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
     'BarengIn Campus Admin', 'admin@ugm.ac.id', '081100000001',
     '$2a$11$RXt1u9e5.e.UVKO6TLcd.urDPySN5QV/XeYyIqi6b1k.JWFLuq8s.',
     'ADMIN/01', 'Campus Mobility & Sustainability Administrator', 'Urban Planning, Cycling', 'admin', 'verified'),
    ((SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
     'Altaf Parves Shua Ilham', 'altaf@mail.ugm.ac.id', '081100000002',
     '$2a$11$RXt1u9e5.e.UVKO6TLcd.urDPySN5QV/XeYyIqi6b1k.JWFLuq8s.',
     '24/536741/TK/59565', 'Software Architect, DTETI FT UGM', 'Distributed Systems, Coffee', 'admin', 'verified'),
    ((SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
     'Calvin', 'calvin@mail.ugm.ac.id', '081100000003',
     '$2a$11$RXt1u9e5.e.UVKO6TLcd.urDPySN5QV/XeYyIqi6b1k.JWFLuq8s.',
     '24/532894/TK/59025', 'Backend & Database Engineer, DTETI FT UGM', 'PostgreSQL, Badminton', 'admin', 'verified'),
    ((SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
     'M Dimas Dwi Ananda', 'dimas@mail.ugm.ac.id', '081100000004',
     '$2a$11$RXt1u9e5.e.UVKO6TLcd.urDPySN5QV/XeYyIqi6b1k.JWFLuq8s.',
     '24/536904/TK/59594', 'Frontend & UI Engineer, DTETI FT UGM', 'UI Design, Photography', 'admin', 'verified'),
    ((SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
     'Diffie Alfierie Iswanto', 'diffie@mail.ugm.ac.id', '081100000005',
     '$2a$11$RXt1u9e5.e.UVKO6TLcd.urDPySN5QV/XeYyIqi6b1k.JWFLuq8s.',
     '24/533049/TK/59056', 'Frontend & QA Engineer, DTETI FT UGM', 'Music, Gaming', 'admin', 'verified')
ON CONFLICT (ktm_number) DO UPDATE SET
    password_hash = EXCLUDED.password_hash,
    verification_status = 'verified',
    bio = EXCLUDED.bio,
    hobbies = EXCLUDED.hobbies;

INSERT INTO admin (user_id, department, admin_level)
SELECT user_id, 'DTETI', 'super_admin'::admin_level_type
FROM users
WHERE ktm_number IN ('ADMIN/01', '24/536741/TK/59565', '24/532894/TK/59025', '24/536904/TK/59594', '24/533049/TK/59056')
ON CONFLICT (user_id) DO NOTHING;

-- ----------------------------------------------------------------
-- 2. DEMO DRIVERS: Satrio (Mazda 6 Red) & Nalen (Toyota Fortuner)
--    (password: demo123)
-- ----------------------------------------------------------------
INSERT INTO users (faculty_id, full_name, email, phone_number, password_hash, ktm_number, bio, hobbies, role, verification_status)
VALUES
    ((SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
     'Satrio', 'satrio.driver@mail.ugm.ac.id', '081210000001',
     '$2a$11$O2eRYTxlh9QXa.sd7IWSdeX6FezqYbU9vmQZilO4MH5qEErdTzeXG',
     '24/510001/TK/58001', 'Daily morning commuter in a red Mazda 6 from Jakal Km 5.5 to DTETI FT.', 'Badminton, Indie Music, Coffee', 'driver', 'verified'),
    ((SELECT faculty_id FROM faculty WHERE name = 'Economics and Business (FEB)' LIMIT 1),
     'Nalen', 'nalen.driver@mail.ugm.ac.id', '081210000002',
     '$2a$11$O2eRYTxlh9QXa.sd7IWSdeX6FezqYbU9vmQZilO4MH5qEErdTzeXG',
     '24/510002/EK/58002', 'Commuting in a Toyota Fortuner from Seturan to FEB every morning.', 'Podcasts, Running, Culinary', 'driver', 'verified')
ON CONFLICT (ktm_number) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    verification_status = 'verified',
    bio = EXCLUDED.bio,
    hobbies = EXCLUDED.hobbies;

INSERT INTO driver (user_id, license_number, rating_avg, total_rides_completed)
VALUES
    ((SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001'), 'AB-510001-UGM', 4.90, 14),
    ((SELECT user_id FROM users WHERE ktm_number = '24/510002/EK/58002'), 'AB-510002-UGM', 4.85, 11)
ON CONFLICT (user_id) DO UPDATE SET
    rating_avg = EXCLUDED.rating_avg,
    total_rides_completed = EXCLUDED.total_rides_completed;

INSERT INTO vehicle (driver_id, plate_number, model, color, seat_capacity)
VALUES
    ((SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001'), 'AB 1421 MZ', 'Mazda 6', 'Red', 4),
    ((SELECT user_id FROM users WHERE ktm_number = '24/510002/EK/58002'), 'AB 1988 FT', 'Toyota Fortuner', 'Black', 6)
ON CONFLICT (plate_number) DO UPDATE SET
    model = EXCLUDED.model,
    color = EXCLUDED.color,
    seat_capacity = EXCLUDED.seat_capacity;

-- ----------------------------------------------------------------
-- 3. DEMO PASSENGERS: Vino (Engineering, Pogung Dalangan) & Farhan (KTM Pending)
--    (password: demo123)
-- ----------------------------------------------------------------
INSERT INTO users (faculty_id, full_name, email, phone_number, password_hash, ktm_number, bio, hobbies, role, verification_status)
VALUES
    ((SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
     'Vino', 'vino.pax@mail.ugm.ac.id', '081220000001',
     '$2a$11$O2eRYTxlh9QXa.sd7IWSdeX6FezqYbU9vmQZilO4MH5qEErdTzeXG',
     '24/520001/TK/59001', 'Engineering student commuting daily from Pogung Dalangan.', 'UI/UX, Coffee, Reading', 'passenger', 'verified'),
    ((SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
     'Farhan', 'farhan.pending@mail.ugm.ac.id', '081220000002',
     '$2a$11$O2eRYTxlh9QXa.sd7IWSdeX6FezqYbU9vmQZilO4MH5qEErdTzeXG',
     '24/520002/TK/59002', 'Engineering student waiting for KTM verification.', 'Robotics, Cycling', 'passenger', 'pending')
ON CONFLICT (ktm_number) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    verification_status = EXCLUDED.verification_status,
    bio = EXCLUDED.bio,
    hobbies = EXCLUDED.hobbies;

INSERT INTO passenger (user_id, home_address, default_pickup_lat, default_pickup_lng)
VALUES
    ((SELECT user_id FROM users WHERE ktm_number = '24/520001/TK/59001'), 'Pogung Dalangan', -7.7611, 110.3738),
    ((SELECT user_id FROM users WHERE ktm_number = '24/520002/TK/59002'), 'Sendowo', -7.7712, 110.3715)
ON CONFLICT (user_id) DO UPDATE SET
    home_address = EXCLUDED.home_address;

-- Seed KTM verification queue rows
DELETE FROM ktm_verification
WHERE user_id IN (
    SELECT user_id FROM users WHERE ktm_number IN (
        '24/510001/TK/58001', '24/510002/EK/58002',
        '24/520001/TK/59001', '24/520002/TK/59002'
    )
);

INSERT INTO ktm_verification (user_id, ktm_image_url, status, verified_by_admin_id, submitted_at, verified_at)
VALUES
    ((SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001'), 'ktm_satrio_ft.jpg', 'approved',
     (SELECT user_id FROM users WHERE ktm_number = 'ADMIN/01'), now() - interval '5 days', now() - interval '4 days'),
    ((SELECT user_id FROM users WHERE ktm_number = '24/510002/EK/58002'), 'ktm_nalen_feb.jpg', 'approved',
     (SELECT user_id FROM users WHERE ktm_number = 'ADMIN/01'), now() - interval '4 days', now() - interval '3 days'),
    ((SELECT user_id FROM users WHERE ktm_number = '24/520001/TK/59001'), 'ktm_vino_ft.png', 'approved',
     (SELECT user_id FROM users WHERE ktm_number = 'ADMIN/01'), now() - interval '3 days', now() - interval '2 days'),
    ((SELECT user_id FROM users WHERE ktm_number = '24/520002/TK/59002'), 'ktm_farhan_pending.png', 'pending',
     NULL, now() - interval '30 minutes', NULL);

-- ----------------------------------------------------------------
-- 4. SCHEDULED RIDES, INCOMING REQUESTS & CO2 LOGS
-- ----------------------------------------------------------------
DELETE FROM co2_log WHERE ride_id IN (
    SELECT r.ride_id FROM ride r JOIN users u ON u.user_id = r.driver_id
    WHERE u.ktm_number IN ('24/510001/TK/58001', '24/510002/EK/58002')
);
DELETE FROM ride_request WHERE ride_id IN (
    SELECT r.ride_id FROM ride r JOIN users u ON u.user_id = r.driver_id
    WHERE u.ktm_number IN ('24/510001/TK/58001', '24/510002/EK/58002')
);
DELETE FROM ride WHERE driver_id IN (
    SELECT user_id FROM users WHERE ktm_number IN ('24/510001/TK/58001', '24/510002/EK/58002')
);

-- Ride 1: Satrio (Mazda 6 Red) -> Engineering (FT), 07:30, 3 seats, Rp 5.000
INSERT INTO ride (driver_id, vehicle_id, destination_faculty_id, origin_address, origin_lat, origin_lng, departure_time, available_seats, cost_per_seat, status)
VALUES (
    (SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001'),
    (SELECT vehicle_id FROM vehicle WHERE plate_number = 'AB 1421 MZ'),
    (SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
    'Jakal Km 5.5', -7.7523, 110.3812,
    CURRENT_DATE + TIME '07:30:00', 3, 5000.00, 'scheduled'
);

-- Ride 2: Satrio (Mazda 6 Red) -> Engineering (FT), 08:45, 2 seats, Rp 5.000
INSERT INTO ride (driver_id, vehicle_id, destination_faculty_id, origin_address, origin_lat, origin_lng, departure_time, available_seats, cost_per_seat, status)
VALUES (
    (SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001'),
    (SELECT vehicle_id FROM vehicle WHERE plate_number = 'AB 1421 MZ'),
    (SELECT faculty_id FROM faculty WHERE name = 'Engineering (FT)' LIMIT 1),
    'Pogung Dalangan', -7.7611, 110.3738,
    CURRENT_DATE + TIME '08:45:00', 2, 5000.00, 'scheduled'
);

-- Ride 3: Nalen (Toyota Fortuner) -> Seturan to Economics and Business (FEB), 07:45, 4 seats, Rp 6.000
INSERT INTO ride (driver_id, vehicle_id, destination_faculty_id, origin_address, origin_lat, origin_lng, departure_time, available_seats, cost_per_seat, status)
VALUES (
    (SELECT user_id FROM users WHERE ktm_number = '24/510002/EK/58002'),
    (SELECT vehicle_id FROM vehicle WHERE plate_number = 'AB 1988 FT'),
    (SELECT faculty_id FROM faculty WHERE name = 'Economics and Business (FEB)' LIMIT 1),
    'Seturan', -7.7689, 110.4097,
    CURRENT_DATE + TIME '07:45:00', 4, 6000.00, 'scheduled'
);

-- Seed requests on Satrio's rides so Driver screen immediately shows pending & accepted requests
INSERT INTO ride_request (ride_id, passenger_id, pickup_address, pickup_lat, pickup_lng, status, requested_at)
VALUES
    ((SELECT ride_id FROM ride WHERE origin_address = 'Jakal Km 5.5' AND driver_id = (SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001') LIMIT 1),
     (SELECT user_id FROM users WHERE ktm_number = '24/520001/TK/59001'),
     'Pogung Dalangan', -7.7611, 110.3738, 'pending', now() - interval '15 minutes'),
    ((SELECT ride_id FROM ride WHERE origin_address = 'Jakal Km 5.5' AND driver_id = (SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001') LIMIT 1),
     (SELECT user_id FROM users WHERE ktm_number = '24/520002/TK/59002'),
     'Sendowo', -7.7712, 110.3715, 'pending', now() - interval '10 minutes'),
    ((SELECT ride_id FROM ride WHERE origin_address = 'Pogung Dalangan' AND driver_id = (SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001') LIMIT 1),
     (SELECT user_id FROM users WHERE ktm_number = '24/520001/TK/59001'),
     'Pogung Dalangan', -7.7611, 110.3738, 'accepted', now() - interval '1 day');

-- Seed CO2 savings logs for sustainability analytics
INSERT INTO co2_log (ride_id, distance_km, co2_saved_kg)
SELECT r.ride_id, 5.40, 2.40
FROM ride r
WHERE r.origin_address = 'Jakal Km 5.5'
  AND r.driver_id = (SELECT user_id FROM users WHERE ktm_number = '24/510001/TK/58001')
LIMIT 1
ON CONFLICT (ride_id) DO NOTHING;

INSERT INTO co2_log (ride_id, distance_km, co2_saved_kg)
SELECT r.ride_id, 6.80, 3.60
FROM ride r
WHERE r.origin_address = 'Seturan'
  AND r.driver_id = (SELECT user_id FROM users WHERE ktm_number = '24/510002/EK/58002')
LIMIT 1
ON CONFLICT (ride_id) DO NOTHING;

COMMIT;
