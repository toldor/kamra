using KamraApp.Domain.Households;

namespace KamraApp.Application.Auth;

// ADR-0006: ASP.NET Core Identity stays behind this port (implemented in Infrastructure).
public interface IIdentityService
{
    // Creates the user, the household and the household claim in one transaction, then signs in.
    // Throws ConflictException (EMAIL_ALREADY_REGISTERED) when the e-mail is taken.
    Task RegisterAndSignInAsync(Guid userId, string email, string password, Household household, CancellationToken cancellationToken);

    Task<SignInOutcome> PasswordSignInAsync(string email, string password, CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);
}

public enum SignInOutcome
{
    Succeeded,
    InvalidCredentials,
    LockedOut,
}
