using KamraApp.Application.Common;
using KamraApp.Domain.Households;

namespace KamraApp.Application.Auth;

// US auth (ADR-0006): one account = one household, created together, then the user is signed in.
public sealed class RegisterUser(IIdentityService identity, TimeProvider time, RegistrationPolicy policy)
{
    public const string InviteCodeMessage = "Hibás vagy hiányzó meghívókód. Kérd el attól, aki a címet adta.";

    public async Task ExecuteAsync(RegisterRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        // t1_minispec K2: one message for a missing and a wrong code, checked before any account exists.
        if (!policy.Accepts(valid.InviteCode))
        {
            throw ValidationException.ForField("inviteCode", InviteCodeMessage);
        }

        var household = Household.Create(ownerUserId: Guid.CreateVersion7(), time.GetUtcNow());

        await identity.RegisterAndSignInAsync(valid.Email!, valid.Password!, household, cancellationToken);
    }
}
