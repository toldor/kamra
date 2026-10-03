using KamraApp.Application.Common;
using KamraApp.Domain.Households;

namespace KamraApp.Application.Auth;

// US auth (ADR-0006): one account = one household, created together, then the user is signed in.
public sealed class RegisterUser(IIdentityService identity, TimeProvider time)
{
    public async Task ExecuteAsync(RegisterRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var household = Household.Create(ownerUserId: Guid.CreateVersion7(), time.GetUtcNow());

        await identity.RegisterAndSignInAsync(valid.Email!, valid.Password!, household, cancellationToken);
    }
}
