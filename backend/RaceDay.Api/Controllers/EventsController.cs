using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Events;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/v1/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        private readonly ILogger<EventsController> _logger;

        public EventsController(RaceDayDbContext db, ILogger<EventsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /api/v1/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetAll(
            [FromQuery] string? city,
            [FromQuery] string? province,
            [FromQuery] string? eventType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            if (from.HasValue && to.HasValue && from > to)
                return BadRequest(new { message = "'from' must not be later than 'to'." });

            var query = _db.Event.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(city)) query = query.Where(e => e.City.Contains(city));
            if (!string.IsNullOrWhiteSpace(province)) query = query.Where(e => e.Province.Contains(province));
            if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType == eventType);
            if (from.HasValue) query = query.Where(e => e.EventDateTime >= from.Value);
            if (to.HasValue) query = query.Where(e => e.EventDateTime <= to.Value);

            var events = await query
                .AsNoTracking()
                .OrderBy(e => e.EventDateTime)
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    OrganiserId = e.OrganiserId,
                    Name = e.Name,
                    Description = e.Description,
                    EventType = e.EventType,
                    Location = e.Location,
                    City = e.City,
                    Province = e.Province,
                    DistanceKm = e.DistanceKm,
                    EventDateTime = e.EventDateTime,
                    Status = e.Status,
                    MaxParticipants = e.MaxParticipants
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: /api/v1/events/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EventDto>> GetById(int id)
        {
            var e = await _db.Event
                .AsNoTracking()
                .Where(x => x.EventId == id)
                .Select(x => new EventDto
                {
                    EventId = x.EventId,
                    OrganiserId = x.OrganiserId,
                    Name = x.Name,
                    Description = x.Description,
                    EventType = x.EventType,
                    Location = x.Location,
                    City = x.City,
                    Province = x.Province,
                    DistanceKm = x.DistanceKm,
                    EventDateTime = x.EventDateTime,
                    Status = x.Status,
                    MaxParticipants = x.MaxParticipants
                })
                .SingleOrDefaultAsync();

            if (e == null) return NotFound();
            return Ok(e);
        }

        // POST: /api/v1/events
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult> Create(EventCreateDto dto)
        {
            var userId = User.GetUserId();
            if (userId is null) return Forbid();

            var ev = new Event
            {
                OrganiserId = userId.Value,
                Name = dto.Name,
                Description = dto.Description,
                EventType = dto.EventType,
                Location = dto.Location,
                City = dto.City,
                Province = dto.Province,
                DistanceKm = dto.DistanceKm,
                EventDateTime = dto.EventDateTime,
                Status = dto.Status,
                MaxParticipants = dto.MaxParticipants
            };

            _db.Event.Add(ev);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToDto(ev));
        }

        // PUT: /api/v1/events/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult> Update(int id, EventUpdateDto dto)
        {
            var ev = await _db.Event.FindAsync(id);
            if (ev == null) return NotFound();

            var userId = User.GetUserId();
            if (userId is null) return Forbid();

            if (ev.OrganiserId != userId.Value) return Forbid();

            // Apply updates if provided
            if (dto.Name is not null) ev.Name = dto.Name.Trim();
            if (dto.Description is not null) ev.Description = dto.Description;
            if (dto.EventType is not null) ev.EventType = dto.EventType.Trim();
            if (dto.Location is not null) ev.Location = dto.Location.Trim();
            if (dto.City is not null) ev.City = dto.City.Trim();
            if (dto.Province is not null) ev.Province = dto.Province.Trim();
            if (dto.DistanceKm.HasValue) ev.DistanceKm = dto.DistanceKm.Value;
            if (dto.EventDateTime.HasValue) ev.EventDateTime = dto.EventDateTime.Value;
            if (dto.Status is not null) ev.Status = dto.Status.Trim();
            if (dto.MaxParticipants.HasValue) ev.MaxParticipants = dto.MaxParticipants;

            await _db.SaveChangesAsync();
            return Ok(ToDto(ev));
        }

        // DELETE: /api/v1/events/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult> Delete(int id)
        {
            var ev = await _db.Event.FindAsync(id);
            if (ev == null) return NotFound();

            var userId = User.GetUserId();
            if (userId is null) return Forbid();

            if (ev.OrganiserId != userId.Value) return Forbid();

            if (await _db.EventEnrolment.AnyAsync(enrolment => enrolment.EventId == id))
                return Conflict(new { message = "An event with enrolments cannot be deleted." });

            _db.Route.RemoveRange(await _db.Route.Where(route => route.EventId == id).ToListAsync());
            _db.EventCategory.RemoveRange(await _db.EventCategory.Where(category => category.EventId == id).ToListAsync());
            _db.Event.Remove(ev);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static EventDto ToDto(Event ev) => new()
        {
            EventId = ev.EventId,
            OrganiserId = ev.OrganiserId,
            Name = ev.Name,
            Description = ev.Description,
            EventType = ev.EventType,
            Location = ev.Location,
            City = ev.City,
            Province = ev.Province,
            DistanceKm = ev.DistanceKm,
            EventDateTime = ev.EventDateTime,
            Status = ev.Status,
            MaxParticipants = ev.MaxParticipants
        };
    }
}
