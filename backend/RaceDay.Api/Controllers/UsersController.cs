using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.DTOs.Events;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var userId = User.GetUserId();
        if (userId is null) return Forbid();

        var profile = await db.User.AsNoTracking()
            .Where(user => user.UserId == userId.Value)
            .Select(user => new UserProfileDto
            {
                UserId = user.UserId,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                IdNumber = user.IdNumber,
                EmergencyContactName = user.EmergencyContactName,
                EmergencyContactPhone = user.EmergencyContactPhone,
                OrganisationName = user.OrganisationName
            })
            .SingleOrDefaultAsync();

        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UserUpdateRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Forbid();

        var user = await db.User.FindAsync(userId.Value);
        if (user is null) return NotFound();

        if (request.FirstName is not null) user.FirstName = request.FirstName.Trim();
        if (request.LastName is not null) user.LastName = request.LastName.Trim();
        if (request.Phone is not null) user.Phone = request.Phone;
        if (request.EmergencyContactName is not null) user.EmergencyContactName = request.EmergencyContactName;
        if (request.EmergencyContactPhone is not null) user.EmergencyContactPhone = request.EmergencyContactPhone;
        if (request.OrganisationName is not null) user.OrganisationName = request.OrganisationName;

        await db.SaveChangesAsync();
        return await GetMe();
    }

    [HttpGet("me/history")]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<IEnumerable<ParticipantHistoryDto>>> GetHistory()
    {
        var userId = User.GetUserId();
        if (userId is null) return Forbid();

        var enrolments = await db.EventEnrolment.AsNoTracking()
            .Where(enrolment => enrolment.ParticipantId == userId.Value)
            .Include(enrolment => enrolment.Event)
            .Include(enrolment => enrolment.Result)
            .OrderByDescending(enrolment => enrolment.Event!.EventDateTime)
            .ToListAsync();

        var history = enrolments.Select(enrolment => new ParticipantHistoryDto
        {
            Event = new EventDto
            {
                EventId = enrolment.Event!.EventId,
                OrganiserId = enrolment.Event.OrganiserId,
                Name = enrolment.Event.Name,
                Description = enrolment.Event.Description,
                EventType = enrolment.Event.EventType,
                Location = enrolment.Event.Location,
                City = enrolment.Event.City,
                Province = enrolment.Event.Province,
                DistanceKm = enrolment.Event.DistanceKm,
                EventDateTime = enrolment.Event.EventDateTime,
                Status = enrolment.Event.Status,
                MaxParticipants = enrolment.Event.MaxParticipants
            },
            Enrolment = new EnrolmentDto
            {
                EnrolmentId = enrolment.EnrolmentId,
                EventId = enrolment.EventId,
                CategoryId = enrolment.CategoryId,
                ParticipantId = enrolment.ParticipantId,
                EnrolmentDate = enrolment.EnrolmentDate,
                PaymentStatus = enrolment.PaymentStatus,
                BibNumber = enrolment.BibNumber,
                IsConfirmed = enrolment.IsConfirmed
            },
            Result = enrolment.Result is null ? null : new ResultDto
            {
                ResultId = enrolment.Result.ResultId,
                EnrolmentId = enrolment.Result.EnrolmentId,
                FinishTimeSeconds = enrolment.Result.FinishTimeSeconds,
                PositionOverall = enrolment.Result.PositionOverall,
                PositionCategory = enrolment.Result.PositionCategory,
                PositionGender = enrolment.Result.PositionGender,
                Status = enrolment.Result.Status
            }
        });

        return Ok(history);
    }
}
