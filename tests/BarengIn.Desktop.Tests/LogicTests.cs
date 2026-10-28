using BarengIn.Desktop.Logic;

namespace BarengIn.Desktop.Tests;

public class LogicTests
{
    [Fact]
    public void Departure_SubtractsMinutes()
    {
        Assert.Equal("07:30", RideLogic.Departure(new TimeSpan(8, 0, 0), 30));
    }

    [Fact]
    public void Departure_WrapsPastMidnight()
    {
        Assert.Equal("23:15", RideLogic.Departure(new TimeSpan(0, 15, 0), 60));
    }

    [Fact]
    public void Find_ReturnsThreeRidesForSelectedFaculty()
    {
        var rides = RideLogic.Find("MIPA", new TimeSpan(8, 0, 0));
        Assert.Equal(3, rides.Count);
        Assert.All(rides, r => Assert.Equal("MIPA", r.Faculty));
        Assert.Equal(new[] { "07:30", "07:15", "07:00" }, rides.Select(r => r.Departs));
    }

    [Theory]
    [InlineData("", "pw")]
    [InlineData("   ", "pw")]
    [InlineData("24/536741", "")]
    public void Login_RejectsEmptyInput(string nim, string pw)
    {
        Assert.NotNull(Validation.Login(nim, pw));
    }

    [Fact]
    public void Login_AcceptsFilledInput()
    {
        Assert.Null(Validation.Login("24/536741", "secret"));
    }

    [Fact]
    public void Register_RejectsMismatchedPasswords()
    {
        Assert.Equal("Passwords do not match.",
            Validation.Register("Altaf", "24/536741", "secret1", "secret2", "ktm.jpg"));
    }

    [Fact]
    public void Register_RejectsMissingKtm()
    {
        Assert.NotNull(Validation.Register("Altaf", "24/536741", "secret1", "secret1", ""));
    }

    [Fact]
    public void Register_AcceptsCompleteForm()
    {
        Assert.Null(Validation.Register("Altaf", "24/536741", "secret1", "secret1", "ktm.jpg"));
    }
}

public class DisplayTests
{
    [Theory]
    [InlineData(5000, "Rp 5.000")]
    [InlineData(1250000, "Rp 1.250.000")]
    [InlineData(0, "Rp 0")]
    public void Rupiah_UsesDotThousandsSeparator(int amount, string expected)
    {
        Assert.Equal(expected, Display.Rupiah(amount));
    }

    [Theory]
    [InlineData(0, "Estimated CO2 saved: 0.0 kg")]
    [InlineData(2.4, "Estimated CO2 saved: 2.4 kg")]
    public void Co2_UsesDotDecimalRegardlessOfLocale(double kg, string expected)
    {
        Assert.Equal(expected, Display.Co2(kg));
    }

    [Theory]
    [InlineData("Altaf Parves", "AP")]
    [InlineData("calvin", "C")]
    [InlineData("24/536741", "U")]
    public void Initials_TakesUpToTwoLetters(string name, string expected)
    {
        Assert.Equal(expected, Display.Initials(name));
    }
}
