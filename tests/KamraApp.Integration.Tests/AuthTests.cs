using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// ADR-0006 Verification list, against a real PostgreSQL database.
[Collection("api")]
public class AuthTests(KamraApiFactory factory)
{
    private const string WrongPassword = "a wrong but long password";

    [Fact]
    public async Task Me_without_cookie_returns_401()
    {
        var client = await ApiClient.CreateAsync(factory);

        var response = await client.MeAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Forged_auth_cookie_returns_401_not_500()
    {
        var client = await ApiClient.CreateAsync(factory);
        client.Http.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Identity.Application=CfDJ8forged-value-that-is-not-encrypted");

        var response = await client.MeAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected_and_creates_nothing()
    {
        var client = await ApiClient.CreateAsync(factory);
        client.Http.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        var email = ApiClient.NewEmail();

        var response = await client.RegisterAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("ANTIFORGERY_TOKEN_INVALID");
        await client.RefreshAntiforgeryTokenAsync();
        (await client.RegisterAsync(email)).StatusCode.Should().Be(HttpStatusCode.NoContent, "the rejected request must not have created the account");
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_identical_responses()
    {
        var email = ApiClient.NewEmail();
        await (await ApiClient.CreateAsync(factory)).RegisterAsync(email);

        var wrongPassword = await (await ApiClient.CreateAsync(factory)).LoginAsync(email, WrongPassword);
        var unknownEmail = await (await ApiClient.CreateAsync(factory)).LoginAsync(ApiClient.NewEmail(), WrongPassword);

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(wrongPassword.StatusCode);
        var first = await ApiClient.ProblemAsync(wrongPassword);
        var second = await ApiClient.ProblemAsync(unknownEmail);
        first.GetProperty("code").GetString().Should().Be("INVALID_CREDENTIALS");
        second.GetProperty("code").GetString().Should().Be(first.GetProperty("code").GetString());
        second.GetProperty("title").GetString().Should().Be(first.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Account_is_locked_by_the_fifth_failed_login_even_for_the_right_password()
    {
        var email = ApiClient.NewEmail();
        await (await ApiClient.CreateAsync(factory)).RegisterAsync(email);
        var attacker = await ApiClient.CreateAsync(factory);
        for (var i = 0; i < 4; i++)
        {
            (await attacker.LoginAsync(email, WrongPassword)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Identity locks the account on the 5th failure (MaxFailedAccessAttempts = 5).
        var fifth = await attacker.LoginAsync(email, WrongPassword);
        var rightPassword = await attacker.LoginAsync(email);

        fifth.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await ApiClient.ProblemAsync(fifth)).GetProperty("code").GetString().Should().Be("LOGIN_LOCKED_OUT");
        rightPassword.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await ApiClient.ProblemAsync(rightPassword)).GetProperty("code").GetString().Should().Be("LOGIN_LOCKED_OUT");
    }

    [Fact]
    public async Task Too_many_auth_requests_from_one_address_return_429()
    {
        using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:PermitLimit", "3"));
        var client = await ApiClient.CreateAsync(limited);
        for (var i = 0; i < 3; i++)
        {
            await client.LoginAsync(ApiClient.NewEmail(), WrongPassword);
        }

        var response = await client.LoginAsync(ApiClient.NewEmail(), WrongPassword);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("RATE_LIMITED");
    }

    [Fact]
    public async Task Password_shorter_than_15_characters_is_rejected_with_a_hungarian_field_error()
    {
        var client = await ApiClient.CreateAsync(factory);

        var response = await client.RegisterAsync(ApiClient.NewEmail(), "fourteen chars");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").GetProperty("password")[0].GetString().Should().StartWith("A jelszó legalább 15");
    }

    [Fact]
    public async Task Registration_creates_the_household_and_signs_the_user_in()
    {
        var client = await ApiClient.CreateAsync(factory);
        var email = ApiClient.NewEmail();

        (await client.RegisterAsync(email)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var me = await client.Http.GetFromJsonAsync<JsonElement>("/api/v1/auth/me", TestContext.Current.CancellationToken);

        me.GetProperty("email").GetString().Should().Be(email);
        var householdId = me.GetProperty("householdId").GetGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email, TestContext.Current.CancellationToken);
        var household = await db.Households.SingleAsync(h => h.Id == householdId, TestContext.Current.CancellationToken);
        household.OwnerUserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Registering_a_taken_email_in_different_case_returns_409()
    {
        var email = ApiClient.NewEmail();
        await (await ApiClient.CreateAsync(factory)).RegisterAsync(email);

        var response = await (await ApiClient.CreateAsync(factory)).RegisterAsync(email.ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("EMAIL_ALREADY_REGISTERED");
    }

    [Fact]
    public async Task Login_signs_in_and_logout_signs_out()
    {
        var email = ApiClient.NewEmail();
        await (await ApiClient.CreateAsync(factory)).RegisterAsync(email);
        var client = await ApiClient.CreateAsync(factory);

        (await client.LoginAsync(email)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.MeAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync("/api/v1/auth/logout")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.MeAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
