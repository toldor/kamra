using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KamraApp.Integration.Tests;

// t1_minispec K2: on the public instance registration needs the static invite code; without a configured code
// nothing changes (local, CI, e2e).
[Collection("api")]
public class InviteCodeTests(KamraApiFactory factory)
{
    private const string Code = "demo-invite-code-1234";
    private const string Message = "Hibás vagy hiányzó meghívókód. Kérd el attól, aki a címet adta.";

    [Fact]
    public async Task Registration_without_the_code_is_rejected_with_a_field_error()
    {
        var client = await ApiClient.CreateAsync(WithCode());

        var response = await client.PostAsync("/api/v1/auth/register", new { email = ApiClient.NewEmail(), password = ApiClient.ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").GetProperty("inviteCode")[0].GetString().Should().Be(Message);
    }

    [Fact]
    public async Task Registration_with_a_wrong_code_creates_no_account()
    {
        var app = WithCode();
        var client = await ApiClient.CreateAsync(app);
        var email = ApiClient.NewEmail();

        var response = await client.PostAsync("/api/v1/auth/register", new { email, password = ApiClient.ValidPassword, inviteCode = "demo-invite-code-1235" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("errors").GetProperty("inviteCode")[0].GetString().Should().Be(Message);
        var login = await (await ApiClient.CreateAsync(app)).LoginAsync(email);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ApiClient.ProblemAsync(login)).GetProperty("code").GetString().Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Registration_with_the_right_code_succeeds()
    {
        var client = await ApiClient.CreateAsync(WithCode());

        var response = await client.PostAsync("/api/v1/auth/register", new { email = ApiClient.NewEmail(), password = ApiClient.ValidPassword, inviteCode = Code });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Without_a_configured_code_a_sent_code_is_ignored()
    {
        var client = await ApiClient.CreateAsync(factory);

        var response = await client.PostAsync("/api/v1/auth/register", new { email = ApiClient.NewEmail(), password = ApiClient.ValidPassword, inviteCode = "anything" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task The_registration_endpoint_tells_whether_a_code_is_needed()
    {
        var withCode = await (await ApiClient.CreateAsync(WithCode())).GetJsonAsync("/api/v1/auth/registration");
        var withoutCode = await (await ApiClient.CreateAsync(factory)).GetJsonAsync("/api/v1/auth/registration");

        withCode.GetProperty("inviteCodeRequired").GetBoolean().Should().BeTrue();
        withoutCode.GetProperty("inviteCodeRequired").GetBoolean().Should().BeFalse();
    }

    private WebApplicationFactory<Program> WithCode() =>
        factory.WithWebHostBuilder(builder => builder.UseSetting("Auth:InviteCode", Code));
}
