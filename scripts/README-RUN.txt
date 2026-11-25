BarengIn Desktop (Windows x64 Self-Contained Bundle)
=====================================================

1. Bundle Contents
------------------
Keep all files together in the same folder:
  - BarengIn.Desktop.exe  Self-contained .NET 9 Avalonia desktop application (no .NET runtime install needed)
  - appsettings.json      Optional PostgreSQL connection string configuration
  - schema.sql            PostgreSQL database schema (tables, enums, constraints)
  - README-RUN.txt        This guide

2. Option A: Offline Quick Start (No Database Required)
------------------------------------------------------
1. Extract all files from BarengIn-MVP-win-x64.zip into a folder (do not run directly inside the ZIP).
2. Double-click BarengIn.Desktop.exe.
3. By default, appsettings.json contains "Password=PASSWORDHERE". When left unchanged (or if PostgreSQL is unreachable), BarengIn automatically runs in offline mode so you can explore Login, Register, Passenger Home, Driver Dashboard, and Profile immediately.

3. Option B: Full PostgreSQL Setup
----------------------------------
1. Install PostgreSQL (14+) and ensure the PostgreSQL service is running on localhost:5432.
2. Create the database and load the schema:
     psql -U postgres -c "CREATE DATABASE barengin;"
     psql -U postgres -d barengin -f schema.sql
3. Open appsettings.json in Notepad and replace PASSWORDHERE with your PostgreSQL password:
     {
       "ConnectionStrings": {
         "Default": "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=YOUR_PASSWORD"
       }
     }
   (Alternatively, set the BARENGIN_DB environment variable, which overrides appsettings.json.)
4. Launch BarengIn.Desktop.exe. New registrations, posted trips, ride requests, approvals/rejections, and profile updates will persist to PostgreSQL.

4. Live Route Preview (REST API)
--------------------------------
When clicking "Find Rides" (Passenger) or "Check Route" / "Post Trip" (Driver), BarengIn queries OpenStreetMap Nominatim and OSRM over HTTPS to estimate route distance, travel time, and CO2 saved. If you are offline or the public API times out, BarengIn falls back cleanly to an offline estimate.

5. Windows SmartScreen Tip
--------------------------
Because this academic MVP executable is not code-signed with a commercial certificate, Windows Defender SmartScreen may display "Windows protected your PC".
Click "More info" -> "Run anyway" to launch BarengIn.Desktop.exe.
