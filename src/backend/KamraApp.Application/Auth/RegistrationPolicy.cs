using System.Security.Cryptography;
using System.Text;

namespace KamraApp.Application.Auth;

// t1_minispec K2: on the public instance registration needs a static invite code (Auth:InviteCode);
// without one (local, CI, e2e) registration is open, as before.
public sealed record RegistrationPolicy(string? InviteCode)
{
    public static readonly RegistrationPolicy Open = new((string?)null);

    public bool InviteCodeRequired => !string.IsNullOrEmpty(InviteCode);

    // Constant time over equal-length hashes, so a wrong guess reveals neither how much of it was right nor the
    // configured code's length (Antigravity review #3).
    public bool Accepts(string? inviteCode) =>
        !InviteCodeRequired || CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(inviteCode ?? "")), SHA256.HashData(Encoding.UTF8.GetBytes(InviteCode!)));
}
