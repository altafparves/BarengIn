# BarengIn: Windows Test Guide

Use this guide to test the BarengIn desktop app on Windows.

You do **not** need to install .NET, Visual Studio, or a database.
The ZIP file has everything the app needs.

## 1. Requirements

| Item | Value |
|---|---|
| Operating system | Windows 10 or Windows 11, 64-bit |
| Free disk space | About 300 MB |
| Internet | Optional. The route preview uses it. |

## 2. Get the app

1. Download `BarengIn-MVP-win-x64.zip` from this link:

   https://github.com/altafparves/BarengIn/releases/tag/v0.1.0-mvp

   Under **Assets**, click `BarengIn-MVP-win-x64.zip`.

2. Save the file on your computer.

You do not need the GitHub repository to test the app. Download only the ZIP file.

## 3. Run the app

1. Right-click the ZIP file.
2. Click **Extract All**.
3. Click **Extract**.
4. Open the new folder.
5. Make sure the folder has these files:
   - `BarengIn.Desktop.exe`
   - `appsettings.json`
   - `schema.sql`
6. Double-click `BarengIn.Desktop.exe`.

> [!WARNING]
> Do not run the app from inside the ZIP file. Always extract it first.
> Keep all files in the same folder.

You see the BarengIn **Login** screen.

### If Windows shows "Windows protected your PC"

The app has no commercial signature. This is normal.

1. Click **More info**.
2. Click **Run anyway**.

## 4. Offline mode

The app has no database by default. It runs in **offline mode**.
In this mode, you can test all main screens. Data is not saved after you close the app.

Do not change `appsettings.json`.

## 5. Test checklist

Do each step. Compare the result with the "Expected result" column.

| # | Screen | Action | Expected result |
|---|---|---|---|
| 1 | Register | Enter your details. Click **Register**. | The app accepts the account. |
| 2 | Login | Enter your email and password. Click **Login**. | You see the Home screen. |
| 3 | Passenger Home | Enter a start and an end place. Click **Find Rides**. | You see a list of rides. |
| 4 | Route preview | Look at the distance, time, and CO2 values. | The values show on the screen. |
| 5 | Driver Dashboard | Open the Driver Dashboard. | You see the trips and the requests. |
| 6 | Post Trip | Enter trip data. Click **Post Trip**. | The trip shows in the list. |
| 7 | Requests | Approve one request. Reject another request. | The status changes. |
| 8 | Profile | Change your name. Save. | The new name shows. |
| 9 | Logout | Click **Logout**. | You see the Login screen. |

## 6. Troubleshooting

| Problem | Solution |
|---|---|
| Nothing happens when you double-click the file | Wait 10 seconds. Then try again. |
| SmartScreen blocks the app | Click **More info**, then **Run anyway**. |
| The app shows an error about a missing file | Extract the ZIP again. Keep all files in one folder. |
| The route preview is slow or empty | Check your internet. The app uses an offline estimate when the internet is off. This is normal. |
| Antivirus deletes the file | Add the folder to the antivirus exceptions. Extract the ZIP again. |
| Windows is 32-bit or ARM | Ask Altaf for help. This build is for x64 only. |

## 7. Report a bug

Send this information to Altaf:

1. The screen name.
2. The steps you did.
3. What you expected.
4. What happened.
5. A screenshot.
6. Your Windows version. To find it, press `Win + R`, type `winver`, and press Enter.

