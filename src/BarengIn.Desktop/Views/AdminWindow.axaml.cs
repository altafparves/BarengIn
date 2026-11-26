using Avalonia.Controls;
using Avalonia.Interactivity;
using BarengIn.Desktop.Models;
using BarengIn.Domain;
using BarengIn.Domain.Enums;

namespace BarengIn.Desktop.Views;

public partial class AdminWindow : Window
{
    private int totalSharedRides = 47;
    private double totalCo2Kg = 56.4;

    public AdminWindow()
    {
        InitializeComponent();
        lblWelcome.Text = $"Hi, {AppSession.Name} ({AppSession.Role})";

        AppSession.InitializeAdminStateIfNeeded();
        lvUsers.ItemsSource = AppSession.AdminUsers;
        lvLeaderboard.ItemsSource = AppSession.Leaderboard;

        ReloadFromDatabaseIfAvailable();
        UpdateKpiCardsAndReportBanner();
    }

    private Admin CurrentAdmin =>
        AppSession.CurrentUser as Admin
        ?? new Admin(
            string.IsNullOrWhiteSpace(AppSession.Nim) ? "ADMIN/01" : AppSession.Nim,
            string.IsNullOrWhiteSpace(AppSession.Name) ? "Admin" : AppSession.Name,
            "admin@mail.ugm.ac.id",
            "admin-hash",
            "08123456789",
            Guid.Empty,
            "DTETI",
            1);

    private void ReloadFromDatabaseIfAvailable()
    {
        if (!AppDb.IsAvailable)
        {
            AppSession.InitializeAdminStateIfNeeded();
            RecalculateOfflineTotals();
            return;
        }

        try
        {
            var dbUsers = AppDb.Users.GetAllUsersForAdmin();
            if (dbUsers.Count > 0)
            {
                AppSession.AdminUsers.Clear();
                foreach (var u in dbUsers)
                {
                    AppSession.AdminUsers.Add(new AdminUserItem(
                        u.Nim,
                        u.FullName,
                        u.Role,
                        u.Faculty,
                        u.KtmFile,
                        u.VerificationStatus,
                        u.UserId));
                }
            }

            var analytics = AppDb.Rides.GetAdminAnalytics();
            totalSharedRides = analytics.TotalRides + analytics.ApprovedRequests;
            totalCo2Kg = analytics.TotalCo2SavedKg;

            if (analytics.Leaderboard.Count > 0)
            {
                AppSession.Leaderboard.Clear();
                int rank = 1;
                foreach (var item in analytics.Leaderboard)
                {
                    AppSession.Leaderboard.Add(new LeaderboardItem(
                        rank++,
                        item.Name,
                        item.Nim,
                        item.Faculty,
                        item.Role,
                        item.SharedRides,
                        item.Co2SavedKg));
                }
            }
        }
        catch
        {
            // Keep resilient in-memory data if database query fails.
            RecalculateOfflineTotals();
        }
    }

    private void RecalculateOfflineTotals()
    {
        int lbRides = AppSession.Leaderboard.Sum(l => l.SharedRides);
        double lbCo2 = AppSession.Leaderboard.Sum(l => l.Co2SavedKg);
        totalSharedRides = Math.Max(1, lbRides + AppSession.DriverTrips.Count);
        totalCo2Kg = Math.Round(Math.Max(1.2, lbCo2 + AppSession.Co2Saved), 1, MidpointRounding.AwayFromZero);
    }

    private void UpdateKpiCardsAndReportBanner()
    {
        var admin = CurrentAdmin;
        DateTime from = DateTime.Today.AddDays(-30);
        DateTime to = DateTime.Today;

        double dashboardCo2 = admin.ViewEmissionDashboard(from, to, totalCo2Kg);
        int pendingCount = AppSession.AdminUsers.Count(u => u.IsPending);
        int verifiedCount = AppSession.AdminUsers.Count(u => u.IsVerified);
        double trees = Math.Round(dashboardCo2 / 21.0, 1, MidpointRounding.AwayFromZero);

        lblTotalCo2.Text = $"{dashboardCo2:0.0} kg";
        lblTreesEquivalent.Text = $"~{trees:0.0} trees/year equivalent";
        lblTotalRides.Text = $"{totalSharedRides} trips";
        lblPendingKtms.Text = $"{pendingCount} pending";
        lblVerifiedUsers.Text = $"{verifiedCount} verified";

        lblReportSummary.Text = admin.GenerateReport(
            from,
            to,
            totalSharedRides,
            verifiedCount,
            pendingCount,
            dashboardCo2);
    }

    private void btnVerifyKtm_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not AdminUserItem item)
        {
            return;
        }

        if (!CurrentAdmin.VerifyKtm(item.UserId, VerificationStatus.Verified))
        {
            return;
        }

        if (AppDb.IsAvailable)
        {
            try
            {
                AppDb.Users.UpdateVerificationStatus(item.Nim, "Verified");
            }
            catch
            {
                // Keep UI update resilient.
            }
        }

        AppDb.UpdateOfflineUserStatus(item.Nim, "Verified");
        item.Status = "Verified";
        UpdateKpiCardsAndReportBanner();
    }

    private void btnRejectKtm_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not AdminUserItem item)
        {
            return;
        }

        if (!CurrentAdmin.VerifyKtm(item.UserId, VerificationStatus.Rejected))
        {
            return;
        }

        if (AppDb.IsAvailable)
        {
            try
            {
                AppDb.Users.UpdateVerificationStatus(item.Nim, "Rejected");
            }
            catch
            {
                // Keep UI update resilient.
            }
        }

        AppDb.UpdateOfflineUserStatus(item.Nim, "Rejected");
        item.Status = "Rejected";
        UpdateKpiCardsAndReportBanner();
    }

    private void btnSuspendUser_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not AdminUserItem item)
        {
            return;
        }

        if (!CurrentAdmin.SuspendUser(item.UserId, "Policy or KTM violation"))
        {
            return;
        }

        if (AppDb.IsAvailable)
        {
            try
            {
                AppDb.Users.UpdateVerificationStatus(item.Nim, "Suspended");
            }
            catch
            {
                // Keep UI update resilient.
            }
        }

        AppDb.UpdateOfflineUserStatus(item.Nim, "Suspended");
        item.Status = "Suspended";
        UpdateKpiCardsAndReportBanner();
    }

    private async void btnGenerateReport_Click(object? sender, RoutedEventArgs e)
    {
        UpdateKpiCardsAndReportBanner();
        await MessageBox.Show(this, lblReportSummary.Text ?? "Report generated.");
    }

    private void btnRefresh_Click(object? sender, RoutedEventArgs e)
    {
        ReloadFromDatabaseIfAvailable();
        UpdateKpiCardsAndReportBanner();
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

