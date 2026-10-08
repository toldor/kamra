using KamraApp.Application.Common;

namespace KamraApp.Api.Auth;

// ADR-0006: the household id comes only from the encrypted, signed authentication cookie's claim.
public sealed class HttpCurrentHousehold(IHttpContextAccessor accessor) : ICurrentHousehold
{
    public Guid HouseholdId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(KamraClaimTypes.HouseholdId)?.Value, out var id)
            ? id
            : throw new UnauthorizedException(AuthenticationSetup.UnauthenticatedCode, AuthenticationSetup.UnauthenticatedTitle);
}
