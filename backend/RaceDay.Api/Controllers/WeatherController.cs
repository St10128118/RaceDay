using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/v1/events/{eventId:int}/weather")]
public sealed class WeatherController(
    RaceDayDbContext db,
    IWeatherService weatherService,
    ILogger<WeatherController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EventWeatherDto>> GetForecast(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Event.AsNoTracking()
            .SingleOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound();

        try
        {
            var forecast = await weatherService.GetForecastAsync(raceEvent, cancellationToken);
            return Ok(forecast ?? new EventWeatherDto
            {
                Available = false,
                Message = "A forecast is not available yet. Check again closer to the event."
            });
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Weather provider request failed for event {EventId}.", eventId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Weather information is temporarily unavailable."
            });
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Weather provider request timed out for event {EventId}.", eventId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Weather information is temporarily unavailable."
            });
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Weather provider returned an invalid response for event {EventId}.", eventId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Weather information is temporarily unavailable."
            });
        }
    }
}
