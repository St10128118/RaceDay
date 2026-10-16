using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceRoute = RaceDay.Api.Models.Route;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class RoutesController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet("events/{eventId:int}/routes")]
    public async Task<ActionResult<IEnumerable<EventRouteDto>>> GetForEvent(int eventId)
    {
        if (!await db.Event.AnyAsync(item => item.EventId == eventId)) return NotFound();
        var routes = await db.Route.AsNoTracking()
            .Where(item => item.EventId == eventId)
            .OrderBy(item => item.RouteId)
            .Select(item => ToDto(item))
            .ToListAsync();
        return Ok(routes);
    }

    [HttpGet("routes/{routeId:int}")]
    public async Task<ActionResult<EventRouteDto>> GetById(int routeId)
    {
        var route = await db.Route.AsNoTracking()
            .SingleOrDefaultAsync(item => item.RouteId == routeId);
        return route is null ? NotFound() : Ok(ToDto(route));
    }

    [HttpPost("events/{eventId:int}/routes")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<EventRouteDto>> Create(int eventId, RouteCreateRequest request)
    {
        var ownership = await CheckEventOwnership(eventId);
        if (ownership is not null) return ownership;

        var route = new RaceRoute
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            DistanceKm = request.DistanceKm,
            GPXUrl = request.GPXUrl,
            ElevationGainMeters = request.ElevationGainMeters
        };
        db.Route.Add(route);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { routeId = route.RouteId }, ToDto(route));
    }

    [HttpPut("routes/{routeId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<EventRouteDto>> Update(int routeId, RouteUpdateRequest request)
    {
        var route = await db.Route.SingleOrDefaultAsync(item => item.RouteId == routeId);
        if (route is null) return NotFound();
        var ownership = await CheckEventOwnership(route.EventId);
        if (ownership is not null) return ownership;

        if (request.Name is not null) route.Name = request.Name.Trim();
        if (request.DistanceKm.HasValue) route.DistanceKm = request.DistanceKm.Value;
        if (request.GPXUrl is not null) route.GPXUrl = request.GPXUrl;
        if (request.ElevationGainMeters.HasValue) route.ElevationGainMeters = request.ElevationGainMeters;
        await db.SaveChangesAsync();
        return Ok(ToDto(route));
    }

    [HttpDelete("routes/{routeId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int routeId)
    {
        var route = await db.Route.SingleOrDefaultAsync(item => item.RouteId == routeId);
        if (route is null) return NotFound();
        var ownership = await CheckEventOwnership(route.EventId);
        if (ownership is not null) return ownership;

        db.Route.Remove(route);
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

    private static EventRouteDto ToDto(RaceRoute route) => new()
    {
        RouteId = route.RouteId,
        EventId = route.EventId,
        Name = route.Name,
        DistanceKm = route.DistanceKm,
        GPXUrl = route.GPXUrl,
        ElevationGainMeters = route.ElevationGainMeters
    };
}
