# BarengIn 3-Minute Demo Video Runbook & Online Database Guide

This guide covers:
1. Deploying the online PostgreSQL database on **Neon (`ap-southeast-1` Singapore)** in 2 minutes.
2. Resetting and seeding deterministic demo data before every video recording take.
3. Packaging the final Windows `.exe` submission bundle connected to the live Neon database.
4. A second-by-second **3-Minute Demo Video Script & Screen Choreography** that obeys every grading rule:
   - **Maximum duration:** 3 minutes (`<= 180 seconds`).
   - **Application demo only:** No title slide, no PPT intro, start directly inside the live application and database.
   - **Database is NOT a local database:** Explicitly shows the live Neon cloud host (`*.ap-southeast-1.aws.neon.tech`) at the start and during database updates.

---

## Part 1: Deploy the Online PostgreSQL Database on Neon (2 Minutes)

### Why Neon (`ap-southeast-1` Singapore)?
- **Zero local dependency:** Hosted on AWS Singapore (`ap-southeast-1`), giving low latency (~25–40 ms) from Indonesia.
- **Never pauses for 7 days:** Unlike Supabase Free Tier (which pauses after 7 days and breaks grading if lecturers test a week later), Neon scales to zero and wakes up automatically in `< 500 ms` when the lecturer launches `BarengIn.Desktop.exe`.
- **Standard Npgsql TLS connection:** Works out of the box with `BarengIn.Infrastructure` and `AppDb.cs` (which now uses a 15-second cloud TLS timeout).

