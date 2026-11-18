# Module 9 — OOP Pillars, Ride Flow & Access Modifiers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete Module 9 (`WORKSHEET HOMEWORK 9.docx`) by implementing the end-to-end Ride Publishing, Searching, Requesting, and Approval/Rejection flow using OOP Pillars (Inheritance, Encapsulation, Polymorphism) and explicit Access Modifiers (`public`, `protected`, `private`), backed by PostgreSQL (`RideRepository`) with an automatic offline fallback so all headless tests pass and screenshots/worksheet evidence are generated.

**Architecture:**
- **Domain (`src/BarengIn.Domain`)**: `User` -> `Passenger` -> `Driver` inheritance hierarchy and encapsulated aggregates (`Trip`, `RideRequest`, `ClassSchedule`, `Money`). `User.CanPublishTrip()` and `User.GetRoleName()` demonstrate polymorphism (`Passenger` returns `false`/`"Passenger"`, `Driver` overrides to `true`/`"Driver"`), while `Driver.ApproveRequest(Guid)` and `Driver.RejectRequest(Guid, string)` encapsulate state transitions across `private` backing collections (`publishedTrips`, `rideRequests`, `passengerIds`) and `protected` base fields (`userId`, `verificationStatus`).
- **Infrastructure (`src/BarengIn.Infrastructure`)**: Add `RideRepository` alongside `UserRepository` to persist and query rides (`ride` table) and ride requests (`ride_request` table) in PostgreSQL, plus seed helper for `faculty` and `driver`/`passenger`/`vehicle` rows when missing.
- **Desktop (`src/BarengIn.Desktop`)**: `AppSession` holds the active `User` domain instance (`CurrentUser`) and a live `Driver` aggregate (`ActiveDriver`) wired to `DriverWindow` and `HomeWindow`. `AppDb` checks whether PostgreSQL is reachable (`BARENGIN_DB` env var or local DB) and falls back cleanly to in-memory Domain execution when offline so `BarengIn.Desktop.Tests` and screenshot generation work anywhere.

**Tech Stack:** C#, .NET 9, Avalonia 12, Npgsql 10, BCrypt.Net-Next, xUnit + Avalonia.Headless, Python `python-docx` / `zipfile` for populating `docs/modules/module-9/WORKSHEET HOMEWORK 9.docx`.

**Spec:** Agreed in chat (Option A — Ride Publishing, Searching & Request/Approval flow + Module 9 Worksheet with screenshots).

## Global Constraints

- Keep `BarengIn.Domain` 100% dependency-free (no Npgsql, no EF Core, no UI references).
- Preserve all existing UI control names (`tbUsername`, `tbPassword`, `cbRole`, `btnLogin`, `btnRegister`, `tbOrigin`, `cbFaculty`, `dtpTime`, `btnFind`, `lvRides`, `btnRequest`, `lblCo2`, `nudSeats`, `nudCost`, `btnPost`, `lvTrips`, `lvRequests`, `btnProfile`, `btnLogout`) so existing tests in `tests/BarengIn.Desktop.Tests` continue to pass.
- Clearly annotate OOP Pillars (`Inheritance`, `Encapsulation`, `Polymorphism`) and Access Modifiers (`public`, `protected`, `private`) in XML doc/code comments on the methods showcased in the Module 9 worksheet screenshots.
- Store Module 9 screenshots and completed `.docx` worksheet under `docs/modules/module-9/`.

## Review Focus

- `LoginWindow` and `RegisterWindow` in headless tests (`NavigationTests`): when PostgreSQL is not running or `BARENGIN_DB` is unset, `AppDb` must not block navigation with a DB exception popup in headless tests; it should use PostgreSQL when configured/reachable and fall back to constructing the polymorphic `User` (`Driver` or `Passenger`) in memory when offline.
- `Driver.CanPublishTrip()` checks `IsVerified() && vehicles.Count > 0`: a newly signed-in `Driver` in `AppSession` must have a verified status and default `Vehicle` registered so `CanPublishTrip()` returns `true` (whereas `Passenger.CanPublishTrip()` returns `false`).
- Approving a `RideRequest` on `DriverWindow` must call `Driver.ApproveRequest(requestId)` -> `RideRequest.Approve()` and `Trip.AddPassenger(passengerId)`, decrementing `Trip.AvailableSeats` (`private int availableSeats`) and updating the `TripRow.Seats` shown in the UI.
- Rejecting an already-approved or already-rejected request must return `false` (`RideRequest.status != RequestStatus.Pending`) and leave seat counts unchanged.
- Posting a trip with empty pickup area on `DriverWindow` must still be rejected before calling Domain/DB.

---

### Task 1: Domain Enhancements for Ride Flow & Access Modifier Showcase

