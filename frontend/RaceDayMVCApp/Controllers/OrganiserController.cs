using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVCApp.Models;
using RaceDayMVCApp.Services;

namespace RaceDayMVCApp.Controllers;

[Authorize(Roles = "Organiser")]
[Route("Organiser")]
public sealed class OrganiserController(IRaceDayApiClient apiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var events = await apiClient.GetAsync<List<ApiEvent>>("/api/v1/events", cancellationToken);
        if (!events.IsSuccess || events.Data is null)
            return View(new OrganiserDashboardViewModel
            {
                Error = events.Error ?? "Your events could not be loaded."
            });

        var organiserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(organiserId, out var userId))
            return Forbid();

        var ownedEvents = events.Data.Where(item => item.OrganiserId == userId)
            .OrderBy(item => item.EventDateTime)
            .ToArray();
        var summaries = new List<OrganiserEventSummaryViewModel>();
        foreach (var raceEvent in ownedEvents)
        {
            var enrolments = await apiClient.GetAsync<List<ApiEnrolment>>(
                $"/api/v1/events/{raceEvent.EventId}/enrolments",
                cancellationToken);
            summaries.Add(new OrganiserEventSummaryViewModel
            {
                Event = raceEvent,
                EnrolmentCount = enrolments.Data?.Count ?? 0,
                CountError = enrolments.IsSuccess ? null : enrolments.Error
            });
        }

        return View(new OrganiserDashboardViewModel { Events = summaries });
    }

    [HttpGet("Events/Create")]
    public IActionResult CreateEvent() => View("EventForm", new EventEditorViewModel
    {
        EventDateTime = SouthAfricaTime.ToLocal(DateTime.UtcNow.AddDays(30))
    });

    [HttpPost("Events/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventEditorViewModel model, CancellationToken cancellationToken)
    {
        if (model.EventDateTime <= SouthAfricaTime.ToLocal(DateTime.UtcNow))
            ModelState.AddModelError(nameof(model.EventDateTime), "Choose a future date and time.");
        if (!ModelState.IsValid) return View("EventForm", model);

        var response = await apiClient.PostAsync<ApiEvent>("/api/v1/events", new
        {
            model.Name,
            model.Description,
            model.EventType,
            model.Location,
            model.City,
            model.Province,
            model.DistanceKm,
            EventDateTime = SouthAfricaTime.ToUtc(model.EventDateTime),
            Status = "Scheduled",
            model.MaxParticipants
        }, cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The event could not be created.");
            return View("EventForm", model);
        }

        TempData["Success"] = "Your event has been created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Events/{eventId:int}/Edit")]
    public async Task<IActionResult> EditEvent(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await GetOwnedEventAsync(eventId, cancellationToken);
        if (raceEvent is null) return RedirectToAction(nameof(Index));

        return View("EventForm", new EventEditorViewModel
        {
            EventId = raceEvent.EventId,
            Name = raceEvent.Name,
            Description = raceEvent.Description,
            EventType = raceEvent.EventType,
            Location = raceEvent.Location,
            City = raceEvent.City,
            Province = raceEvent.Province,
            DistanceKm = raceEvent.DistanceKm,
            EventDateTime = SouthAfricaTime.ToLocal(raceEvent.EventDateTime),
            MaxParticipants = raceEvent.MaxParticipants
        });
    }

    [HttpPost("Events/{eventId:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(
        int eventId,
        EventEditorViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.EventDateTime <= SouthAfricaTime.ToLocal(DateTime.UtcNow))
            ModelState.AddModelError(nameof(model.EventDateTime), "Choose a future date and time.");
        if (!ModelState.IsValid)
        {
            model.EventId = eventId;
            return View("EventForm", model);
        }

        var response = await apiClient.PutAsync<ApiEvent>($"/api/v1/events/{eventId}", new
        {
            model.Name,
            model.Description,
            model.EventType,
            model.Location,
            model.City,
            model.Province,
            model.DistanceKm,
            EventDateTime = SouthAfricaTime.ToUtc(model.EventDateTime),
            model.MaxParticipants
        }, cancellationToken);
        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The event could not be updated.");
            model.EventId = eventId;
            return View("EventForm", model);
        }

        TempData["Success"] = "Your event has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Events/{eventId:int}/Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEvent(int eventId, CancellationToken cancellationToken)
    {
        var response = await apiClient.PostAsync<ApiEvent>(
            $"/api/v1/events/{eventId}/cancel",
            new { },
            cancellationToken);
        TempData[response.IsSuccess ? "Success" : "Error"] = response.IsSuccess
            ? "The event is cancelled. Enrolment history has been kept."
            : response.Error ?? "The event could not be cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Events/{eventId:int}/Complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteEvent(int eventId, CancellationToken cancellationToken)
    {
        var response = await apiClient.PutAsync<ApiEvent>(
            $"/api/v1/events/{eventId}",
            new { status = "Completed" },
            cancellationToken);
        TempData[response.IsSuccess ? "Success" : "Error"] = response.IsSuccess
            ? "The event has been marked Completed."
            : response.Error ?? "The event could not be marked Completed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Events/{eventId:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int eventId, CancellationToken cancellationToken)
    {
        var response = await apiClient.DeleteAsync<object>(
            $"/api/v1/events/{eventId}",
            cancellationToken);
        TempData[response.IsSuccess ? "Success" : "Error"] = response.IsSuccess
            ? "The event was deleted."
            : response.Error ?? "The event could not be deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Events/{eventId:int}/Categories")]
    public async Task<IActionResult> Categories(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await GetOwnedEventAsync(eventId, cancellationToken);
        if (raceEvent is null) return RedirectToAction(nameof(Index));
        var response = await apiClient.GetAsync<List<ApiEventCategory>>(
            $"/api/v1/events/{eventId}/categories",
            cancellationToken);
        return View(new ManageCategoriesViewModel
        {
            Event = raceEvent,
            Categories = response.Data ?? [],
            Error = response.IsSuccess ? null : response.Error
        });
    }

    [HttpGet("Events/{eventId:int}/Categories/Create")]
    public async Task<IActionResult> CreateCategory(int eventId, CancellationToken cancellationToken)
    {
        if (await GetOwnedEventAsync(eventId, cancellationToken) is null)
            return RedirectToAction(nameof(Index));
        return View("CategoryForm", new CategoryEditorViewModel { EventId = eventId });
    }

    [HttpPost("Events/{eventId:int}/Categories/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(
        int eventId,
        CategoryEditorViewModel model,
        CancellationToken cancellationToken)
    {
        model.EventId = eventId;
        ValidateAgeRange(model);
        if (!ModelState.IsValid) return View("CategoryForm", model);

        var response = await apiClient.PostAsync<ApiEventCategory>(
            $"/api/v1/events/{eventId}/categories",
            new { model.Name, model.DistanceKm, model.MinAge, model.MaxAge },
            cancellationToken);
        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The category could not be created.");
            return View("CategoryForm", model);
        }

        TempData["Success"] = "The category has been added.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpGet("Events/{eventId:int}/Categories/{categoryId:int}/Edit")]
    public async Task<IActionResult> EditCategory(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        if (await GetOwnedEventAsync(eventId, cancellationToken) is null)
            return RedirectToAction(nameof(Index));
        var response = await apiClient.GetAsync<ApiEventCategory>(
            $"/api/v1/events/{eventId}/categories/{categoryId}",
            cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Error ?? "The category could not be loaded.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        return View("CategoryForm", new CategoryEditorViewModel
        {
            EventId = eventId,
            CategoryId = categoryId,
            Name = response.Data.Name,
            DistanceKm = response.Data.DistanceKm,
            MinAge = response.Data.MinAge,
            MaxAge = response.Data.MaxAge
        });
    }

    [HttpPost("Events/{eventId:int}/Categories/{categoryId:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(
        int eventId,
        int categoryId,
        CategoryEditorViewModel model,
        CancellationToken cancellationToken)
    {
        model.EventId = eventId;
        model.CategoryId = categoryId;
        ValidateAgeRange(model);
        if (!ModelState.IsValid) return View("CategoryForm", model);

        var response = await apiClient.PutAsync<ApiEventCategory>(
            $"/api/v1/events/{eventId}/categories/{categoryId}",
            new { model.Name, model.DistanceKm, model.MinAge, model.MaxAge },
            cancellationToken);
        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The category could not be updated.");
            return View("CategoryForm", model);
        }

        TempData["Success"] = "The category has been updated.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost("Events/{eventId:int}/Categories/{categoryId:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        var response = await apiClient.DeleteAsync<object>(
            $"/api/v1/events/{eventId}/categories/{categoryId}",
            cancellationToken);
        TempData[response.IsSuccess ? "Success" : "Error"] = response.IsSuccess
            ? "The category was deleted."
            : response.Error ?? "The category could not be deleted.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpGet("Events/{eventId:int}/Routes")]
    public async Task<IActionResult> Routes(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await GetOwnedEventAsync(eventId, cancellationToken);
        if (raceEvent is null) return RedirectToAction(nameof(Index));
        var response = await apiClient.GetAsync<List<ApiEventRoute>>(
            $"/api/v1/events/{eventId}/routes",
            cancellationToken);
        return View(new ManageRoutesViewModel
        {
            Event = raceEvent,
            Routes = response.Data ?? [],
            Error = response.IsSuccess ? null : response.Error
        });
    }

    [HttpGet("Events/{eventId:int}/Routes/Create")]
    public async Task<IActionResult> CreateRoute(int eventId, CancellationToken cancellationToken)
    {
        if (await GetOwnedEventAsync(eventId, cancellationToken) is null)
            return RedirectToAction(nameof(Index));
        return View("RouteForm", new RouteEditorViewModel { EventId = eventId });
    }

    [HttpPost("Events/{eventId:int}/Routes/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoute(
        int eventId,
        RouteEditorViewModel model,
        CancellationToken cancellationToken)
    {
        model.EventId = eventId;
        if (!ModelState.IsValid) return View("RouteForm", model);
        var response = await apiClient.PostAsync<ApiEventRoute>(
            $"/api/v1/events/{eventId}/routes",
            new { model.Name, model.DistanceKm, model.GPXUrl, model.ElevationGainMeters },
            cancellationToken);
        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The route could not be created.");
            return View("RouteForm", model);
        }

        TempData["Success"] = "The route has been added.";
        return RedirectToAction(nameof(Routes), new { eventId });
    }

    [HttpGet("Events/{eventId:int}/Routes/{routeId:int}/Edit")]
    public async Task<IActionResult> EditRoute(int eventId, int routeId, CancellationToken cancellationToken)
    {
        if (await GetOwnedEventAsync(eventId, cancellationToken) is null)
            return RedirectToAction(nameof(Index));
        var response = await apiClient.GetAsync<ApiEventRoute>($"/api/v1/routes/{routeId}", cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Error ?? "The route could not be loaded.";
            return RedirectToAction(nameof(Routes), new { eventId });
        }

        return View("RouteForm", new RouteEditorViewModel
        {
            EventId = eventId,
            RouteId = routeId,
            Name = response.Data.Name,
            DistanceKm = response.Data.DistanceKm,
            GPXUrl = response.Data.GPXUrl,
            ElevationGainMeters = response.Data.ElevationGainMeters
        });
    }

    [HttpPost("Events/{eventId:int}/Routes/{routeId:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoute(
        int eventId,
        int routeId,
        RouteEditorViewModel model,
        CancellationToken cancellationToken)
    {
        model.EventId = eventId;
        model.RouteId = routeId;
        if (!ModelState.IsValid) return View("RouteForm", model);
        var response = await apiClient.PutAsync<ApiEventRoute>(
            $"/api/v1/routes/{routeId}",
            new { model.Name, model.DistanceKm, model.GPXUrl, model.ElevationGainMeters },
            cancellationToken);
        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The route could not be updated.");
            return View("RouteForm", model);
        }

        TempData["Success"] = "The route has been updated.";
        return RedirectToAction(nameof(Routes), new { eventId });
    }

    [HttpPost("Events/{eventId:int}/Routes/{routeId:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoute(int eventId, int routeId, CancellationToken cancellationToken)
    {
        var response = await apiClient.DeleteAsync<object>($"/api/v1/routes/{routeId}", cancellationToken);
        TempData[response.IsSuccess ? "Success" : "Error"] = response.IsSuccess
            ? "The route was deleted."
            : response.Error ?? "The route could not be deleted.";
        return RedirectToAction(nameof(Routes), new { eventId });
    }

    [HttpGet("Events/{eventId:int}/Enrolments")]
    public async Task<IActionResult> Enrolments(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await GetOwnedEventAsync(eventId, cancellationToken);
        if (raceEvent is null) return RedirectToAction(nameof(Index));
        var enrolments = await apiClient.GetAsync<List<ApiEnrolment>>(
            $"/api/v1/events/{eventId}/enrolments",
            cancellationToken);
        var results = await apiClient.GetAsync<List<ApiResult>>(
            $"/api/v1/events/{eventId}/results",
            cancellationToken);
        return View(new EnrolmentsViewModel
        {
            Event = raceEvent,
            Enrolments = enrolments.Data ?? [],
            Results = results.Data?.ToDictionary(item => item.EnrolmentId) ?? new Dictionary<int, ApiResult>(),
            Error = !enrolments.IsSuccess
                ? enrolments.Error
                : !results.IsSuccess
                    ? results.Error
                    : null
        });
    }

    [HttpGet("Events/{eventId:int}/Enrolments/{enrolmentId:int}/Result")]
    public async Task<IActionResult> EditResult(int eventId, int enrolmentId, CancellationToken cancellationToken)
    {
        var raceEvent = await GetOwnedEventAsync(eventId, cancellationToken);
        if (raceEvent is null) return RedirectToAction(nameof(Index));
        if (raceEvent.Status != "Completed")
        {
            TempData["Error"] = "Mark the event Completed before entering results.";
            return RedirectToAction(nameof(Enrolments), new { eventId });
        }
        var enrolments = await apiClient.GetAsync<List<ApiEnrolment>>(
            $"/api/v1/events/{eventId}/enrolments",
            cancellationToken);
        var enrolment = enrolments.Data?.SingleOrDefault(item => item.EnrolmentId == enrolmentId);
        if (!enrolments.IsSuccess || enrolment is null)
        {
            TempData["Error"] = enrolments.Error ?? "The participant enrolment could not be found.";
            return RedirectToAction(nameof(Enrolments), new { eventId });
        }

        var results = await apiClient.GetAsync<List<ApiResult>>(
            $"/api/v1/events/{eventId}/results",
            cancellationToken);
        if (!results.IsSuccess)
        {
            TempData["Error"] = results.Error ?? "Event results could not be loaded.";
            return RedirectToAction(nameof(Enrolments), new { eventId });
        }
        var result = results.Data?.SingleOrDefault(item => item.EnrolmentId == enrolmentId);
        return View("ResultForm", new ResultEditorViewModel
        {
            EventId = eventId,
            EnrolmentId = enrolmentId,
            ResultId = result?.ResultId,
            Status = result?.Status ?? "Finished",
            FinishTimeSeconds = result?.FinishTimeSeconds,
            PositionOverall = result?.PositionOverall,
            PositionCategory = result?.PositionCategory,
            PositionGender = result?.PositionGender
        });
    }

    [HttpPost("Events/{eventId:int}/Enrolments/{enrolmentId:int}/Result")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(
        int eventId,
        int enrolmentId,
        ResultEditorViewModel model,
        CancellationToken cancellationToken)
    {
        model.EventId = eventId;
        model.EnrolmentId = enrolmentId;
        if (model.Status == "Finished" &&
            (!model.FinishTimeSeconds.HasValue || !model.PositionOverall.HasValue))
            ModelState.AddModelError(string.Empty, "Finished results need a finish time and overall position.");
        if (model.Status != "Finished")
        {
            model.FinishTimeSeconds = null;
            model.PositionOverall = null;
            model.PositionCategory = null;
            model.PositionGender = null;
        }
        if (!ModelState.IsValid) return View("ResultForm", model);

        var payload = new
        {
            model.Status,
            model.FinishTimeSeconds,
            model.PositionOverall,
            model.PositionCategory,
            model.PositionGender
        };
        var response = model.ResultId.HasValue
            ? await apiClient.PutAsync<ApiResult>(
                $"/api/v1/results/{model.ResultId.Value}",
                payload,
                cancellationToken)
            : await apiClient.PostAsync<ApiResult>(
                $"/api/v1/enrolments/{enrolmentId}/result",
                payload,
                cancellationToken);
        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The result could not be saved.");
            return View("ResultForm", model);
        }

        TempData["Success"] = "The result has been saved.";
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    [HttpPost("Events/{eventId:int}/Results/{resultId:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResult(
        int eventId,
        int resultId,
        CancellationToken cancellationToken)
    {
        var response = await apiClient.DeleteAsync<object>(
            $"/api/v1/results/{resultId}",
            cancellationToken);
        TempData[response.IsSuccess ? "Success" : "Error"] = response.IsSuccess
            ? "The result was deleted."
            : response.Error ?? "The result could not be deleted.";
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    private async Task<ApiEvent?> GetOwnedEventAsync(int eventId, CancellationToken cancellationToken)
    {
        var response = await apiClient.GetAsync<ApiEvent>($"/api/v1/events/{eventId}", cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Error ?? "The event could not be loaded.";
            return null;
        }

        var organiserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(organiserId, out var userId) || response.Data.OrganiserId != userId)
        {
            TempData["Error"] = "You can only manage events that you created.";
            return null;
        }
        return response.Data;
    }

    private void ValidateAgeRange(CategoryEditorViewModel model)
    {
        if (model.MinAge.HasValue && model.MaxAge.HasValue && model.MinAge > model.MaxAge)
            ModelState.AddModelError(nameof(model.MaxAge), "Maximum age must be at least the minimum age.");
    }
}
