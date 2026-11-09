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
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetForEvent(int eventId)
    {
        var ownership = await CheckEventOwnership(eventId);
        if (ownership is not null) return ownership;
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
        if (!await IsEventCompleted(enrolment.EventId))
            return Conflict(new { message = "Mark the event Completed before recording results." });
        if (await db.Result.AnyAsync(item => item.EnrolmentId == enrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment." });
        if (!IsSupportedStatus(request.Status))
            return BadRequest(new { message = "Status must be Finished, DNF, DNS, or DSQ." });
        if (string.Equals(request.Status, "Finished", StringComparison.OrdinalIgnoreCase) &&
            (!request.FinishTimeSeconds.HasValue || !request.PositionOverall.HasValue))
            return BadRequest(new { message = "Finished results require a finish time and overall position." });
        var normalizedStatus = NormalizeStatus(request.Status);

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTimeSeconds = normalizedStatus == "Finished"
                ? request.FinishTimeSeconds
                : null,
            PositionOverall = normalizedStatus == "Finished"
                ? request.PositionOverall
                : null,
            PositionCategory = normalizedStatus == "Finished"
                ? request.PositionCategory
                : null,
            PositionGender = normalizedStatus == "Finished"
                ? request.PositionGender
                : null,
            Status = normalizedStatus
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
        if (!await IsEventCompleted(result.Enrolment.EventId))
            return Conflict(new { message = "Results can only be changed after the event is Completed." });

        var status = request.Status ?? result.Status;
        var finishTimeSeconds = request.FinishTimeSeconds ?? result.FinishTimeSeconds;
        var positionOverall = request.PositionOverall ?? result.PositionOverall;
        if (!IsSupportedStatus(status))
            return BadRequest(new { message = "Status must be Finished, DNF, DNS, or DSQ." });
        status = NormalizeStatus(status);
        if (string.Equals(status, "Finished", StringComparison.OrdinalIgnoreCase) &&
            (!finishTimeSeconds.HasValue || !positionOverall.HasValue))
            return BadRequest(new { message = "Finished results require a finish time and overall position." });

        if (request.FinishTimeSeconds.HasValue) result.FinishTimeSeconds = request.FinishTimeSeconds;
        if (request.PositionOverall.HasValue) result.PositionOverall = request.PositionOverall;
        if (request.PositionCategory.HasValue) result.PositionCategory = request.PositionCategory;
        if (request.PositionGender.HasValue) result.PositionGender = request.PositionGender;
        if (request.Status is not null) result.Status = status;
        if (!string.Equals(status, "Finished", StringComparison.OrdinalIgnoreCase))
        {
            result.FinishTimeSeconds = null;
            result.PositionOverall = null;
            result.PositionCategory = null;
            result.PositionGender = null;
        }
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

    private Task<bool> IsEventCompleted(int eventId) =>
        db.Event.AnyAsync(item => item.EventId == eventId && item.Status == "Completed");

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

    private static bool IsSupportedStatus(string status) =>
        status.Equals("Finished", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("DNF", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("DNS", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("DSQ", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeStatus(string status) => status.ToUpperInvariant() switch
    {
        "FINISHED" => "Finished",
        "DNF" => "DNF",
        "DNS" => "DNS",
        "DSQ" => "DSQ",
        _ => status.Trim()
    };
}
