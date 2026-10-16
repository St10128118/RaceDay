namespace RaceDay.Api.DTOs.Auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
    }
}