**Files:**
- Modify: `src/BarengIn.Domain/User.cs`
- Modify: `src/BarengIn.Domain/Driver.cs`
- Modify: `src/BarengIn.Domain/Trip.cs`
- Modify: `src/BarengIn.Domain/RideRequest.cs`
- Test: `tests/BarengIn.Desktop.Tests/LogicTests.cs`

**Interfaces:**
- Produces:
  - `User.UserId` (`public Guid UserId => userId;` reading `protected Guid userId;`)
  - `User.MarkVerified()` (`public void MarkVerified()` setting `protected VerificationStatus verificationStatus = VerificationStatus.Verified;`)
  - `Trip.AvailableSeats` (`public int AvailableSeats => availableSeats;` reading `private int availableSeats;`)
  - `Trip.OriginAddress` (`public string OriginAddress => originAddress;`)
  - `Trip.Status` (`public TripStatus Status => status;`)
  - `RideRequest.Status` (`public RequestStatus Status => status;`)
  - `RideRequest.PickupAddress` (`public string PickupAddress => pickupAddress;`)
  - `Driver.PublishNewTrip(Guid vehicleId, string originAddress, GeoPoint originLocation, Guid destinationFacultyId, DateTime departureTime, int seats, decimal costPerSeatIdr) -> Trip` (checks polymorphic `CanPublishTrip()`, constructs `Trip`, records it in `private readonly List<Trip> publishedTrips`, and returns it)

- [ ] **Step 1: Write failing unit tests in `tests/BarengIn.Desktop.Tests/LogicTests.cs` for polymorphic `CanPublishTrip`, `Driver.PublishNewTrip`, and `Driver.ApproveRequest` / `RejectRequest` seat encapsulation**

```csharp
[Fact]
public void Polymorphism_CanPublishTrip_FalseForPassenger_TrueForVerifiedDriverWithVehicle()
{
    User passenger = new Passenger("24/536741", "Altaf", "a@ugm.ac.id", "hash", "081", Guid.Empty, "Jakal", new GeoPoint(-7.7, 110.3));
    var driver = new Driver("24/532894", "Calvin", "c@ugm.ac.id", "hash", "082", Guid.Empty, "Seturan", new GeoPoint(-7.7, 110.4), "AB-1234-XX");
    driver.MarkVerified();
    var vehicle = new Vehicle(driver.UserId, "AB 1234 XY", "Honda", "Jazz", "White", VehicleType.Car, 4);
    driver.AddVehicle(vehicle);

    User polymorphicDriver = driver;
    Assert.False(passenger.CanPublishTrip());
    Assert.True(polymorphicDriver.CanPublishTrip());
}

[Fact]
public void Encapsulation_DriverApproveRequest_DecrementsTripAvailableSeats()
{
    var driver = new Driver("24/532894", "Calvin", "c@ugm.ac.id", "hash", "082", Guid.Empty, "Seturan", new GeoPoint(-7.7, 110.4), "AB-1234-XX");
    driver.MarkVerified();
    var vehicle = new Vehicle(driver.UserId, "AB 1234 XY", "Honda", "Jazz", "White", VehicleType.Car, 4);
    driver.AddVehicle(vehicle);

    var trip = driver.PublishNewTrip(vehicle.VehicleId, "Seturan", new GeoPoint(-7.7, 110.4), Guid.Empty, DateTime.Today.AddHours(7.5), 3, 5000m);
    var req = new RideRequest(trip.TripId, Guid.NewGuid(), "Jakal Km 5", new GeoPoint(-7.75, 110.38), "Seat please");
    trip.ReceiveRequest(req);

    Assert.Equal(3, trip.AvailableSeats);
    Assert.True(driver.ApproveRequest(req.RequestId));
    Assert.Equal(2, trip.AvailableSeats);
    Assert.Equal(RequestStatus.Approved, req.Status);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarengIn.Desktop.Tests --filter "Polymorphism_CanPublishTrip|Encapsulation_DriverApproveRequest"`
Expected: FAIL with compilation errors (`UserId`, `MarkVerified`, `PublishNewTrip`, `AvailableSeats` not defined).

- [ ] **Step 3: Implement the properties and `Driver.PublishNewTrip` in `src/BarengIn.Domain/`**

