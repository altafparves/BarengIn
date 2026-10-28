using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Logic;
using BarengIn.Desktop.Models;

namespace BarengIn.Desktop.Views;

public partial class DriverWindow : Window
{
    private readonly ObservableCollection<TripRow> trips = new();

    public DriverWindow()
    {
        InitializeComponent();
        lblWelcome.Text = "Hi, " + AppSession.Name + " (" + AppSession.Role + ")";
        cbFaculty.ItemsSource = Faculties.All;
        cbFaculty.SelectedItem = AppSession.Faculty;
        lvTrips.ItemsSource = trips;
        lvRequests.ItemsSource = new ObservableCollection<RequestItem>
        {
            new("Rani Putri", "Jakal Km 5", "MIPA"),
            new("Budi Santoso", "Condongcatur", "Engineering (FT)"),
            new("Sari Dewi", "Seturan", "Medicine"),
        };
    }

    private async void btnPost_Click(object? sender, RoutedEventArgs e)
    {
        string origin = (tbOrigin.Text ?? "").Trim();
        if (origin == "")
        {
            await MessageBox.Show(this, "Please enter your pickup area.");
            return;
        }

        TimeSpan time = dtpTime.SelectedTime ?? TimeSpan.Zero;
        trips.Add(new TripRow(
            origin,
            cbFaculty.SelectedItem as string ?? Faculties.All[0],
            time.ToString(@"hh\:mm"),
            (int)(nudSeats.Value ?? 1),
            Display.Rupiah((int)(nudCost.Value ?? 0))));
        tbOrigin.Text = "";
    }

    private void btnApprove_Click(object? sender, RoutedEventArgs e)
    {
        if (((Button)sender!).DataContext is RequestItem item)
        {
            item.Status = "Approved";
        }
    }

    private void btnReject_Click(object? sender, RoutedEventArgs e)
    {
        if (((Button)sender!).DataContext is RequestItem item)
        {
            item.Status = "Rejected";
        }
    }

    private void btnProfile_Click(object? sender, RoutedEventArgs e)
    {
        Navigator.Go<ProfileWindow>(this);
    }

    private void btnLogout_Click(object? sender, RoutedEventArgs e)
    {
        Navigator.Logout(this);
    }
}
