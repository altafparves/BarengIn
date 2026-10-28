using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using BarengIn.Desktop.Models;
using BarengIn.Desktop.Views;

namespace BarengIn.Desktop.Tests;

public class ScreenBehaviorTests
{
    private static HomeWindow NewHome()
    {
        AppSession.Reset();
        AppSession.SignIn("24/536741", "Passenger");
        var home = new HomeWindow();
        home.Show();
        return home;
    }

    private static DriverWindow NewDriver()
    {
        AppSession.Reset();
        AppSession.SignIn("24/536741", "Driver");
        var driver = new DriverWindow();
        driver.Show();
        return driver;
    }

    [AvaloniaFact]
    public void Home_FindRides_ShowsThreeRows()
    {
        var home = NewHome();
        home.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
        home.Click("btnFind");
        var rides = Assert.IsAssignableFrom<IEnumerable<RideRow>>(home.Find<ListBox>("lvRides").ItemsSource);
        Assert.Equal(3, rides.Count());
    }

    [AvaloniaFact]
    public void Home_FindRides_WithoutPickup_ShowsNothing()
    {
        var home = NewHome();
        home.Click("btnFind");
        Assert.Null(home.Find<ListBox>("lvRides").ItemsSource);
    }

    [AvaloniaFact]
    public void Home_RequestWithoutSelection_KeepsCo2AtZero()
    {
        var home = NewHome();
        home.Click("btnRequest");
        Assert.Equal(0, AppSession.Co2Saved);
        Assert.Equal("Estimated CO2 saved: 0.0 kg", home.Find<TextBlock>("lblCo2").Text);
    }

    [AvaloniaFact]
    public void Home_RequestWithSelection_AddsCo2()
    {
        var home = NewHome();
        home.Find<TextBox>("tbOrigin").Text = "Jakal Km 5";
        home.Click("btnFind");
        home.Find<ListBox>("lvRides").SelectedIndex = 0;
        home.Click("btnRequest");
        Assert.Equal("Estimated CO2 saved: 1.2 kg", home.Find<TextBlock>("lblCo2").Text);
    }

    [AvaloniaFact]
    public void Driver_PostTrip_AddsToList_AndRejectsEmptyPickup()
    {
        var driver = NewDriver();
        var trips = driver.Find<ListBox>("lvTrips");

        driver.Click("btnPost");
        Assert.Empty(trips.ItemsSource!.Cast<object>());

        driver.Find<TextBox>("tbOrigin").Text = "Seturan";
        driver.Click("btnPost");
        var trip = Assert.IsType<TripRow>(Assert.Single(trips.ItemsSource!.Cast<object>()));
        Assert.Equal("Seturan", trip.Pickup);
        Assert.Equal("07:30", trip.Departs);
        Assert.Equal("Rp 5.000", trip.Cost);
    }

    [AvaloniaFact]
    public void Driver_ApproveAndReject_ChangeRequestStatus()
    {
        var driver = NewDriver();
        driver.Layout();
        var requests = driver.Find<ListBox>("lvRequests").ItemsSource!.Cast<RequestItem>().ToList();

        driver.FindButton("Approve").Press();
        Assert.Equal("Approved", requests[0].Status);

        driver.Layout();
        driver.FindButton("Reject").Press();
        Assert.Equal("Rejected", requests[1].Status);
        Assert.Equal("Pending", requests[2].Status);
    }

    [AvaloniaFact]
    public void Profile_Save_KeepsHobbiesAndBio()
    {
        AppSession.Reset();
        AppSession.SignIn("24/536741", "Passenger");
        var profile = new ProfileWindow();
        profile.Show();
        profile.Find<TextBox>("tbHobbies").Text = "badminton";
        profile.Find<TextBox>("tbBio").Text = "Likes early rides.";
        profile.Click("btnSave");
        Assert.Equal("badminton", AppSession.Hobbies);
        Assert.Equal("Likes early rides.", AppSession.Bio);
    }
}
