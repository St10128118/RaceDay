using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ResultsController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet("events/{eventId:int}/results")]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetForEvent(int eventId)
    {
        if (!await db.Event.AnyAsync(item => item.EventId == eventId)) return NotFound();
        var results = await db.Result.AsNoTracking()
            .Where(result => result.Enrolment!.EventId == eventId)
            .OrderBy(result => result.PositionOverall)
            .Select(result => ToDto(result))
            .ToListAsync();
        return Ok(results);
    }

    [HttpGet("enrolments/{enrolmentId:int}/result")]
    [Authorize]
    public async Task<ActionResult<ResultDto>> GetForEnrolment(int enrolmentId)
    {
        var enrolment = await db.EventEnrolment.AsNoTracking()
            .SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound();
        if (!await CanViewEnrolment(enrolment)) return Forbid();

        var result = await db.Result.AsNoTracking()
            .SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId);
        return result is null ? NotFound() : Ok(ToDto(result));
    }

    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<ResultDto>> Create(int enrolmentId, ResultCreateRequest request)
    {
        var enrolment = await db.EventEnrolment.SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound();
        var ownership = await CheckEventOwnership(enrolment.EventId);
        if (ownership is not null) return ownership;
        if (await db.Result.AnyAsync(item => item.EnrolmentId == enrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTimeSeconds = request.FinishTimeSeconds,
            PositionOverall = request.PositionOverall,
            PositionCategory = request.PositionCategory,
            PositionGender = request.PositionGender,
            Status = request.Status.Trim()
        };
        db.Result.Add(result);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetForEnrolment), new { enrolmentId }, ToDto(result));
    }

    [HttpPut("results/{resultId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<ResultDto>> Update(int resultId, ResultUpdateRequest request)
    {
        var result = await db.Result.Include(item => item.Enrolment)
            .SingleOrDefaultAsync(item => item.ResultId == resultId);
        if (result is null) return NotFound();
        var ownership = await CheckEventOwnership(result.Enrolment!.EventId);
        if (ownership is not null) return ownership;

        if (request.FinishTimeSeconds.HasValue) result.FinishTimeSeconds = request.FinishTimeSeconds;
        if (request.PositionOverall.HasValue) result.PositionOverall = request.PositionOverall;
        if (request.PositionCategory.HasValue) result.PositionCategory = request.PositionCategory;
        if (request.PositionGender.HasValue) result.PositionGender = request.PositionGender;
        if (request.Status is not null) result.Status = request.Status.Trim();
        await db.SaveChangesAsync();
        return Ok(ToDto(result));
    }

    [HttpDelete("results/{resultId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int resultId)
    {
        var result = await db.Result.Include(item => item.Enrolment)
            .SingleOrDefaultAsync(item => item.ResultId == resultId);
        if (result is null) return NotFound();
        var ownership = await CheckEventOwnership(result.Enrolment!.EventId);
        if (ownership is not null) return ownership;

        db.Result.Remove(result);
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

    private static ResultDto ToDto(Result result) => new()
    {
        ResultId = result.ResultId,
        EnrolmentId = result.EnrolmentId,
        FinishTimeSeconds = result.FinishTimeSeconds,
        PositionOverall = result.PositionOverall,
        PositionCategory = result.PositionCategory,
        PositionGender = result.PositionGender,
        Status = result.Status
    };
}
