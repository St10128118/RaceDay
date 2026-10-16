using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Auth;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        private readonly IJwtService _jwt;
        private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _hasher;

        public AuthController(RaceDayDbContext db, IJwtService jwt, Microsoft.AspNetCore.Identity.IPasswordHasher<User> hasher)
        {
            _db = db;
            _jwt = jwt;
            _hasher = hasher;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest req)
        {
            var email = req.Email.Trim().ToLowerInvariant();
            if (await _db.User.AnyAsync(u => u.Email == email))
                return Conflict(new { message = "Email already exists" });

            var user = new User
            {
                FirstName = req.FirstName,
                LastName = req.LastName,
                Email = email,
                Role = req.Role,
                OrganisationName = req.OrganisationName?.Trim()
            };
            user.PasswordHash = _hasher.HashPassword(user, req.Password);

            _db.User.Add(user);
            await _db.SaveChangesAsync();

            return Created("/api/v1/users/me", new UserProfileDto
            {
                UserId = user.UserId,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                OrganisationName = user.OrganisationName
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest req)
        {
            var email = req.Email.Trim().ToLowerInvariant();
            var user = await _db.User.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null) return Unauthorized();

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed) return Unauthorized();
            if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, req.Password);
                await _db.SaveChangesAsync();
            }

            var token = _jwt.GenerateAccessToken(user);
            return Ok(new AuthResponse { AccessToken = token });
        }
    }
}
