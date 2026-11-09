using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public interface IWeatherService
{
    Task<EventWeatherDto?> GetForecastAsync(Event raceEvent, CancellationToken cancellationToken);
}
