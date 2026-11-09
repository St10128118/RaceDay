using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVCApp.Models;
using RaceDayMVCApp.Services;

namespace RaceDayMVCApp.Controllers;

[Authorize(Roles = "Participant")]
[Route("Participant")]
public sealed class ParticipantController(IRaceDayApiClient apiClient) : Controller
{
    [HttpGet("")]
    public Task<IActionResult> Index(CancellationToken cancellationToken) =>
        HistoryView(false, cancellationToken);

    [HttpGet("Results")]
    public Task<IActionResult> Results(CancellationToken cancellationToken) =>
        HistoryView(true, cancellationToken);

    [HttpPost("Enrol")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        var response = await apiClient.PostAsync<ApiEnrolment>(
            $"/api/v1/events/{eventId}/enrolments",
            new { categoryId },
            cancellationToken);
        if (!response.IsSuccess)
            TempData["Error"] = response.Error ?? "You could not be enrolled in this event.";
        else
            TempData["Success"] = "You’re on the start list. Your enrolment is pending confirmation.";

        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    [HttpPost("CancelEnrolment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEnrolment(int enrolmentId, CancellationToken cancellationToken)
    {
        var response = await apiClient.DeleteAsync<object>(
            $"/api/v1/enrolments/{enrolmentId}",
            cancellationToken);
        TempData[response.IsSuccess ? "Success" : "Error"] = response.IsSuccess
            ? "Your enrolment was cancelled."
            : response.Error ?? "The enrolment could not be cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var response = await apiClient.GetAsync<ApiUserProfile>("/api/v1/users/me", cancellationToken);
        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Error ?? "Your profile could not be loaded.";
            return RedirectToAction(nameof(Index));
        }

        return View(ToProfileViewModel(response.Data));
    }

    [HttpPost("Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var response = await apiClient.PutAsync<ApiUserProfile>("/api/v1/users/me", new
        {
            model.FirstName,
            model.LastName,
            model.Phone,
            model.EmergencyContactName,
            model.EmergencyContactPhone,
            model.OrganisationName
        }, cancellationToken);
        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "Your profile could not be updated.");
            return View(model);
        }

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    private async Task<IActionResult> HistoryView(bool onlyResults, CancellationToken cancellationToken)
    {
        var response = await apiClient.GetAsync<List<ApiParticipantHistory>>(
            "/api/v1/users/me/history",
            cancellationToken);
        if (!response.IsSuccess || response.Data is null)
            return View("Index", new ParticipantHistoryViewModel
            {
                OnlyResults = onlyResults,
                Error = response.Error ?? "Your race history could not be loaded."
            });

        var categoryNames = new Dictionary<(int EventId, int CategoryId), string>();
        string? categoryError = null;
        foreach (var eventId in response.Data.Select(item => item.Event.EventId).Distinct())
        {
            var categories = await apiClient.GetAsync<List<ApiEventCategory>>(
                $"/api/v1/events/{eventId}/categories",
                cancellationToken);
            if (!categories.IsSuccess || categories.Data is null)
            {
                categoryError ??= categories.Error ?? "Some category details could not be loaded.";
                continue;
            }

            foreach (var category in categories.Data)
                categoryNames[(eventId, category.CategoryId)] = category.Name;
        }

        var items = response.Data
            .Where(item => !onlyResults || item.Result is not null)
            .Select(item => new ParticipantHistoryItemViewModel
        {
            History = item,
            CategoryName = categoryNames.GetValueOrDefault((item.Event.EventId, item.Enrolment.CategoryId))
        }).ToArray();

        return View("Index", new ParticipantHistoryViewModel
        {
            Items = items,
            OnlyResults = onlyResults,
            Error = categoryError
        });
    }

    private static ProfileEditViewModel ToProfileViewModel(ApiUserProfile profile) => new()
    {
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        Email = profile.Email,
        Role = profile.Role,
        Phone = profile.Phone,
        EmergencyContactName = profile.EmergencyContactName,
        EmergencyContactPhone = profile.EmergencyContactPhone,
        OrganisationName = profile.OrganisationName
    };
}
