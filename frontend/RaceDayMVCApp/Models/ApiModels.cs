namespace RaceDayMVCApp.Models;

public sealed class ApiResponse<T>(bool isSuccess, T? data, string? error, int statusCode)
{
    public bool IsSuccess { get; } = isSuccess;
    public T? Data { get; } = data;
    public string? Error { get; } = error;
    public int StatusCode { get; } = statusCode;
}

public sealed class ApiEvent
{
    public int EventId { get; init; }
    public int OrganiserId { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string EventType { get; init; } = "";
    public string Location { get; init; } = "";
    public string City { get; init; } = "";
    public string Province { get; init; } = "";
    public decimal DistanceKm { get; init; }
    public DateTime EventDateTime { get; init; }
    public string Status { get; init; } = "";
    public int? MaxParticipants { get; init; }
}

public sealed class ApiAuthResponse
{
    public string AccessToken { get; init; } = "";
}

public sealed class ApiUserProfile
{
    public int UserId { get; init; }
    public string Role { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? OrganisationName { get; init; }
}

public sealed class ApiEventCategory
{
    public int CategoryId { get; init; }
    public int EventId { get; init; }
    public string Name { get; init; } = "";
    public decimal? DistanceKm { get; init; }
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
}

public sealed class ApiEnrolment
{
    public int EnrolmentId { get; init; }
    public int EventId { get; init; }
    public int CategoryId { get; init; }
    public int ParticipantId { get; init; }
    public DateTime EnrolmentDate { get; init; }
    public string PaymentStatus { get; init; } = "";
    public string? BibNumber { get; init; }
    public bool IsConfirmed { get; init; }
    public string? ParticipantFirstName { get; init; }
    public string? ParticipantLastName { get; init; }
    public string? CategoryName { get; init; }
}

public sealed class ApiResult
{
    public int ResultId { get; init; }
    public int EnrolmentId { get; init; }
    public int? FinishTimeSeconds { get; init; }
    public int? PositionOverall { get; init; }
    public int? PositionCategory { get; init; }
    public int? PositionGender { get; init; }
    public string Status { get; init; } = "";
    public int? TotalFinishers { get; init; }
}

public sealed class ApiEventRoute
{
    public int RouteId { get; init; }
    public int EventId { get; init; }
    public string Name { get; init; } = "";
    public decimal DistanceKm { get; init; }
    public string? GPXUrl { get; init; }
    public int? ElevationGainMeters { get; init; }
}

public sealed class ApiParticipantHistory
{
    public ApiEvent Event { get; init; } = new();
    public ApiEnrolment Enrolment { get; init; } = new();
    public ApiResult? Result { get; init; }
}

public sealed class ApiWeatherForecast
{
    public bool Available { get; init; }
    public string? Message { get; init; }
    public DateOnly? ForecastDate { get; init; }
    public decimal? MinimumTemperatureCelsius { get; init; }
    public decimal? MaximumTemperatureCelsius { get; init; }
    public int? PrecipitationProbabilityPercent { get; init; }
    public decimal? MaximumWindSpeedKph { get; init; }
    public string? Conditions { get; init; }
    public string Provider { get; init; } = "Open-Meteo";
}
