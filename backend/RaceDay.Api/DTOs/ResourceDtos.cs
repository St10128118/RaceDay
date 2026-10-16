using System.ComponentModel.DataAnnotations;
using RaceDay.Api.DTOs.Events;

namespace RaceDay.Api.DTOs;

public sealed class UserProfileDto
{
    public int UserId { get; init; }
    public string Role { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? Phone { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? IdNumber { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? OrganisationName { get; init; }
}

public sealed class UserUpdateRequest
{
    [StringLength(100)]
    public string? FirstName { get; init; }
    [StringLength(100)]
    public string? LastName { get; init; }
    [StringLength(50)]
    public string? Phone { get; init; }
    [StringLength(200)]
    public string? EmergencyContactName { get; init; }
    [StringLength(50)]
    public string? EmergencyContactPhone { get; init; }
    [StringLength(200)]
    public string? OrganisationName { get; init; }
}

public sealed class EventCategoryDto
{
    public int CategoryId { get; init; }
    public int EventId { get; init; }
    public string Name { get; init; } = null!;
    public decimal? DistanceKm { get; init; }
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
}

public sealed class CategoryCreateRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = null!;
    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? DistanceKm { get; init; }
    [Range(0, 130)]
    public int? MinAge { get; init; }
    [Range(0, 130)]
    public int? MaxAge { get; init; }
}

public sealed class CategoryUpdateRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }
    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? DistanceKm { get; init; }
    [Range(0, 130)]
    public int? MinAge { get; init; }
    [Range(0, 130)]
    public int? MaxAge { get; init; }
}

public sealed class EnrolmentDto
{
    public int EnrolmentId { get; init; }
    public int EventId { get; init; }
    public int CategoryId { get; init; }
    public int ParticipantId { get; init; }
    public DateTime EnrolmentDate { get; init; }
    public string PaymentStatus { get; init; } = null!;
    public string? BibNumber { get; init; }
    public bool IsConfirmed { get; init; }
}

public sealed class EnrolmentCreateRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
}

public sealed class EnrolmentUpdateRequest
{
    [StringLength(50)]
    public string? PaymentStatus { get; init; }
    [StringLength(50)]
    public string? BibNumber { get; init; }
}

public sealed class ResultDto
{
    public int ResultId { get; init; }
    public int EnrolmentId { get; init; }
    public int? FinishTimeSeconds { get; init; }
    public int? PositionOverall { get; init; }
    public int? PositionCategory { get; init; }
    public int? PositionGender { get; init; }
    public string Status { get; init; } = null!;
}

public sealed class ResultCreateRequest
{
    [Range(0, int.MaxValue)]
    public int? FinishTimeSeconds { get; init; }
    [Range(1, int.MaxValue)]
    public int? PositionOverall { get; init; }
    [Range(1, int.MaxValue)]
    public int? PositionCategory { get; init; }
    [Range(1, int.MaxValue)]
    public int? PositionGender { get; init; }
    [Required, StringLength(50, MinimumLength = 1)]
    public string Status { get; init; } = null!;
}

public sealed class ResultUpdateRequest
{
    [Range(0, int.MaxValue)]
    public int? FinishTimeSeconds { get; init; }
    [Range(1, int.MaxValue)]
    public int? PositionOverall { get; init; }
    [Range(1, int.MaxValue)]
    public int? PositionCategory { get; init; }
    [Range(1, int.MaxValue)]
    public int? PositionGender { get; init; }
    [StringLength(50, MinimumLength = 1)]
    public string? Status { get; init; }
}

public sealed class EventRouteDto
{
    public int RouteId { get; init; }
    public int EventId { get; init; }
    public string Name { get; init; } = null!;
    public decimal DistanceKm { get; init; }
    public string? GPXUrl { get; init; }
    public int? ElevationGainMeters { get; init; }
}

public sealed class RouteCreateRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = null!;
    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKm { get; init; }
    [StringLength(500)]
    public string? GPXUrl { get; init; }
    [Range(0, int.MaxValue)]
    public int? ElevationGainMeters { get; init; }
}

public sealed class RouteUpdateRequest
{
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; init; }
    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? DistanceKm { get; init; }
    [StringLength(500)]
    public string? GPXUrl { get; init; }
    [Range(0, int.MaxValue)]
    public int? ElevationGainMeters { get; init; }
}

public sealed class ParticipantHistoryDto
{
    public EventDto Event { get; init; } = null!;
    public EnrolmentDto Enrolment { get; init; } = null!;
    public ResultDto? Result { get; init; }
}
