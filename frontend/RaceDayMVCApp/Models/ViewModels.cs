using System.ComponentModel.DataAnnotations;

namespace RaceDayMVCApp.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [Required, MinLength(12), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Participant";

    [StringLength(200)]
    public string? OrganisationName { get; set; }
}

public sealed class EventListViewModel
{
    public IReadOnlyList<ApiEvent> Events { get; init; } = [];
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? EventType { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Error { get; init; }
}

public sealed class EventDetailsViewModel
{
    public ApiEvent Event { get; init; } = new();
    public IReadOnlyList<ApiEventCategory> Categories { get; init; } = [];
    public IReadOnlyList<ApiEventRoute> Routes { get; init; } = [];
    public ApiWeatherForecast? Weather { get; init; }
    public string? Error { get; init; }
}

public sealed class ManageCategoriesViewModel
{
    public ApiEvent Event { get; init; } = new();
    public IReadOnlyList<ApiEventCategory> Categories { get; init; } = [];
    public string? Error { get; init; }
}

public sealed class ManageRoutesViewModel
{
    public ApiEvent Event { get; init; } = new();
    public IReadOnlyList<ApiEventRoute> Routes { get; init; } = [];
    public string? Error { get; init; }
}

public sealed class EventEditorViewModel
{
    public int? EventId { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required]
    public string EventType { get; set; } = "Running";

    [Required, StringLength(200, MinimumLength = 1)]
    public string Location { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    public string Province { get; set; } = "";

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKm { get; set; }

    [Required]
    [Display(Name = "Event date and time (South Africa time)")]
    public DateTime EventDateTime { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Participant limit")]
    public int? MaxParticipants { get; set; }

    public string SubmitLabel => EventId.HasValue ? "Save changes" : "Create event";
}

public sealed class CategoryEditorViewModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 130)]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 130)]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }
}

public sealed class RouteEditorViewModel
{
    public int EventId { get; set; }
    public int? RouteId { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Url, StringLength(500)]
    [Display(Name = "GPX file URL")]
    public string? GPXUrl { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Elevation gain (m)")]
    public int? ElevationGainMeters { get; set; }
}

public sealed class ResultEditorViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";

    [Range(0, int.MaxValue)]
    [Display(Name = "Finish time (seconds)")]
    public int? FinishTimeSeconds { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Overall position")]
    public int? PositionOverall { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Category position")]
    public int? PositionCategory { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Gender position")]
    public int? PositionGender { get; set; }
}

public sealed class ParticipantHistoryViewModel
{
    public IReadOnlyList<ParticipantHistoryItemViewModel> Items { get; init; } = [];
    public bool OnlyResults { get; init; }
    public string? Error { get; init; }
}

public sealed class ParticipantHistoryItemViewModel
{
    public ApiParticipantHistory History { get; init; } = new();
    public string? CategoryName { get; init; }
}

public sealed class ProfileEditViewModel
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Phone, StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(200)]
    [Display(Name = "Emergency contact")]
    public string? EmergencyContactName { get; set; }

    [Phone, StringLength(50)]
    [Display(Name = "Emergency contact phone")]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(200)]
    public string? OrganisationName { get; set; }

    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}

public sealed class OrganiserDashboardViewModel
{
    public IReadOnlyList<OrganiserEventSummaryViewModel> Events { get; init; } = [];
    public string? Error { get; init; }
}

public sealed class OrganiserEventSummaryViewModel
{
    public ApiEvent Event { get; init; } = new();
    public int EnrolmentCount { get; init; }
    public string? CountError { get; init; }
}

public sealed class EnrolmentsViewModel
{
    public ApiEvent Event { get; init; } = new();
    public IReadOnlyList<ApiEnrolment> Enrolments { get; init; } = [];
    public IReadOnlyDictionary<int, ApiResult> Results { get; init; } = new Dictionary<int, ApiResult>();
    public string? Error { get; init; }
}
