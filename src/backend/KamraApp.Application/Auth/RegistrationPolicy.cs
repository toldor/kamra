using System.Security.Cryptography;
using System.Text;

namespace KamraApp.Application.Auth;

// t1_minispec K2: on the public instance registration needs a static invite code (Auth:InviteCode);
// without one (local, CI, e2e) registration is open, as before.
public sealed record RegistrationPolicy(string? InviteCode)
{
    public static readonly RegistrationPolicy Open = new((string?)null);

    public bool InviteCodeRequired => !string.IsNullOrEmpty(InviteCode);

    // Constant time for equal lengths, so a wrong guess does not reveal how much of it was right.
    public bool Accepts(string? inviteCode) =>
        !InviteCodeRequired || CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(inviteCode ?? ""), Encoding.UTF8.GetBytes(InviteCode!));
}
