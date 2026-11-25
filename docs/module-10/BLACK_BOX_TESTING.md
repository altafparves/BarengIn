# Lab 10.2 — Black Box Testing Report (BarengIn Desktop MVP)

This document records the Lab 10.2 functional **Black Box Testing** for the BarengIn Desktop application across all five UI screens, PostgreSQL persistence repositories, configuration loader, and REST API route service.

---

## 1. Test Environment & Scope

- **Application Under Test:** `BarengIn.Desktop` (.NET 9, Avalonia UI 12.1.4) + `BarengIn.Infrastructure` + `BarengIn.Domain`
- **Verification Environment:**
  - **OS / Runtime:** macOS (`net9.0`), Avalonia Headless UI (`Avalonia.Headless.XUnit` + `Avalonia.Skia`) and xUnit v3 (`3.2.2`)
  - **Database:** Local PostgreSQL instance (`barengin` database loaded with `docs/database/schema.sql`), tested via `AppDb` / `UserRepository` / `RideRepository`
  - **REST API:** OpenStreetMap Nominatim + OSRM tested deterministically via stubbed `HttpMessageHandler` and `AppDb.RouteEstimator` overrides (covering live JSON parsing, `de-DE` invariant formatting, timeouts, HTTP errors, and `"Jakal"` retry)
- **Windows `.exe` Scope Note:** `scripts/package-windows.sh` cross-compiles and bundles `dist/BarengIn-MVP-win-x64.zip` (`BarengIn.Desktop.exe`, `appsettings.json`, `schema.sql`, `README-RUN.txt`) on macOS. Interactive GUI behavior is verified in Avalonia Headless on macOS; native Windows 10/11 Explorer double-click launch and SmartScreen interaction are left for manual confirmation on a Windows host.

---

## 2. Black Box Testing Summary Table (`BB-01` .. `BB-24`)

