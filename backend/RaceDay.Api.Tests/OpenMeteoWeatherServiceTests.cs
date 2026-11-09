using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using Xunit;

namespace RaceDay.Api.Tests;

public sealed class OpenMeteoWeatherServiceTests
{
    [Fact]
    public async Task GetForecastAsync_ReturnsSouthAfricanEventDayForecast()
    {
        var southAfricanTimeZone = FindSouthAfricanTimeZone();
        var tomorrow = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricanTimeZone).AddDays(1));
        var localEventTime = tomorrow.ToDateTime(new TimeOnly(9, 0));
        var eventUtc = TimeZoneInfo.ConvertTimeToUtc(localEventTime, southAfricanTimeZone);
        var handler = new WeatherResponseHandler(tomorrow);
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new OpenMeteoWeatherService(client, cache);
        var raceEvent = new Event
        {
            City = "Cape Town",
            Province = "Western Cape",
            EventDateTime = eventUtc
        };

        var forecast = await service.GetForecastAsync(raceEvent, CancellationToken.None);

        Assert.NotNull(forecast);
        Assert.True(forecast.Available);
        Assert.Equal(tomorrow, forecast.ForecastDate);
        Assert.Equal(12.4m, forecast.MinimumTemperatureCelsius);
        Assert.Equal(20.6m, forecast.MaximumTemperatureCelsius);
        Assert.Equal(30, forecast.PrecipitationProbabilityPercent);
        Assert.Equal(24.7m, forecast.MaximumWindSpeedKph);
        Assert.Equal("Rain", forecast.Conditions);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task GetForecastAsync_ReturnsUnavailableOutsideForecastHorizon()
    {
        var raceEvent = new Event
        {
            City = "Cape Town",
            Province = "Western Cape",
            EventDateTime = DateTime.UtcNow.AddDays(30)
        };
        var handler = new WeatherResponseHandler(DateOnly.FromDateTime(raceEvent.EventDateTime));
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new OpenMeteoWeatherService(client, cache);

        var forecast = await service.GetForecastAsync(raceEvent, CancellationToken.None);

        Assert.Null(forecast);
        Assert.Equal(0, handler.RequestCount);
    }

    private static TimeZoneInfo FindSouthAfricanTimeZone()
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

    private sealed class WeatherResponseHandler(DateOnly forecastDate) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var json = request.RequestUri!.Host == "geocoding-api.open-meteo.com"
                ? """{"results":[{"name":"Cape Town","latitude":-33.9,"longitude":18.4,"country_code":"ZA","admin1":"Western Cape"}]}"""
                : "{\"daily\":{\"time\":[\"" + forecastDate.ToString("yyyy-MM-dd") +
                    "\"],\"temperature_2m_min\":[12.4],\"temperature_2m_max\":[20.6]," +
                    "\"precipitation_probability_max\":[30],\"wind_speed_10m_max\":[24.7],\"weather_code\":[61]}}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
