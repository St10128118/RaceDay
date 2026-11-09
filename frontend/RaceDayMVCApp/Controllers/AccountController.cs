using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVCApp.Models;
using RaceDayMVCApp.Services;

namespace RaceDayMVCApp.Controllers;

[Route("Account")]
public sealed class AccountController(IRaceDayApiClient apiClient) : Controller
{
    [AllowAnonymous]
    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var login = await apiClient.PostAsync<ApiAuthResponse>("/api/v1/auth/login", new
        {
            model.Email,
            model.Password
        }, cancellationToken);
        if (!login.IsSuccess || string.IsNullOrWhiteSpace(login.Data?.AccessToken))
        {
            ModelState.AddModelError(string.Empty, login.Error ?? "The API did not return a valid sign-in token.");
            return View(model);
        }

        var profile = await apiClient.GetWithTokenAsync<ApiUserProfile>(
            "/api/v1/users/me",
            login.Data.AccessToken,
            cancellationToken);
        if (!profile.IsSuccess || profile.Data is null)
        {
            ModelState.AddModelError(string.Empty, profile.Error ?? "Your profile could not be loaded.");
            return View(model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, profile.Data.UserId.ToString()),
            new Claim(ClaimTypes.Name, $"{profile.Data.FirstName} {profile.Data.LastName}".Trim()),
            new Claim(ClaimTypes.Email, profile.Data.Email),
            new Claim(ClaimTypes.Role, profile.Data.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
        };
        properties.StoreTokens([new AuthenticationToken
        {
            Name = "access_token",
            Value = login.Data.AccessToken
        }]);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        return SafeRedirect(model.ReturnUrl) ?? RedirectToRoleHome();
    }

    [AllowAnonymous]
    [HttpGet("Register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (model.Role is not ("Participant" or "Organiser"))
            ModelState.AddModelError(nameof(model.Role), "Choose Participant or Organiser.");
        if (!ModelState.IsValid) return View(model);

        var registration = await apiClient.PostAsync<object>("/api/v1/auth/register", new
        {
            model.FirstName,
            model.LastName,
            model.Email,
            model.Password,
            model.Role,
            model.OrganisationName
        }, cancellationToken);
        if (!registration.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, registration.Error ?? "Registration could not be completed.");
            return View(model);
        }

        TempData["Success"] = "Your account is ready. Sign in to continue.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Events");
    }

    [AllowAnonymous]
    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToRoleHome() =>
        User.IsInRole("Organiser")
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Participant");

    private IActionResult? SafeRedirect(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : null;
}
