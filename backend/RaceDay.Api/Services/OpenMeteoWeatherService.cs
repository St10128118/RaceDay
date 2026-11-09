using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public sealed class OpenMeteoWeatherService(HttpClient httpClient, IMemoryCache cache) : IWeatherService
{
    private const int ForecastHorizonDays = 16;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<EventWeatherDto?> GetForecastAsync(Event raceEvent, CancellationToken cancellationToken)
    {
        var southAfricanTimeZone = GetSouthAfricanTimeZone();
        var southAfricanToday = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricanTimeZone).Date);
        var eventUtc = DateTime.SpecifyKind(raceEvent.EventDateTime, DateTimeKind.Utc);
        var eventDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(eventUtc, southAfricanTimeZone).Date);
        var daysUntilEvent = eventDate.DayNumber - southAfricanToday.DayNumber;
        if (daysUntilEvent < 0 || daysUntilEvent >= ForecastHorizonDays)
            return null;

        var coordinates = await GetCoordinatesAsync(raceEvent.City, raceEvent.Province, cancellationToken);
        if (coordinates is null)
            return null;

        var cacheKey = $"weather:{coordinates.Value.Latitude:F3}:{coordinates.Value.Longitude:F3}:{eventDate:yyyy-MM-dd}";
        if (cache.TryGetValue(cacheKey, out EventWeatherDto? cached))
            return cached;

        var forecastUri = "https://api.open-meteo.com/v1/forecast" +
            $"?latitude={coordinates.Value.Latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={coordinates.Value.Longitude.ToString(CultureInfo.InvariantCulture)}" +
            "&daily=temperature_2m_min,temperature_2m_max,precipitation_probability_max,wind_speed_10m_max,weather_code" +
            "&timezone=Africa%2FJohannesburg&forecast_days=16";
        using var forecastResponse = await httpClient.GetAsync(forecastUri, cancellationToken);
        forecastResponse.EnsureSuccessStatusCode();
        using var forecastDocument = JsonDocument.Parse(
            await forecastResponse.Content.ReadAsStringAsync(cancellationToken));

        if (!forecastDocument.RootElement.TryGetProperty("daily", out var daily) ||
            !daily.TryGetProperty("time", out var dates) ||
            !daily.TryGetProperty("temperature_2m_min", out var minimums) ||
            !daily.TryGetProperty("temperature_2m_max", out var maximums) ||
            !daily.TryGetProperty("precipitation_probability_max", out var precipitation) ||
            !daily.TryGetProperty("wind_speed_10m_max", out var windSpeeds) ||
            !daily.TryGetProperty("weather_code", out var codes))
            return null;

        var forecastIndex = -1;
        for (var index = 0; index < dates.GetArrayLength(); index++)
        {
            if (DateOnly.TryParse(dates[index].GetString(), CultureInfo.InvariantCulture, out var date) &&
                date == eventDate)
            {
                forecastIndex = index;
                break;
            }
        }

        if (forecastIndex < 0 ||
            !TryGetDecimal(minimums[forecastIndex], out var minimum) ||
            !TryGetDecimal(maximums[forecastIndex], out var maximum) ||
            !TryGetInt(precipitation[forecastIndex], out var rainChance) ||
            !TryGetDecimal(windSpeeds[forecastIndex], out var windSpeed) ||
            !TryGetInt(codes[forecastIndex], out var weatherCode))
            return null;

        var forecast = new EventWeatherDto
        {
            Available = true,
            ForecastDate = eventDate,
            MinimumTemperatureCelsius = minimum,
            MaximumTemperatureCelsius = maximum,
            PrecipitationProbabilityPercent = rainChance,
            MaximumWindSpeedKph = windSpeed,
            Conditions = DescribeWeatherCode(weatherCode)
        };
        cache.Set(cacheKey, forecast, CacheDuration);
        return forecast;
    }

    private async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(
        string city,
        string province,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"geocode:ZA:{city.Trim().ToLowerInvariant()}:{province.Trim().ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out (double Latitude, double Longitude)? cached))
            return cached;

        var uri = "https://geocoding-api.open-meteo.com/v1/search" +
            $"?name={Uri.EscapeDataString(city)}&count=10&language=en&format=json";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            return null;

        (double Latitude, double Longitude)? firstSouthAfricanMatch = null;
        foreach (var result in results.EnumerateArray())
        {
            if (!result.TryGetProperty("country_code", out var countryCode) ||
                !string.Equals(countryCode.GetString(), "ZA", StringComparison.OrdinalIgnoreCase) ||
                !TryGetDouble(result, "latitude", out var latitude) ||
                !TryGetDouble(result, "longitude", out var longitude))
                continue;

            var coordinates = (Latitude: latitude, Longitude: longitude);
            firstSouthAfricanMatch ??= coordinates;
            if (result.TryGetProperty("admin1", out var admin1) &&
                string.Equals(admin1.GetString(), province, StringComparison.OrdinalIgnoreCase))
            {
                cache.Set(cacheKey, coordinates, TimeSpan.FromDays(7));
                return coordinates;
            }
        }

        if (firstSouthAfricanMatch is not { } fallback)
            return null;

        cache.Set(cacheKey, fallback, TimeSpan.FromDays(7));
        return fallback;
    }

    private static TimeZoneInfo GetSouthAfricanTimeZone()
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

    private static bool TryGetDouble(JsonElement element, string propertyName, out double value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetDouble(out value);
    }

    private static bool TryGetDecimal(JsonElement element, out decimal value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value);
    }

    private static bool TryGetInt(JsonElement element, out int value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value);
    }

    private static string DescribeWeatherCode(int code) => code switch
    {
        0 => "Clear",
        1 or 2 => "Mostly clear",
        3 => "Cloudy",
        45 or 48 => "Fog",
        51 or 53 or 55 or 56 or 57 => "Drizzle",
        61 or 63 or 65 or 66 or 67 => "Rain",
        71 or 73 or 75 or 77 => "Snow",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 or 96 or 99 => "Thunderstorms",
        _ => "Conditions unavailable"
    };
}