| ID | Feature / Module | Scenario / Input | Expected Output / Behavior | Observed Output / Behavior | Status |
|---|---|---|---|---|---|
| **BB-01** | Register (`RegisterWindow`) | Valid full name, NIM, faculty, matching 6+ char passwords, and KTM photo attached; click **Register** | Validation returns `null`; user created (or offline fallback succeeds) and redirects to `LoginWindow` | Validation passes (`null`); `Register` succeeds in both DB and offline flows | **Match (v)** |
| **BB-02** | Register (`RegisterWindow`) | Mismatched password & confirm (`secret1` vs `secret2`) | Validation returns `"Passwords do not match."` and keeps user on `RegisterWindow` | Returns `"Passwords do not match."`; `Navigator.Current` remains `RegisterWindow` | **Match (v)** |
| **BB-03** | Register (`RegisterWindow`) | Missing KTM attachment (`ktmFile == ""`) or short password (`< 6` chars) | Returns `"Please attach a photo of your KTM."` / `"Password must be at least 6 characters."` | Exact error messages returned; registration blocked | **Match (v)** |
| **BB-04** | Login (`LoginWindow`) | Empty NIM or empty password; click **Login** | Returns `"Please enter your NIM and password."` and stays on `LoginWindow` | Validation rejects empty/whitespace NIM and empty password; window stays on `LoginWindow` | **Match (v)** |
| **BB-05** | Login (`LoginWindow`) | Valid NIM + password as **Passenger** vs **Driver** | Passenger opens `HomeWindow` (`(Passenger)`); Driver opens `DriverWindow` (`(Driver)`) | Opens `HomeWindow` for role `0` and `DriverWindow` for role `1` with matching welcome banner | **Match (v)** |
| **BB-06** | Navigation (`Navigator`) | Click **Create account**, **Back**, **Profile**, **Back**, **Logout** across screens | Transitions cleanly between `LoginWindow`, `RegisterWindow`, `HomeWindow`, `DriverWindow`, `ProfileWindow` | All round-trip and logout transitions land on expected `Window` types | **Match (v)** |
| **BB-07** | Passenger Search (`HomeWindow`) | Enter pickup `"Jakal Km 5"`, select faculty (`MIPA`), click **Find Rides** | Populates `lvRides` immediately with 3 rides for selected faculty and triggers route preview | `lvRides` displays 3 `MIPA` rows (`07:30`, `07:15`, `07:00`) while `lblRouteInfo` updates asynchronously | **Match (v)** |
| **BB-08** | Passenger Search (`HomeWindow`) | Leave pickup blank (`""`) and click **Find Rides** | Shows `"Please enter your pickup area."`; `lvRides.ItemsSource` remains `null` | `lvRides.ItemsSource` stays `null`; search does not proceed | **Match (v)** |
| **BB-09** | Ride Request (`HomeWindow`) | Click **Request Ride** without selecting a row vs with row selected | Without selection: CO2 stays `0.0 kg`; with selection: CO2 increases by `1.2 kg` | `lblCo2` stays `"Estimated CO2 saved: 0.0 kg"` when unselected; updates to `"1.2 kg"` when selected | **Match (v)** |
| **BB-10** | Driver Post Trip (`DriverWindow`) | Empty pickup vs valid pickup (`"Seturan"`) and click **Post Trip** | Empty pickup is rejected; valid pickup adds `TripRow` (`07:30`, `Rp 5.000`) and updates Domain `PublishedTrips` | Empty click leaves `lvTrips` empty; valid click adds `Seturan` `TripRow` and `Trip` | **Match (v)** |
| **BB-11** | Driver Offline Requests (`DriverWindow`) | Click **Approve** on 1st request and **Reject** on 2nd request in offline mode | 1st status becomes `"Approved"` and decrements `TripRow.Seats` (`3 -> 2`); 2nd becomes `"Rejected"`; 3rd stays `"Pending"` | Statuses update to `"Approved"`, `"Rejected"`, `"Pending"` and visible `Seats` decrements `3 -> 2` | **Match (v)** |
| **BB-12** | Driver Refresh (`DriverWindow`) | Click **Refresh** (`btnRefresh`) in offline mode | Preserves existing 3 offline `DriverRequests` without throwing or clearing UI | All 3 seeded requests remain intact with zero exceptions | **Match (v)** |
| **BB-13** | Profile (`ProfileWindow`) | Select `"Medicine"`, enter hobbies & bio, click **Save** | Updates `AppSession.Faculty`, `Hobbies`, `Bio`, and `CurrentUser.Bio` | All 4 properties reflect updated values immediately | **Match (v)** |
| **BB-14** | Config Loader (`AppDb`) | Priority check: `BARENGIN_DB` env vs `appsettings.json` vs `PASSWORDHERE` template | `BARENGIN_DB` wins over `appsettings.json`; `PASSWORDHERE` keeps `IsDatabaseConfigured == false` and `IsAvailable == false` | Exact priority observed; `appsettings.json` is copied to `AppContext.BaseDirectory` | **Match (v)** |
| **BB-15** | Config Edge Cases (`AppDb`) | Whitespace `BARENGIN_DB`, malformed JSON, missing file, JSON with comments & trailing commas | Whitespace env falls back to file; bad/missing JSON falls back offline cleanly; JSONC parses cleanly | All 4 edge cases pass without exceptions | **Match (v)** |
| **BB-16** | PostgreSQL Seat Decrement (`RideRepository`) | Live DB: 1-seat ride with 2 pending requests; approve 1st, approve 1st again, approve 2nd | 1st approve returns `true` (`seats: 1 -> 0`); double-approve returns `false`; 2nd approve fails (`available_seats == 0`), preventing overbooking | Confirmed against live PostgreSQL: seats decrement atomically once and 2nd request stays `'pending'` | **Match (v)** |
| **BB-17** | PostgreSQL Reject & Bounds (`RideRepository`) | Live DB: reject already-accepted request, reject pending request, call `ApproveRequest(-1)` / `RejectRequest(-1)`, and `MapRequestStatus` normalization | Rejecting accepted returns `false`; rejecting pending returns `true` (`'rejected'`, seats unchanged); `-1` IDs return `false`; `"Approved"` maps to `"accepted"` | All assertions pass in live PostgreSQL and pure unit tests | **Match (v)** |
| **BB-18** | PostgreSQL Profile Persistence (`UserRepository`) | Blank KTM, register -> `UpdateProfile` -> 5-arg & 2-arg `Authenticate` -> clear fields -> missing KTM | Blank KTM returns `false` without DB call; valid update persists `faculty_id`, `hobbies`, `bio` and restores on login; missing KTM returns `false` | All assertions pass against live PostgreSQL | **Match (v)** |
| **BB-19** | Live UI + DB Flow (`DriverWindow` + `ProfileWindow`) | Login as DB driver -> load DB ride/requests -> **Approve** -> **Refresh** -> **Reject** -> save `ProfileWindow` -> re-login | UI loads live DB rows, syncs seat `1 -> 0` and statuses, and persists profile faculty/hobbies/bio across re-login | Full headless UI + live PostgreSQL round-trip passes | **Match (v)** |
| **BB-20** | REST API Parsing & Coordinates (`RouteApiService`) | All 5 `Faculties.All` names + unknown/blank faculty; valid & malformed Nominatim/OSRM JSON | Distinct campus coordinates for all 5 faculties; Bulaksumur fallback `(-7.7702, 110.3775)`; invalid JSON returns `null` | All coordinate and parser assertions pass | **Match (v)** |
| **BB-21** | REST API Live Calculation (`RouteApiService`) | Mocked Nominatim + OSRM under `de-DE` culture (`8500 m`, `1080 s`) | `User-Agent` header set, `Timeout == 4s`, dot-decimal OSRM URL, `8.5 km`, `18 min`, `1.02 kg CO2 saved` | Invariant URLs, headers, timeout, and CO2 calculation verified under `de-DE` culture | **Match (v)** |
| **BB-22** | REST API `"Jakal"` Retry (`RouteApiService`) | Pickup `"Jakal Km 5"` returns `[]` on 1st Nominatim call, valid coords on retry | Retries once with `"Jalan Kaliurang"` before OSRM lookup (`3` total requests, `IsLiveApi == true`) | 3 HTTP requests observed; 2nd request encodes `Jalan%20Kaliurang` and returns live estimate | **Match (v)** |
| **BB-23** | REST API Fallbacks (`RouteApiService`) | Blank pickup, HTTP `429`/`500`/`503`, empty `[]`, `HttpRequestException`, `TaskCanceledException` | Never throws; returns `CreateFallbackEstimate()` (`5.0 km`, `12 min`, `0.6 kg`, `"Offline estimate"`) | All failure modes cleanly return fallback estimate | **Match (v)** |
| **BB-24** | UI Route Preview & Resize (`HomeWindow`, `DriverWindow`, all windows) | Loading state `"Checking route..."` -> `"Route: ..."` / `"Fallback: ..."` + `CanResize` / `MinWidth` / `MinHeight` on all 5 windows | Loading text appears immediately, updates async on completion; all 5 windows have `CanResize == true` and minimum bounds | Verified in headless UI tests across all 5 windows | **Match (v)** |

