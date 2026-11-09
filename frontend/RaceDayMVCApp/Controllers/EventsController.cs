using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVCApp.Models;
using RaceDayMVCApp.Services;

namespace RaceDayMVCApp.Controllers;

[AllowAnonymous]
[Route("Events")]
public sealed class EventsController(IRaceDayApiClient apiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? city,
        string? province,
        string? eventType,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
        {
            ModelState.AddModelError(string.Empty, "The start date must be on or before the end date.");
            return View(new EventListViewModel
            {
                City = city,
                Province = province,
                EventType = eventType,
                From = from,
                To = to
            });
        }

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(city)) query.Add($"city={Uri.EscapeDataString(city.Trim())}");
        if (!string.IsNullOrWhiteSpace(province)) query.Add($"province={Uri.EscapeDataString(province.Trim())}");
        if (!string.IsNullOrWhiteSpace(eventType)) query.Add($"eventType={Uri.EscapeDataString(eventType)}");
        if (from.HasValue)
            query.Add($"from={Uri.EscapeDataString(SouthAfricaTime.ToUtc(from.Value).ToString("O"))}");
        if (to.HasValue)
            query.Add($"to={Uri.EscapeDataString(SouthAfricaTime.ToUtcEndOfDay(to.Value).ToString("O"))}");

        var path = "/api/v1/events" + (query.Count > 0 ? $"?{string.Join("&", query)}" : "");
        var response = await apiClient.GetAsync<List<ApiEvent>>(path, cancellationToken);
        var upcoming = response.Data?
            .Where(item => item.Status == "Scheduled" && item.EventDateTime > DateTime.UtcNow)
            .OrderBy(item => item.EventDateTime)
            .ToArray() ?? [];

        return View(new EventListViewModel
        {
            Events = upcoming,
            City = city,
            Province = province,
            EventType = eventType,
            From = from,
            To = to,
            Error = response.IsSuccess ? null : response.Error
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var eventResponse = await apiClient.GetAsync<ApiEvent>($"/api/v1/events/{id}", cancellationToken);
        if (eventResponse.StatusCode == 404) return NotFound();
        if (!eventResponse.IsSuccess || eventResponse.Data is null)
        {
            TempData["Error"] = eventResponse.Error ?? "Event details could not be loaded.";
            return RedirectToAction(nameof(Index));
        }

        var categories = await apiClient.GetAsync<List<ApiEventCategory>>(
            $"/api/v1/events/{id}/categories", cancellationToken);
        var routes = await apiClient.GetAsync<List<ApiEventRoute>>(
            $"/api/v1/events/{id}/routes", cancellationToken);
        var weather = await apiClient.GetAsync<ApiWeatherForecast>(
            $"/api/v1/events/{id}/weather", cancellationToken);

        return View(new EventDetailsViewModel
        {
            Event = eventResponse.Data,
            Categories = categories.Data ?? [],
            Routes = routes.Data ?? [],
            Weather = weather.Data,
            Error = !categories.IsSuccess
                ? categories.Error
                : !routes.IsSuccess
                    ? routes.Error
                    : !weather.IsSuccess
                        ? weather.Error
                        : null
        });
    }
}