Update `User.cs`, `Driver.cs`, `Trip.cs`, `RideRequest.cs`, and `Vehicle.cs` (`public Guid VehicleId => vehicleId;`, `Trip.SetCostPerSeat(Money cost)`). Keep fields `protected` on `User` and `private` on `Trip`, `Driver`, `RideRequest`, and `Vehicle` to showcase access modifiers clearly.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/BarengIn.Desktop.Tests --filter "Polymorphism_CanPublishTrip|Encapsulation_DriverApproveRequest"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/BarengIn.Domain tests/BarengIn.Desktop.Tests/LogicTests.cs
git commit -m "feat(domain): add PublishNewTrip and read-only accessors for ride flow"
```

---

### Task 2: PostgreSQL `RideRepository` & Resilient `AppDb` Connection

**Files:**
- Create: `src/BarengIn.Infrastructure/Persistence/RideRepository.cs`
- Modify: `src/BarengIn.Infrastructure/Persistence/UserRepository.cs`
- Delete: `src/BarengIn.Infrastructure/Class1.cs`
- Modify: `src/BarengIn.Desktop/AppDb.cs`
- Test: `tests/BarengIn.Desktop.Tests/NavigationTests.cs`

**Interfaces:**
- Produces:
  - `RideRepository.EnsureSeedData()` (idempotently seeds `faculty` rows from `Faculties.All` and ensures `driver`/`passenger`/`vehicle` rows exist for registered users)
  - `RideRepository.InsertRide(string driverNim, string originAddress, string facultyName, DateTime departureTime, int seats, decimal costPerSeat) -> int`
  - `RideRepository.FindRides(string facultyName) -> List<(int RideId, string DriverName, string Faculty, string Departs, int Seats, decimal Cost)>`
  - `RideRepository.InsertRequest(int rideId, string passengerNim, string pickupAddress) -> int`
  - `RideRepository.UpdateRequestStatus(int requestId, string status) -> bool`
  - `AppDb.IsAvailable -> bool` (reads `BARENGIN_DB` env var or default localhost connection with a short timeout; returns `false` when PostgreSQL isn't running so UI falls back to Domain in-memory session without crashing tests)

- [ ] **Step 1: Run existing `NavigationTests` to observe failure caused by hardcoded `PASSWORDHERE` DB connection**

Run: `dotnet test tests/BarengIn.Desktop.Tests --filter NavigationTests`
Expected: FAIL (`Login_AsPassenger_OpensHome`, `Login_AsDriver_OpensDriverScreen`, etc. fail because `AppDb.Users.Authenticate` throws a connection error and stays on `LoginWindow`).

- [ ] **Step 2: Implement `RideRepository.cs`, update `UserRepository.cs` to also insert into `passenger`/`driver` role tables, and update `AppDb.cs` to read `BARENGIN_DB` and provide `AppDb.TryAuthenticate` / `AppDb.TryRegister` / `AppDb.Rides`**

In `AppDb.cs`:
- Read `Environment.GetEnvironmentVariable("BARENGIN_DB") ?? DefaultConnectionString`.
- Provide `AuthenticateUser(string nim, string password, string selectedRole) -> User?` which queries `Users.Authenticate(nim, password)` when `IsDatabaseConfigured()` is true, and otherwise constructs the polymorphic `Driver` or `Passenger` domain object directly so both live PostgreSQL and offline headless tests go through the exact same `User` polymorphic hierarchy.

- [ ] **Step 3: Run all `NavigationTests` to verify they pass**

Run: `dotnet test tests/BarengIn.Desktop.Tests --filter NavigationTests`
Expected: PASS (all 7 navigation tests pass).

- [ ] **Step 4: Commit**

```bash
git add src/BarengIn.Infrastructure src/BarengIn.Desktop/AppDb.cs
git commit -m "feat(infra): add RideRepository and resilient AppDb fallback"
```

---

### Task 3: Wire `AppSession`, `DriverWindow`, and `HomeWindow` to Domain OOP Methods

**Files:**
- Modify: `src/BarengIn.Desktop/Models/AppSession.cs`
- Modify: `src/BarengIn.Desktop/Models/RequestItem.cs`
- Modify: `src/BarengIn.Desktop/Models/TripRow.cs`
- Modify: `src/BarengIn.Desktop/Logic/RideLogic.cs`
- Modify: `src/BarengIn.Desktop/Views/LoginWindow.axaml.cs`
- Modify: `src/BarengIn.Desktop/Views/DriverWindow.axaml.cs`
- Modify: `src/BarengIn.Desktop/Views/HomeWindow.axaml.cs`
- Modify: `src/BarengIn.Desktop/Views/ProfileWindow.axaml.cs`
- Test: `tests/BarengIn.Desktop.Tests/ScreenBehaviorTests.cs`

**Interfaces:**
- Consumes: `Driver.PublishNewTrip`, `Driver.ApproveRequest`, `Driver.RejectRequest`, `User.CanPublishTrip`, `User.GetRoleName`, `User.UpdateProfile`, `Trip.AvailableSeats`
- Produces:
  - `AppSession.CurrentUser` (`public static User? CurrentUser { get; private set; }`)
  - `AppSession.SignIn(User user)` and `AppSession.SignIn(string nim, string role)` (creates the polymorphic `Driver` or `Passenger` instance, sets `CurrentUser`, and initializes sample `Trip` + 3 `RideRequest` domain objects on `Driver` for `DriverWindow`)
  - `DriverWindow.btnPost_Click`: verifies `CurrentUser.CanPublishTrip()` (polymorphism), calls `driver.PublishNewTrip(...)` (encapsulation), persists to `AppDb.Rides` if DB active, and adds `TripRow` to `lvTrips`.
  - `DriverWindow.btnApprove_Click` / `btnReject_Click`: calls `driver.ApproveRequest(item.RequestId)` / `driver.RejectRequest(item.RequestId, "Declined by driver")` on the domain `Driver` object, updates `item.Status` and refreshes `lvTrips` seat counts.
  - `ProfileWindow.btnSave_Click`: calls `AppSession.CurrentUser?.UpdateProfile(AppSession.Name, AppSession.Nim, AppSession.Bio)` (encapsulated mutation of `protected` fields on `User`).

- [ ] **Step 1: Write failing test in `tests/BarengIn.Desktop.Tests/ScreenBehaviorTests.cs` verifying Domain state changes on `DriverWindow` and `ProfileWindow`**

```csharp
[AvaloniaFact]
public void Driver_PostAndApprove_UpdatesDomainDriverAndTrip()
{
    var driverWindow = NewDriver();
    var driver = Assert.IsType<BarengIn.Domain.Driver>(AppSession.CurrentUser);
    Assert.True(driver.CanPublishTrip());

    driverWindow.Find<TextBox>("tbOrigin").Text = "Seturan";
    driverWindow.Click("btnPost");
    Assert.Contains(driver.PublishedTrips, t => t.OriginAddress == "Seturan");

    driverWindow.Layout();
    var firstReq = driverWindow.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().First();
    driverWindow.FindButton("Approve").Press();
    Assert.Equal("Approved", firstReq.Status);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BarengIn.Desktop.Tests --filter Driver_PostAndApprove_UpdatesDomainDriverAndTrip`
Expected: FAIL (`AppSession.CurrentUser` not defined).

- [ ] **Step 3: Implement Domain wiring in `AppSession.cs`, `RequestItem.cs`, `LoginWindow.axaml.cs`, `DriverWindow.axaml.cs`, `HomeWindow.axaml.cs`, and `ProfileWindow.axaml.cs`**

- [ ] **Step 4: Run full test suite to verify all tests pass**

Run: `dotnet test tests/BarengIn.Desktop.Tests`
Expected: PASS (all `LogicTests`, `NavigationTests`, and `ScreenBehaviorTests` pass).

- [ ] **Step 5: Commit**

```bash
git add src/BarengIn.Desktop tests/BarengIn.Desktop.Tests
git commit -m "feat(desktop): wire DriverWindow, HomeWindow and ProfileWindow to Domain OOP methods"
```

---

### Task 4: Generate Module 9 Screenshots & Complete `WORKSHEET HOMEWORK 9.docx`

**Files:**
- Modify: `tests/BarengIn.Desktop.Tests/ScreenshotTests.cs`
- Create: `docs/modules/module-9/screenshots/` (PNGs for code + running UI)
- Create: `docs/modules/module-9/WORKSHEET HOMEWORK 9.docx`

- [ ] **Step 1: Add Module 9 screenshot capture to `ScreenshotTests.cs` and run it**

Run: `BARENGIN_SCREENSHOTS=1 dotnet test tests/BarengIn.Desktop.Tests --filter ScreenshotTests`
Expected: Generates crisp UI screenshots in `docs/modules/module-9/screenshots/` (`01-login-polymorphism.png`, `02-driver-post-approve.png`, `03-passenger-find-request.png`, `04-profile-encapsulation.png`) plus syntax-highlighted code screenshots for Task 1 (Inheritance, Encapsulation, Polymorphism) and Task 2 (Access Modifiers: `public`, `protected`, `private`).

- [ ] **Step 2: Populate `docs/modules/module-9/WORKSHEET HOMEWORK 9.docx` from `/Users/altafparvesh/Downloads/WORKSHEET HOMEWORK 9.docx` with the screenshots and explanations for Questions 1 & 2**

- [ ] **Step 3: Inspect the generated `.docx` / rendered pages to verify formatting and completeness**

- [ ] **Step 4: Commit**

```bash
git add tests/BarengIn.Desktop.Tests/ScreenshotTests.cs docs/modules/module-9
git commit -m "docs(module-9): add OOP pillars and access modifiers worksheet and screenshots"
```

