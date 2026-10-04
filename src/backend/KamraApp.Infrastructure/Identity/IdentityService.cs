using System.Security.Claims;
using KamraApp.Application.Auth;
using KamraApp.Application.Common;
using KamraApp.Domain.Households;
using KamraApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KamraApp.Infrastructure.Identity;

// ADR-0006: password hashing, lockout, e-mail uniqueness and the cookie come from ASP.NET Core
// Identity; this adapter only maps its results to the Application port.
public sealed class IdentityService(KamraDbContext db, UserManager<AppUser> users, SignInManager<AppUser> signIn)
    : IIdentityService
{
    private static ConflictException EmailTaken() =>
        new("EMAIL_ALREADY_REGISTERED", "Ezzel az e-mail-címmel már van fiók. Jelentkezz be, vagy adj meg másik címet.");

    public async Task RegisterAndSignInAsync(string email, string password, Household household, CancellationToken cancellationToken)
    {
        var user = new AppUser { Id = household.OwnerUserId, UserName = email, Email = email };

        // UserManager saves through the same scoped DbContext, so the user, the claim and the
        // household are committed together or not at all.
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                ThrowIfFailed(await users.CreateAsync(user, password));
                ThrowIfFailed(await users.AddClaimAsync(user, new Claim(KamraClaimTypes.HouseholdId, household.Id.ToString())));
                db.Households.Add(household);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two registrations with the same e-mail at the same time: the unique index decides.
                throw EmailTaken();
            }
        }

        await signIn.SignInAsync(user, isPersistent: true);
    }

    public async Task<SignInOutcome> PasswordSignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        // Identity increments the failed-attempt counter with optimistic concurrency, so parallel
        // wrong passwords for one account lose increments and slip past the lockout (V-10). Attempts
        // for the same e-mail are serialized; different accounts run in parallel on other stripes.
        // ponytail: in-process striped locks; a multi-instance deployment would need an atomic
        // counter update in the database instead.
        var loginLock = LoginLocks[(int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(email) % LoginLocks.Length)];
        await loginLock.WaitAsync(cancellationToken);
        try
        {
            // The cookie's security-stamp check may already have loaded this user into the request's
            // DbContext before the lock; a stale tracked copy would make the counter update fail silently.
            db.ChangeTracker.Clear();
            var result = await signIn.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: true);

            return result switch
            {
                { Succeeded: true } => SignInOutcome.Succeeded,
                { IsLockedOut: true } => SignInOutcome.LockedOut,
                _ => SignInOutcome.InvalidCredentials,
            };
        }
        finally
        {
            loginLock.Release();
        }
    }

    // A new security stamp invalidates every cookie issued before the logout, on every device;
    // the cookie is checked against the stamp on each request (V-11).
    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(signIn.Context.User);
        if (user is not null)
        {
            // If the stamp cannot be changed, older cookies would stay valid: fail loudly instead of
            // reporting a logout that did not revoke the other sessions.
            var stamp = await users.UpdateSecurityStampAsync(user);
            if (!stamp.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Security stamp update failed: {string.Join(", ", stamp.Errors.Select(e => e.Code))}");
            }
        }

        await signIn.SignOutAsync();
    }

    // A fixed number of locks keeps memory bounded however many e-mail addresses are tried.
    private static readonly SemaphoreSlim[] LoginLocks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        // Identity error codes are the IdentityErrorDescriber method names.
        var codes = result.Errors.Select(e => e.Code).ToHashSet();
        if (codes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail)) || codes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            throw EmailTaken();
        }

        if (codes.Any(code => code.StartsWith("Password", StringComparison.Ordinal)))
        {
            // Backstop: the use case already validated the length with a Hungarian message.
            throw ValidationException.ForField("password", "A jelszó nem felel meg a szabályoknak: legalább 15 és legfeljebb 128 karakter legyen.");
        }

        if (codes.Any(code => code.Contains("Email", StringComparison.Ordinal) || code.Contains("UserName", StringComparison.Ordinal)))
        {
            throw ValidationException.ForField("email", "Adj meg egy érvényes e-mail-címet, például: nev@pelda.hu.");
        }

        throw new InvalidOperationException($"Identity operation failed: {string.Join(", ", codes)}");
    }
}
