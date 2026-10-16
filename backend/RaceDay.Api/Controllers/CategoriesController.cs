using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/v1/events/{eventId:int}/categories")]
public sealed class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventCategoryDto>>> GetAll(int eventId)
    {
        if (!await db.Event.AnyAsync(item => item.EventId == eventId)) return NotFound();
        var categories = await db.EventCategory.AsNoTracking()
            .Where(category => category.EventId == eventId)
            .OrderBy(category => category.CategoryId)
            .Select(category => ToDto(category))
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("{categoryId:int}")]
    public async Task<ActionResult<EventCategoryDto>> GetById(int eventId, int categoryId)
    {
        var category = await db.EventCategory.AsNoTracking()
            .Where(item => item.EventId == eventId && item.CategoryId == categoryId)
            .Select(item => ToDto(item))
            .SingleOrDefaultAsync();
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<EventCategoryDto>> Create(int eventId, CategoryCreateRequest request)
    {
        var ownership = await CheckEventOwnership(eventId);
        if (ownership is not null) return ownership;
        if (request.MinAge.HasValue && request.MaxAge.HasValue && request.MinAge > request.MaxAge)
            return BadRequest(new { message = "MinAge cannot exceed MaxAge." });

        var category = new EventCategory
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            DistanceKm = request.DistanceKm,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge
        };
        db.EventCategory.Add(category);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { eventId, categoryId = category.CategoryId }, ToDto(category));
    }

    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<EventCategoryDto>> Update(int eventId, int categoryId, CategoryUpdateRequest request)
    {
        var ownership = await CheckEventOwnership(eventId);
        if (ownership is not null) return ownership;

        var category = await db.EventCategory.SingleOrDefaultAsync(item =>
            item.EventId == eventId && item.CategoryId == categoryId);
        if (category is null) return NotFound();

        var minAge = request.MinAge ?? category.MinAge;
        var maxAge = request.MaxAge ?? category.MaxAge;
        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            return BadRequest(new { message = "MinAge cannot exceed MaxAge." });

        if (request.Name is not null) category.Name = request.Name.Trim();
        if (request.DistanceKm.HasValue) category.DistanceKm = request.DistanceKm;
        if (request.MinAge.HasValue) category.MinAge = request.MinAge;
        if (request.MaxAge.HasValue) category.MaxAge = request.MaxAge;
        await db.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int eventId, int categoryId)
    {
        var ownership = await CheckEventOwnership(eventId);
        if (ownership is not null) return ownership;

        var category = await db.EventCategory.SingleOrDefaultAsync(item =>
            item.EventId == eventId && item.CategoryId == categoryId);
        if (category is null) return NotFound();
        if (await db.EventEnrolment.AnyAsync(enrolment => enrolment.CategoryId == categoryId))
            return Conflict(new { message = "A category with enrolments cannot be deleted." });

        db.EventCategory.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ActionResult?> CheckEventOwnership(int eventId)
    {
        var organiserId = await db.Event.Where(item => item.EventId == eventId)
            .Select(item => (int?)item.OrganiserId)
            .SingleOrDefaultAsync();
        if (organiserId is null) return NotFound();
        return organiserId == User.GetUserId() ? null : Forbid();
    }

    private static EventCategoryDto ToDto(EventCategory category) => new()
    {
        CategoryId = category.CategoryId,
        EventId = category.EventId,
        Name = category.Name,
        DistanceKm = category.DistanceKm,
        MinAge = category.MinAge,
        MaxAge = category.MaxAge
    };
}
