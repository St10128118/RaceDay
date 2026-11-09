using RaceDayMVCApp.Services;
using Xunit;

namespace RaceDayMVCApp.Tests;

public sealed class SouthAfricaTimeTests
{
    [Fact]
    public void ToUtc_ConvertsSouthAfricanLocalTime()
    {
        var localTime = new DateTime(2026, 7, 14, 8, 30, 0, DateTimeKind.Unspecified);

        var result = SouthAfricaTime.ToUtc(localTime);

        Assert.Equal(new DateTime(2026, 7, 14, 6, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ToLocal_InterpretsUnspecifiedDateTimeAsUtc()
    {
        var utcTime = new DateTime(2026, 7, 14, 6, 30, 0, DateTimeKind.Unspecified);

        var result = SouthAfricaTime.ToLocal(utcTime);

        Assert.Equal(new DateTime(2026, 7, 14, 8, 30, 0), result);
    }

    [Fact]
    public void ToUtc_PreservesUtcDateTime()
    {
        var utcTime = new DateTime(2026, 7, 14, 6, 30, 0, DateTimeKind.Utc);

        var result = SouthAfricaTime.ToUtc(utcTime);

        Assert.Equal(utcTime, result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void ToUtcEndOfDay_IncludesTheEntireSouthAfricanDate()
    {
        var localDate = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Unspecified);

        var result = SouthAfricaTime.ToUtcEndOfDay(localDate);

        Assert.Equal(new DateTime(2026, 7, 14, 21, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999), result);
    }
}
