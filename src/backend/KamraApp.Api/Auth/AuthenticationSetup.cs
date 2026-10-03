using System.Threading.RateLimiting;
using KamraApp.Api.ErrorHandling;
using KamraApp.Application.Auth;
using KamraApp.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace KamraApp.Api.Auth;

public static class AuthenticationSetup
{
    public const string AuthRateLimitPolicy = "auth";
    public const string UnauthenticatedCode = "UNAUTHENTICATED";
    public const string UnauthenticatedTitle = "Jelentkezz be a folytatáshoz.";

    public static IServiceCollection AddKamraAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<RegisterUser>();
        services.AddScoped<LoginUser>();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentHousehold, HttpCurrentHousehold>();

        // ADR-0006: HttpOnly, Secure, SameSite=Strict session cookie, 14 days sliding. An API never
        // redirects to a login page: it answers 401/403 ProblemDetails.
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(cookie =>
        {
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            cookie.Cookie.SameSite = SameSiteMode.Strict;
            cookie.ExpireTimeSpan = TimeSpan.FromDays(14);
            cookie.SlidingExpiration = true;
            cookie.Events.OnRedirectToLogin = context => AppExceptionHandler.WriteProblemAsync(
                context.HttpContext, StatusCodes.Status401Unauthorized, UnauthenticatedCode, UnauthenticatedTitle);
            cookie.Events.OnRedirectToAccessDenied = context => AppExceptionHandler.WriteProblemAsync(
                context.HttpContext, StatusCodes.Status403Forbidden, "FORBIDDEN", "Ehhez nincs jogosultságod.");
        });

        // Secure by default: every controller action requires a signed-in user unless marked
        // [AllowAnonymous]. A global filter (not a fallback policy) keeps unknown routes at 404.
        // ADR-0006 / ADR-0007: one global antiforgery filter; the SPA sends the token in a header.
        services.AddAuthorization();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            options.Filters.Add<AntiforgeryProblemFilter>(AntiforgeryProblemFilter.FilterOrder);
        });

        // ADR-0006: per-IP limit on login and registration (the v1.2 rate-limiting bonus item).
        var permitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60));
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window }));
            options.OnRejected = (context, _) => new ValueTask(AppExceptionHandler.WriteProblemAsync(
                context.HttpContext, StatusCodes.Status429TooManyRequests, "RATE_LIMITED", "Túl sok kérés érkezett. Várj egy kicsit, és próbáld újra."));
        });

        return services;
    }
}
