using RaceDay.Api.Models;

namespace RaceDay.Api.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
    }
}