---

## 3. Detailed Test Case Breakdown

### A. Authentication, Navigation & Offline UI Flows (`BB-01` .. `BB-13`)
- **Preconditions:** `AppSession.Reset()` called before each scenario; `AppDb` in offline mode (`PASSWORDHERE`) unless testing live DB.
- **Test Data:** NIM `"24/536741"`, Name `"Altaf Parves"`, Pickup `"Jakal Km 5"` / `"Seturan"`, Hobbies `"badminton"`, `"chess"`, Bio `"Likes early rides."`, `"Evening commuter"`.
- **Verification Method:** `Validation` unit tests (`xUnit`) and `Avalonia.Headless.XUnit` (`[AvaloniaFact]`) simulating button clicks and inspecting `Navigator.Current`, `ListBox.ItemsSource`, `TextBlock.Text`, `AppSession`, and Domain `Driver`/`Trip` state.

### B. Configuration, Database Persistence & Transactional Integrity (`BB-14` .. `BB-19`)
- **Preconditions:** Local PostgreSQL running on `localhost:5432` with database `barengin` (schema from `docs/database/schema.sql`), or `BARENGIN_TEST_DB` set. Tests automatically call `Assert.Skip` if PostgreSQL is unreachable in offline CI.
- **Test Data:** Isolated unique NIMs (`DRV/<hex>`, `PX1/<hex>`, `PX2/<hex>`, `PRF/<hex>`, `UID/<hex>`), 1-seat ride (`5000m`), temporary JSON config files cleaned up in `finally` blocks.
- **Verification Method:** Live `Npgsql` integration tests in `LogicTests.cs` and end-to-end headless UI + PostgreSQL test in `ScreenBehaviorTests.cs`, verifying `FOR UPDATE` locking, seat decrement, overbooking guard, status mapping, and profile round-trip persistence.

