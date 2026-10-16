using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string FirstName { get; set; } = null!;
        [Required, StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = null!;
        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = null!;
        [Required, MinLength(12)]
        public string Password { get; set; } = null!;
        [Required, RegularExpression("^(Organiser|Participant)$")]
        public string Role { get; set; } = null!;
        [StringLength(200)]
        public string? OrganisationName { get; set; }
    }
}