### Step-by-Step Neon Setup
1. Open [https://neon.tech](https://neon.tech) and sign in with GitHub or Google (no credit card required).
2. Click **New Project**:
   - **Project name:** `barengin-ugm`
   - **Postgres version:** `16` (or `17`)
   - **Region:** **AWS Asia Pacific (Singapore) (`ap-southeast-1`)**
3. Once created, Neon displays your connection URI:
   ```text
   postgresql://neondb_owner:AbCdEf123456@ep-cool-forest-a1b2c3d4.ap-southeast-1.aws.neon.tech/neondb?sslmode=require
   ```
4. Convert that URI into the **Npgsql key-value format** used by `.NET / Npgsql` and save it into an untracked `.env.neon` file in the repository root (`.env.*` is gitignored):
   ```bash
   cat << 'EOF' > .env.neon
   NEON_URI="postgresql://neondb_owner:YOUR_PASSWORD@ep-xxxx.ap-southeast-1.aws.neon.tech/neondb?sslmode=require"
   BARENGIN_DB="Host=ep-xxxx.ap-southeast-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
   EOF
   ```
5. Load the 15-table schema (`schema.sql`) and demo accounts/rides (`seed-demo.sql`) into Neon:
   ```bash
   source .env.neon
   psql "$NEON_URI" -f docs/database/schema.sql -f docs/database/seed-demo.sql
   ```
   *(If you do not have `psql` installed locally on macOS, you can paste `docs/database/schema.sql` followed by `docs/database/seed-demo.sql` directly into the **Neon Web SQL Editor** in your browser and click **Run**!)*

---

## Part 2: Build the Final Submission `.zip` with Live Cloud DB

When you run `./scripts/package-windows.sh`, it checks for `.env.neon` (or `BARENGIN_SUBMISSION_DB`) and automatically injects your live Neon connection string into the `appsettings.json` inside `dist/BarengIn-MVP-win-x64.zip`, while keeping your Git repository's `src/BarengIn.Desktop/appsettings.json` clean:

```bash
source .env.neon
export BARENGIN_SUBMISSION_DB="$BARENGIN_DB"
./scripts/package-windows.sh
```

This produces `dist/BarengIn-MVP-win-x64.zip` containing:
- `BarengIn.Desktop.exe` (Self-contained Windows x64 binary)
- `appsettings.json` (Pre-configured with your live Neon Singapore database)
- `schema.sql` & `seed-demo.sql`
- `README-RUN.txt`

For the final Microsoft Teams submission, place into a single archive:
1. Your group's **Final PPT** (`.pptx` / `.pdf`)
2. Your **3-Minute Demo Video** (`.mp4`)
3. The Windows application files (`BarengIn.Desktop.exe` + `appsettings.json` or `BarengIn-MVP-win-x64.zip`)

---

## Part 3: Second-by-Second 3-Minute Demo Video Script (170 Seconds Total)

> [!IMPORTANT]
> **Rubric Rules Reminder:**
> 1. **Max 3 minutes** (target **2:45 – 2:50** so you have a 10-second safety buffer).
> 2. **Application demo only (no introduction needed)** — Do **NOT** start with a slide deck or a 20-second "Hello my name is..." speech. Start frame 1 with the application window and the Neon Cloud SQL console/connection string visible on screen.
> 3. **Database is NOT a local database** — Call out and show the `*.ap-southeast-1.aws.neon.tech` hostname on screen in the first 10 seconds.

### Pre-Recording Reset (Run before every take)
```bash
source .env.neon
psql "$NEON_URI" -f docs/database/schema.sql -f docs/database/seed-demo.sql
BARENGIN_DB="$BARENGIN_DB" dotnet run --project src/BarengIn.Desktop
```

---

### `[0:00 - 0:15]` Scene 1 — Proof of Non-Local Cloud Database (Neon Singapore)
- **On Screen:** Split screen:
  - Left: `appsettings.json` (or Neon Web Console `Tables` view) clearly showing `Host=ep-....ap-southeast-1.aws.neon.tech` (AWS Singapore region).
  - Right: `BarengIn.Desktop` on the **Login** screen.
- **Voiceover (15s):**
  > *"BarengIn is running live against our online Neon PostgreSQL database hosted in AWS Singapore (`ap-southeast-1`), not a local database. You can see the cloud endpoint in `appsettings.json` and the 15 seeded tables in the Neon console."*

---

### `[0:15 - 0:45]` Scene 2 — Student Registration & Pending KTM Badge (30s)
- **Action:**
  1. Click **No account yet? Create one** to open `RegisterWindow`.
  2. Briefly show the registration form fields (Full Name, NIM, UGM Email, Phone, 35 UGM Faculties dropdown, Role, Password, and **Choose file...** for KTM upload).
  3. Switch back to `LoginWindow` and log in as pre-seeded unverified student **Farhan**:
     - **Role:** `Passenger`
     - **Email / NIM:** `farhan.pending@mail.ugm.ac.id` (or `24/520002/TK/59002`)
     - **Password:** `demo123`
  4. Click **Profile** in the top-right corner to open `ProfileWindow`.
  5. Point out the read-only Student Identity Card and the amber **`KTM pending`** badge. Update **Hobbies** to `"Robotics, Cycling, Coffee"` and click **Save**.
- **Voiceover (30s):**
  > *"New students register with their UGM NIM, destination faculty, and KTM image upload. Logging in as Farhan, his profile shows a `KTM pending` status until verified by an administrator. Updating his hobbies and bio persists directly to Neon PostgreSQL."*

---

### `[0:45 - 1:25]` Scene 3 — Driver Route Preview (OpenStreetMap OSRM) & Trip Publishing (40s)
- **Action:**
  1. Click **Back** -> **Logout**.
  2. Log in as verified Driver **Satrio** (Red Mazda 6):
     - **Role:** `Driver`
     - **Email / NIM:** `satrio.driver@mail.ugm.ac.id` (or `24/510001/TK/58001`)
     - **Password:** `demo123`
  3. On `DriverWindow`, type `Pogung` into **Pickup area** to trigger the live Yogyakarta place autocomplete dropdown and select **`Pogung Lor`**.
  4. Select destination **`Engineering (FT)`**, departure **`07:30`**, seats **`3`**, cost **`5000`**.
  5. Click **Check Route** — point to the route banner showing live distance, ETA, and $\text{CO}_2$ saved from OpenStreetMap Nominatim + OSRM.
  6. Click **Post Trip** — the new trip appears in **Your posted trips** and persists to Neon PostgreSQL. (Also note Driver 2 **Nalen** has a Toyota Fortuner scheduled from `Seturan` to `Economics and Business (FEB)`.)
- **Voiceover (40s):**
  > *"Next, we log in as a verified Driver, Satrio, who drives a red Mazda 6. Typing `Pogung` triggers Yogyakarta place autocomplete. Clicking Check Route queries OpenStreetMap Nominatim and the OSRM driving route API to calculate road distance, travel time, and estimated CO2 saved. Clicking Post Trip saves the scheduled ride to our cloud PostgreSQL database."*

---

### `[1:25 - 1:55]` Scene 4 — Passenger Search, Social Profile Match & Ride Request (30s)
- **Action:**
  1. Click **Logout** and log in as verified Passenger **Vino**:
     - **Role:** `Passenger`
     - **Email / NIM:** `vino.pax@mail.ugm.ac.id` (or `24/520001/TK/59001`)
     - **Password:** `demo123`
  2. On `HomeWindow`, type `Pogung` in **Pickup area** and select **`Pogung Dalangan`**, destination **`Engineering (FT)`**, and click **Find Rides**.
  3. Point out the matching scheduled rides from the cloud database, including Satrio's social profile card (NIM, hobbies, and bio) and the live OSRM route banner.
  4. Select Satrio's ride and click **Request Ride**. Notice the **Estimated CO2 saved** counter.
- **Voiceover (30s):**
  > *"Logging in as Passenger Vino from Engineering commuting from Pogung Dalangan, we search for rides to Engineering FT. BarengIn returns matching scheduled drivers from the cloud database along with their hobbies and bio so students can commute with like-minded peers, plus the live OSRM route estimate. Selecting a ride and clicking Request Ride submits a seat request."*

---

### `[1:55 - 2:20]` Scene 5 — Driver Atomic Seat Approval (`SELECT ... FOR UPDATE`) (25s)
- **Action:**
  1. Click **Logout** and log back in as Driver **Satrio** (`satrio.driver@mail.ugm.ac.id` / `demo123`).
  2. Point to **Incoming passenger requests** on the right panel showing Vino's and Farhan's pending requests with their passenger hobbies and bio.
  3. Click **Approve** on the first pending request.
  4. Point to **Your posted trips** on the left: the trip's available seats immediately decrement from `3 seats` to `2 seats` inside a row-locked PostgreSQL transaction, and the request status updates to **`Approved`**.
- **Voiceover (25s):**
  > *"Back on Satrio's Driver Dashboard, incoming passenger requests show the passenger's faculty, hobbies, and bio. Clicking Approve executes a row-locked PostgreSQL transaction that marks the request accepted and atomically decrements available seats from 3 to 2, preventing overbooking."*

---

### `[2:20 - 2:50]` Scene 6 — Admin Dashboard: KTM Verification, Suspension & $\text{CO}_2$ Report (30s)
- **Action:**
  1. Click **Logout** and log in as Campus Administrator:
     - **Role:** `Admin`
     - **Email:** `admin@ugm.ac.id`
     - **Password:** `admin123`
  2. On `AdminWindow`, point to the 4 KPI cards at the top (**Total CO2 Saved**, **Shared Rides**, **Pending KTMs**, **Verified Users**).
  3. In the **KTM Verification & User Moderation** queue, find **Farhan** (`24/520002/TK/59002`, status `Pending`) and click **Verify KTM**. His status immediately updates to **`Verified`** in Neon PostgreSQL and the Pending KTMs counter drops.
  4. Point to the **Top CO2 Savers Leaderboard** on the right and click **Generate Report** to display the 30-day Campus Sustainability Report.
- **Voiceover (30s):**
  > *"Finally, logging in with `admin@ugm.ac.id` opens the Admin Dashboard. Here we monitor total campus CO2 saved and shared rides, verify Farhan's pending KTM upload in real time, review the top CO2 savers leaderboard, and generate a 30-day campus sustainability report—all persisted live on Neon Cloud PostgreSQL."*