### C. REST API Service, UI Route Preview & Resizing (`BB-20` .. `BB-24`)
- **Preconditions:** Mocked `StubHttpMessageHandler` injected into `RouteApiService(handler)` and `TaskCompletionSource` / lambda injected into `AppDb.RouteEstimator` (restored in `finally`).
- **Test Data:** `"Jakal Km 5"`, `"Seturan"`, `"Engineering (FT)"`, `"Medicine"`, `de-DE` thread culture, HTTP `429`/`500`/`503`, `HttpRequestException`, `TaskCanceledException`.
- **Verification Method:** Unit tests in `RouteApiServiceTests.cs` and Avalonia Headless UI tests in `ScreenBehaviorTests.cs`.

---

## 4. Automated Test Traceability Matrix

| Black Box ID(s) | Test File | Automated Test Method(s) |
|---|---|---|
| **BB-01, BB-02, BB-03, BB-04** | `tests/BarengIn.Desktop.Tests/LogicTests.cs` | `Login_RejectsEmptyInput`, `Login_AcceptsFilledInput`, `Register_RejectsMismatchedPasswords`, `Register_RejectsMissingKtm`, `Register_AcceptsCompleteForm` |
| **BB-02, BB-04, BB-05, BB-06** | `tests/BarengIn.Desktop.Tests/NavigationTests.cs` | `Login_AsPassenger_OpensHome`, `Login_AsDriver_OpensDriverScreen`, `Login_WithEmptyFields_StaysOnLogin`, `Login_CreateAccount_OpensRegister_AndBackReturns`, `Register_WithMismatchedPasswords_StaysOnRegister`, `Home_Profile_ThenBack_ReturnsToPassengerHome`, `Driver_Profile_ThenBack_ReturnsToDriverScreen`, `Logout_ReturnsToLogin_FromBothHomes` |
| **BB-07, BB-08, BB-09** | `tests/BarengIn.Desktop.Tests/ScreenBehaviorTests.cs` & `LogicTests.cs` | `Home_FindRides_ShowsThreeRows`, `Home_FindRides_WithoutPickup_ShowsNothing`, `Home_RequestWithoutSelection_KeepsCo2AtZero`, `Home_RequestWithSelection_AddsCo2`, `Find_ReturnsThreeRidesForSelectedFaculty`, `Departure_SubtractsMinutes`, `Departure_WrapsPastMidnight` |
| **BB-10, BB-11, BB-12** | `tests/BarengIn.Desktop.Tests/ScreenBehaviorTests.cs` & `LogicTests.cs` | `Driver_PostTrip_AddsToList_AndRejectsEmptyPickup`, `Driver_ApproveAndReject_ChangeRequestStatus`, `Driver_PostAndApprove_UpdatesDomainDriverAndTrip`, `Driver_RefreshButton_InOfflineMode_PreservesRequestsWithoutCrashing`, `Polymorphism_CanPublishTrip_FalseForPassenger_TrueForVerifiedDriverWithVehicle`, `Encapsulation_DriverApproveRequest_DecrementsTripAvailableSeats`, `ApproveRequest_WhenTripIsFull_DoesNotCorruptRequestStatusToApproved` |
| **BB-13** | `tests/BarengIn.Desktop.Tests/ScreenBehaviorTests.cs` | `Profile_Save_KeepsHobbiesAndBio`, `Profile_Save_PersistsSelectedFaculty`, `AppSession_SignIn_WithProfile_RestoresFacultyHobbiesBio` |
| **BB-14, BB-15** | `tests/BarengIn.Desktop.Tests/LogicTests.cs` | `AppDb_WhenEnvPointsToUnreachableServer_IsAvailableReturnsFalseAndFallbackWorks`, `AppDb_ConfigPriority_EnvOverridesAppSettings_AndPasswordHereFallsBackOffline`, `AppSettings_IsCopiedToOutputDirectory`, `AppDb_MalformedOrMissingAppSettings_AndWhitespaceEnv_FallBackCleanly` |
| **BB-16, BB-17** | `tests/BarengIn.Desktop.Tests/LogicTests.cs` | `RideRepository_MapRequestStatus_NormalizesKnownStatuses`, `RideRepository_GetDriverRides_GetIncomingRequests_AndApproveRequest_AtomicallyDecrementsSeats_AndPreventsOverbooking` |
| **BB-18** | `tests/BarengIn.Desktop.Tests/LogicTests.cs` | `UserRepository_UpdateProfile_WithBlankKtm_ReturnsFalseWithoutOpeningDb`, `UserRepository_UpdateProfile_PersistsFacultyHobbiesBio_AndAuthenticateRestoresThem` |
| **BB-19** | `tests/BarengIn.Desktop.Tests/ScreenBehaviorTests.cs` | `DriverWindow_And_ProfileWindow_WithLivePostgres_LoadApproveReject_AndPersistProfile` |
| **BB-20, BB-21, BB-22, BB-23** | `tests/BarengIn.Desktop.Tests/RouteApiServiceTests.cs` | `GetFacultyCoordinates_ReturnsDistinctCoordinatesForAllKnownFaculties_AndBulaksumurFallback`, `ParseNominatimResponse_ParsesValidArray_AndRejectsInvalidPayloads`, `ParseOsrmResponse_ParsesValidRoute_AndRejectsInvalidPayloads`, `GetRouteEstimateAsync_LiveSuccess_SetsUserAgentTimeoutInvariantUrlsAndCalculatesCo2`, `GetRouteEstimateAsync_JakalAbbreviation_RetriesWithJalanKaliurangWhenFirstLookupEmpty`, `GetRouteEstimateAsync_WithBlankPickup_ReturnsFallbackWithoutCallingNetwork`, `GetRouteEstimateAsync_OnHttpErrorStatus_ReturnsFallbackEstimate`, `GetRouteEstimateAsync_OnEmptyNominatimResult_ReturnsFallbackEstimate`, `GetRouteEstimateAsync_OnOsrmHttpError_ReturnsFallbackEstimate`, `GetRouteEstimateAsync_OnHttpRequestExceptionOrTimeout_ReturnsFallbackEstimate`, `DefaultConstructor_SetsFourSecondTimeout` |
| **BB-24** | `tests/BarengIn.Desktop.Tests/ScreenBehaviorTests.cs` & `LogicTests.cs` | `Home_FindRides_ShowsCheckingRouteLoadingState_ThenLiveRouteSummary`, `Driver_CheckRoute_And_PostTrip_DisplayRouteFallback_WithoutCrashing`, `MainWindows_EnableCanResizeWithMinimumDimensions`, `Rupiah_UsesDotThousandsSeparator`, `Co2_UsesDotDecimalRegardlessOfLocale`, `Initials_TakesUpToTwoLetters` |
