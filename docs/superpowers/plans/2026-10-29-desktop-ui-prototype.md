# Desktop UI Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Complete the Module 7 homework: a 5-screen BarengIn UI with working navigation that runs on macOS.

**Architecture:** Avalonia app (`src/BarengIn.Desktop`), one `Window` per screen, code-behind only, in-memory sample data. Pure logic lives in `Logic/` so it can be unit tested. Calvin's original WinForms screens stay untouched in `src/BarengIn.Desktop.WinForms`.

**Tech Stack:** C#, .NET 9, Avalonia 12 (Fluent theme), xUnit + Avalonia.Headless for tests and screenshots.

**Spec:** agreed in chat, 2026-10-29 (Core 5 screens, Avalonia, branch from `develop`, screenshots into the worksheet).

## Global Constraints

- Keep Calvin's look: accent `#228B5E`, title "BarengIn", control names `tbUsername`, `tbPassword`, `btnLogin`, `btnClose`, `lvRides`, `btnFind`, `btnRequest`, `btnLogout`.
- Fixed-size windows, centered on screen.
- No database, no Domain project reference; sample data only.
- No code or folders under `src/BarengIn.Web`.

## Review Focus

- Empty or whitespace NIM/password at login: show a message, do not navigate.
- Class time near midnight (00:15 minus 60 min): departure wraps to 23:15, not a negative time.
- Request Ride with no row selected: message, CO2 unchanged.
- Register with mismatched passwords or no KTM file: error message, no navigation.
- Close button on Login exits the app; closing any other window must not leave the app running with no windows.

---

### Task 1: Logic and tests
**Files:** Create `Logic/RideLogic.cs`, `Logic/Validation.cs`, `Models/*.cs`, `tests/BarengIn.Desktop.Tests/LogicTests.cs`
**Produces:** `RideLogic.Departure(TimeSpan classStart, int minutesBefore) -> string "HH:mm"`, `RideLogic.Find(string faculty, TimeSpan classStart) -> List<RideRow>`, `RideLogic.Co2PerRequest = 1.2`, `Validation.Login(nim, password) -> string?`, `Validation.Register(...) -> string?`, `AppSession` (static current user).
- [ ] Write failing tests (midnight wrap, 3 rides returned, empty login rejected, mismatched password rejected); run, see fail
- [ ] Implement; run, see pass; commit

### Task 2: Shell and shared UI
**Files:** `App.axaml` (green button style), `Navigator.cs`, `MessageBox.cs`; delete `MainWindow`
**Produces:** `Navigator.Go<T>(Window from) where T : Window, new()`, `Navigator.GoHome(Window from)` (by role), `MessageBox.Show(Window owner, string message)`
- [ ] Implement; `dotnet build` passes; commit

### Task 3: Login, Register, Passenger Home
**Files:** `Views/LoginWindow`, `Views/RegisterWindow`, `Views/HomeWindow` (.axaml + .cs)
- [ ] Port Calvin's screens; add "Create account" link, Profile button
- [ ] Headless test: login as Passenger lands on `HomeWindow`; Find Rides shows 3 rows; Request without selection leaves CO2 at 0.0
- [ ] Commit

### Task 4: Driver and Profile
**Files:** `Views/DriverWindow`, `Views/ProfileWindow`
- [ ] Post Trip list, Approve/Reject on 3 sample requests, Profile edit and Back by role
- [ ] Headless test: login as Driver lands on `DriverWindow`; Approve changes status; Profile Back returns to the right home
- [ ] Commit

### Task 5: Run, screenshot, worksheet
- [ ] `dotnet run` launches without crash
- [ ] Screenshot test writes PNGs to `docs/modules/module-7/screenshots/`
- [ ] Fill `WORKSHEET HOMEWORK 7.docx` with screenshots, copy to the same docs folder
- [ ] Update README/Contributing note for how to run on Mac; push branch
