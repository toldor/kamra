using KamraApp.Application.Auth;
using KamraApp.Application.Common;
using KamraApp.Domain.Households;

namespace KamraApp.Unit.Tests;

public class AuthUseCaseTests
{
    private const string ValidPassword = "correct horse battery staple";
    private static readonly DateTimeOffset Now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeIdentityService _identity = new();

    [Fact]
    public async Task Register_rejects_an_invalid_email_with_a_field_error()
    {
        var act = () => new RegisterUser(_identity, new FixedTime(Now), RegistrationPolicy.Open)
            .ExecuteAsync(new RegisterRequest { Email = "not-an-email", Password = ValidPassword }, CancellationToken.None);

        var error = (await act.Should().ThrowAsync<ValidationException>()).Which;
        error.Code.Should().Be("VALIDATION_FAILED");
        error.Errors.Should().ContainKey("email");
        _identity.Registered.Should().BeNull();
    }

    [Fact]
    public async Task Register_rejects_a_password_longer_than_128_characters()
    {
        var act = () => new RegisterUser(_identity, new FixedTime(Now), RegistrationPolicy.Open)
            .ExecuteAsync(new RegisterRequest { Email = "tomi@example.com", Password = new string('a', 129) }, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("password");
    }

    [Fact]
    public async Task Register_rejects_a_missing_body()
    {
        var act = () => new RegisterUser(_identity, new FixedTime(Now), RegistrationPolicy.Open).ExecuteAsync(null, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Register_creates_a_household_owned_by_the_new_user()
    {
        await new RegisterUser(_identity, new FixedTime(Now), RegistrationPolicy.Open)
            .ExecuteAsync(new RegisterRequest { Email = " tomi@example.com ", Password = ValidPassword }, CancellationToken.None);

        var (email, household) = _identity.Registered!.Value;
        household.OwnerUserId.Version.Should().Be(7, "the new user's id is a GUID v7 generated in the use case");
        household.CreatedAt.Should().Be(Now);
        email.Should().Be("tomi@example.com");
    }

    // t1_minispec K2: the invite code (code review Std 2).
    [Fact]
    public async Task Register_without_the_required_invite_code_is_rejected_before_any_account_exists()
    {
        var act = () => new RegisterUser(_identity, new FixedTime(Now), new RegistrationPolicy("demo-invite-code-1234"))
            .ExecuteAsync(new RegisterRequest { Email = "tomi@example.com", Password = ValidPassword }, CancellationToken.None);

        var error = (await act.Should().ThrowAsync<ValidationException>()).Which;
        error.Errors["inviteCode"].Should().Equal(RegisterUser.InviteCodeMessage);
        _identity.Registered.Should().BeNull();
    }

    [Fact]
    public async Task Register_with_the_required_invite_code_creates_the_account()
    {
        await new RegisterUser(_identity, new FixedTime(Now), new RegistrationPolicy("demo-invite-code-1234"))
            .ExecuteAsync(new RegisterRequest { Email = "tomi@example.com", Password = ValidPassword, InviteCode = "demo-invite-code-1234" }, CancellationToken.None);

        _identity.Registered.Should().NotBeNull();
    }

    [Theory]
    [InlineData("demo-invite-code-1235")]
    [InlineData("demo-invite-code-123")]
    [InlineData("demo-invite-code-12345")]
    [InlineData("")]
    [InlineData(null)]
    public void A_required_invite_code_accepts_only_the_exact_code(string? sent)
    {
        new RegistrationPolicy("demo-invite-code-1234").Accepts(sent).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Without_a_configured_code_any_invite_code_is_accepted(string? configured)
    {
        var policy = new RegistrationPolicy(configured);

        policy.InviteCodeRequired.Should().BeFalse();
        policy.Accepts(null).Should().BeTrue();
        policy.Accepts("anything").Should().BeTrue();
    }

    [Fact]
    public async Task Login_maps_invalid_credentials_to_the_uniform_401_code()
    {
        _identity.Outcome = SignInOutcome.InvalidCredentials;

        var act = () => new LoginUser(_identity).ExecuteAsync(new LoginRequest { Email = "tomi@example.com", Password = "wrong" }, CancellationToken.None);

        var error = (await act.Should().ThrowAsync<UnauthorizedException>()).Which;
        error.Code.Should().Be("INVALID_CREDENTIALS");
        error.Message.Should().Be("Hibás e-mail-cím vagy jelszó.");
    }

    [Fact]
    public async Task Login_maps_a_locked_out_account_to_LOGIN_LOCKED_OUT()
    {
        _identity.Outcome = SignInOutcome.LockedOut;

        var act = () => new LoginUser(_identity).ExecuteAsync(new LoginRequest { Email = "tomi@example.com", Password = ValidPassword }, CancellationToken.None);

        (await act.Should().ThrowAsync<RateLimitedException>()).Which.Code.Should().Be("LOGIN_LOCKED_OUT");
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        public (string Email, Household Household)? Registered { get; private set; }

        public SignInOutcome Outcome { get; set; } = SignInOutcome.Succeeded;

        public Task RegisterAndSignInAsync(string email, string password, Household household, CancellationToken cancellationToken)
        {
            Registered = (email, household);
            return Task.CompletedTask;
        }

        public Task<SignInOutcome> PasswordSignInAsync(string email, string password, CancellationToken cancellationToken) =>
            Task.FromResult(Outcome);

        public Task SignOutAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
