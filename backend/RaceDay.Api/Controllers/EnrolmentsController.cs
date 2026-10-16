using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class EnrolmentsController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetForEvent(int eventId)
    {
        var ownership = await CheckEventOwnership(eventId);
        if (ownership is not null) return ownership;

        var enrolments = await db.EventEnrolment.AsNoTracking()
            .Where(enrolment => enrolment.EventId == eventId)
            .OrderBy(enrolment => enrolment.EnrolmentId)
            .Select(enrolment => ToDto(enrolment))
            .ToListAsync();
        return Ok(enrolments);
    }

    [HttpGet("enrolments/{enrolmentId:int}")]
    [Authorize]
    public async Task<ActionResult<EnrolmentDto>> GetById(int enrolmentId)
    {
        var enrolment = await db.EventEnrolment.AsNoTracking()
            .SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound();
        if (!await CanViewEnrolment(enrolment)) return Forbid();
        return Ok(ToDto(enrolment));
    }

    [HttpPost("events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<EnrolmentDto>> Create(int eventId, EnrolmentCreateRequest request)
    {
        var participantId = User.GetUserId();
        if (participantId is null) return Forbid();

        var eventInfo = await db.Event.Where(item => item.EventId == eventId)
            .Select(item => new { item.EventId, item.MaxParticipants })
            .SingleOrDefaultAsync();
        if (eventInfo is null) return NotFound();

        var categoryExists = await db.EventCategory.AnyAsync(category =>
            category.CategoryId == request.CategoryId && category.EventId == eventId);
        if (!categoryExists) return BadRequest(new { message = "Category does not belong to this event." });

        if (await db.EventEnrolment.AnyAsync(item =>
                item.EventId == eventId && item.ParticipantId == participantId.Value))
            return Conflict(new { message = "You are already enrolled in this event." });

        if (eventInfo.MaxParticipants is int limit &&
            await db.EventEnrolment.CountAsync(item => item.EventId == eventId) >= limit)
            return Conflict(new { message = "The event has reached its participant limit." });

        var enrolment = new EventEnrolment
        {
            EventId = eventId,
            CategoryId = request.CategoryId,
            ParticipantId = participantId.Value,
            EnrolmentDate = DateTime.UtcNow,
            PaymentStatus = "Pending",
            IsConfirmed = false
        };
        db.EventEnrolment.Add(enrolment);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { enrolmentId = enrolment.EnrolmentId }, ToDto(enrolment));
    }

    [HttpPut("enrolments/{enrolmentId:int}")]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<EnrolmentDto>> Update(int enrolmentId, EnrolmentUpdateRequest request)
    {
        var participantId = User.GetUserId();
        if (participantId is null) return Forbid();

        var enrolment = await db.EventEnrolment.SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound();
        if (enrolment.ParticipantId != participantId.Value) return Forbid();

        if (request.PaymentStatus is not null) enrolment.PaymentStatus = request.PaymentStatus.Trim();
        if (request.BibNumber is not null) enrolment.BibNumber = request.BibNumber.Trim();
        await db.SaveChangesAsync();
        return Ok(ToDto(enrolment));
    }

    [HttpDelete("enrolments/{enrolmentId:int}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Delete(int enrolmentId)
    {
        var participantId = User.GetUserId();
        if (participantId is null) return Forbid();

        var enrolment = await db.EventEnrolment.SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound();
        if (enrolment.ParticipantId != participantId.Value) return Forbid();
        if (await db.Result.AnyAsync(result => result.EnrolmentId == enrolmentId))
            return Conflict(new { message = "An enrolment with a result cannot be cancelled." });

        db.EventEnrolment.Remove(enrolment);
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

    private async Task<bool> CanViewEnrolment(EventEnrolment enrolment)
    {
        var userId = User.GetUserId();
        if (userId is null) return false;
        if (enrolment.ParticipantId == userId.Value) return true;
        if (!User.IsInRole("Organiser")) return false;
        return await db.Event.AnyAsync(item =>
            item.EventId == enrolment.EventId && item.OrganiserId == userId.Value);
    }

    private static EnrolmentDto ToDto(EventEnrolment enrolment) => new()
    {
        EnrolmentId = enrolment.EnrolmentId,
        EventId = enrolment.EventId,
        CategoryId = enrolment.CategoryId,
        ParticipantId = enrolment.ParticipantId,
        EnrolmentDate = enrolment.EnrolmentDate,
        PaymentStatus = enrolment.PaymentStatus,
        BibNumber = enrolment.BibNumber,
        IsConfirmed = enrolment.IsConfirmed
    };
}
