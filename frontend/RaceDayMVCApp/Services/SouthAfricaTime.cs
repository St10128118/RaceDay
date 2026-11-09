namespace RaceDayMVCApp.Services;

public static class SouthAfricaTime
{
    private static readonly Lazy<TimeZoneInfo> TimeZone = new(FindTimeZone);

    public static DateTime ToUtc(DateTime localDateTime)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
            return localDateTime;
        if (localDateTime.Kind == DateTimeKind.Local)
            return localDateTime.ToUniversalTime();

        var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, TimeZone.Value);
    }

    public static DateTime ToLocal(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind switch
        {
            DateTimeKind.Utc => utcDateTime,
            DateTimeKind.Local => utcDateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone.Value);
    }

    public static DateTime ToUtcEndOfDay(DateTime localDate) =>
        ToUtc(localDate.Date.AddDays(1).AddTicks(-1));

    private static TimeZoneInfo FindTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
        }
    }
}
