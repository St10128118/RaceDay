using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

internal static class ControllerUserExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
