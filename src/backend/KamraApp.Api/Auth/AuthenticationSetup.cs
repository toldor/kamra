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
    public const string ItemProposalsRateLimitPolicy = "item-proposals";
    public const string ChatRateLimitPolicy = "chat";
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
        // The antiforgery cookie keeps the default SecurePolicy (SameAsRequest): with Always, ASP.NET
        // refuses to issue tokens on plain HTTP, which breaks the HTTP-only local deployment (V-12).
        services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            options.Filters.Add<AntiforgeryProblemFilter>(AntiforgeryProblemFilter.FilterOrder);
        });

        // ADR-0006: per-IP limit on login and registration (the v1.2 rate-limiting bonus item).
        var permitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60));
        // us2_minispec K11: sentences per household (from the session, not the IP), against LLM cost and abuse.
        var proposalLimit = configuration.GetValue("RateLimiting:ItemProposals:PermitLimit", 20);
        var proposalWindow = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:ItemProposals:WindowSeconds", 600));
        // us6_minispec K9: chat questions per household; one question is at most 3 model calls.
        var chatLimit = configuration.GetValue("RateLimiting:Chat:PermitLimit", 30);
        var chatWindow = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Chat:WindowSeconds", 600));
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window }));
            options.AddPolicy(ItemProposalsRateLimitPolicy, context => PerHousehold(context, proposalLimit, proposalWindow));
            options.AddPolicy(ChatRateLimitPolicy, context => PerHousehold(context, chatLimit, chatWindow));
            options.OnRejected = (context, _) => new ValueTask(AppExceptionHandler.WriteProblemAsync(
                context.HttpContext, StatusCodes.Status429TooManyRequests, "RATE_LIMITED", "Túl sok kérés érkezett. Várj egy kicsit, és próbáld újra."));
        });

        return services;
    }

    // Per household (from the session, not the IP). Without a session there is no household: no limit here, the
    // authorization filter answers 401 (ADR-0007).
    private static RateLimitPartition<string> PerHousehold(HttpContext context, int permitLimit, TimeSpan window) =>
        context.User.FindFirst(KamraClaimTypes.HouseholdId)?.Value is { } householdId
            ? RateLimitPartition.GetFixedWindowLimiter(householdId,
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window })
            : RateLimitPartition.GetNoLimiter("anonymous");
}
