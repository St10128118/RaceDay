using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Auth
{
    public class LoginRequest
    {
        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
}
