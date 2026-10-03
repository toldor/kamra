using System.Security.Claims;
using KamraApp.Application.Auth;
using KamraApp.Application.Common;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KamraApp.Api.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationSetup.AuthRateLimitPolicy)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest? request, [FromServices] RegisterUser registerUser, CancellationToken cancellationToken)
    {
        await registerUser.ExecuteAsync(request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationSetup.AuthRateLimitPolicy)]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest? request, [FromServices] LoginUser loginUser, CancellationToken cancellationToken)
    {
        await loginUser.ExecuteAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromServices] IIdentityService identity, CancellationToken cancellationToken)
    {
        await identity.SignOutAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    public MeResponse Me([FromServices] ICurrentHousehold household) =>
        new(User.FindFirstValue(ClaimTypes.Email) ?? "", household.HouseholdId);

    // The token is bound to the current user: the SPA fetches a new one after login, registration and logout.
    [AllowAnonymous]
    [HttpGet("antiforgery")]
    public AntiforgeryTokenResponse Antiforgery([FromServices] IAntiforgery antiforgery) =>
        new(antiforgery.GetAndStoreTokens(HttpContext).RequestToken ?? "");
}

public sealed record MeResponse(string Email, Guid HouseholdId);

public sealed record AntiforgeryTokenResponse(string RequestToken);
