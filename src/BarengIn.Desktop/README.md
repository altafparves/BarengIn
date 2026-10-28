# BarengIn.Desktop

Module 7 UI prototype: five screens (Login, Register, Passenger Home, Driver, Profile) with navigation between them. Built with [Avalonia](https://avaloniaui.net), so it runs on macOS, Windows and Linux. Sample data only: no database, no Domain reference.

Calvin's original Windows Forms version of Login and Home is kept in [`../BarengIn.Desktop.WinForms`](../BarengIn.Desktop.WinForms).

## Run

Needs the .NET 9 SDK or newer.

```bash
dotnet run --project src/BarengIn.Desktop
```

Any NIM and password work for login. Pick the Driver role to see the driver screen.

If the app crashes on start with `Avalonia.Native was not able to start the RenderTimer`, the Mac's display is asleep. Wake the screen and run it again.

## Test

```bash
dotnet test tests/BarengIn.Desktop.Tests
```

To regenerate the screenshots in `docs/modules/module-7/screenshots`:

```bash
BARENGIN_SCREENSHOTS=1 dotnet test tests/BarengIn.Desktop.Tests --filter ScreenshotTests
```
