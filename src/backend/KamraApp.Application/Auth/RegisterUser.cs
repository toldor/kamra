using KamraApp.Application.Common;
using KamraApp.Domain.Households;

namespace KamraApp.Application.Auth;

// US auth (ADR-0006): one account = one household, created together, then the user is signed in.
public sealed class RegisterUser(IIdentityService identity, TimeProvider time)
{
    public async Task ExecuteAsync(RegisterRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var userId = Guid.CreateVersion7();
        var household = Household.Create(userId, time.GetUtcNow());

        await identity.RegisterAndSignInAsync(userId, valid.Email!.Trim(), valid.Password!, household, cancellationToken);
    }
}
