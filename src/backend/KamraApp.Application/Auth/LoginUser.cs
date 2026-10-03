using KamraApp.Application.Common;

namespace KamraApp.Application.Auth;

public sealed class LoginUser(IIdentityService identity)
{
    public async Task ExecuteAsync(LoginRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);

        var outcome = await identity.PasswordSignInAsync(valid.Email!.Trim(), valid.Password!, cancellationToken);

        switch (outcome)
        {
            case SignInOutcome.Succeeded:
                return;
            // ux_flows H4: one message for a wrong e-mail and a wrong password.
            case SignInOutcome.InvalidCredentials:
                throw new UnauthorizedException("INVALID_CREDENTIALS", "Hibás e-mail-cím vagy jelszó.");
            case SignInOutcome.LockedOut:
                throw new RateLimitedException("LOGIN_LOCKED_OUT", "Túl sok sikertelen próbálkozás. Várj 5 percet, és próbáld újra.");
            default:
                throw new ArgumentOutOfRangeException(nameof(request), outcome, "Unknown sign-in outcome.");
        }
    }
}
