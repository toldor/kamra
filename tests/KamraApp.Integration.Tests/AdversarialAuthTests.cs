using System.Net;
using System.Text.Json;

namespace KamraApp.Integration.Tests;

// Adversarial tests written by Antigravity (Gemini 3.1 Pro) against the S3 auth module
// (docs/07_ai/review_prompts.md, template 2). Integrated verbatim apart from compile fixes and the
// approved K2 change: the enumeration test ignores the per-request trace and correlation ids.
[Collection("api")]
public class AdversarialAuthTests(KamraApiFactory factory)
{
    [Fact]
    public async Task Reusing_a_cookie_after_logout_is_rejected()
    {
        var client = await ApiClient.CreateAsync(factory);
        var registerResponse = await client.RegisterAsync(ApiClient.NewEmail());

        var setCookieHeader = registerResponse.Headers.GetValues("Set-Cookie").First();
        var cookieValue = setCookieHeader.Split(';').First();

        await client.PostAsync("/api/v1/auth/logout", new { });

        var attackerClient = await ApiClient.CreateAsync(factory);
        attackerClient.Http.DefaultRequestHeaders.Add("Cookie", cookieValue);

        var response = await attackerClient.MeAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Using_antiforgery_token_from_another_session_is_rejected()
    {
        var victimClient = await ApiClient.CreateAsync(factory);
        await victimClient.RegisterAsync(ApiClient.NewEmail());

        var attackerClient = await ApiClient.CreateAsync(factory);
        var attackerToken = attackerClient.Http.DefaultRequestHeaders.GetValues("X-XSRF-TOKEN").First();

        victimClient.Http.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        victimClient.Http.DefaultRequestHeaders.Add("X-XSRF-TOKEN", attackerToken);

        var response = await victimClient.PostAsync("/api/v1/auth/logout", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("ANTIFORGERY_TOKEN_INVALID");
    }

    [Fact]
    public async Task Antiforgery_token_from_before_login_cannot_be_used_after_login()
    {
        var client = await ApiClient.CreateAsync(factory);
        var preLoginToken = client.Http.DefaultRequestHeaders.GetValues("X-XSRF-TOKEN").First();

        await client.RegisterAsync(ApiClient.NewEmail());

        client.Http.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.Http.DefaultRequestHeaders.Add("X-XSRF-TOKEN", preLoginToken);

        var response = await client.PostAsync("/api/v1/auth/logout", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("ANTIFORGERY_TOKEN_INVALID");
    }

    [Fact]
    public async Task Extreme_passwords_and_emails_are_validated_correctly()
    {
        var client = await ApiClient.CreateAsync(factory);
        var tooShort = new string('a', 14);
        var minValid = new string('a', 15);
        var maxValid = new string('a', 128);
        var tooLong = new string('a', 129);
        var unicodePass = "  árvíztűrő_TÜKÖRFÚRÓGÉP 😈  ";
        var veryLongEmail = new string('a', 255) + "@example.com";
        var spacedEmail = "  test@example.com  ";

        (await client.RegisterAsync(ApiClient.NewEmail(), tooShort)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.RegisterAsync(ApiClient.NewEmail(), tooLong)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await client.RegisterAsync(ApiClient.NewEmail(), minValid)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.RegisterAsync(ApiClient.NewEmail(), maxValid)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.RegisterAsync(ApiClient.NewEmail(), unicodePass)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var longEmailResponse = await client.RegisterAsync(veryLongEmail, minValid);
        longEmailResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.BadRequest);

        var spacedEmailResponse = await client.RegisterAsync(spacedEmail, minValid);
        spacedEmailResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Enumeration_via_response_body_is_prevented()
    {
        var client = await ApiClient.CreateAsync(factory);
        var email = ApiClient.NewEmail();
        await client.RegisterAsync(email);

        var wrongPasswordResponse = await client.LoginAsync(email, "WrongPassword12345");
        var unknownEmailResponse = await client.LoginAsync(ApiClient.NewEmail(), "WrongPassword12345");

        // Per-request identifiers must differ; every other field must be identical (K2 adjustment).
        var wrongPasswordBody = WithoutRequestIds(await ApiClient.ProblemAsync(wrongPasswordResponse));
        var unknownEmailBody = WithoutRequestIds(await ApiClient.ProblemAsync(unknownEmailResponse));

        wrongPasswordResponse.StatusCode.Should().Be(unknownEmailResponse.StatusCode);
        wrongPasswordBody.Should().Equal(unknownEmailBody);
    }

    [Fact]
    public async Task Lockout_threshold_is_enforced_under_concurrent_requests_and_is_case_insensitive()
    {
        var email = ApiClient.NewEmail();
        var client = await ApiClient.CreateAsync(factory);
        await client.RegisterAsync(email);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => client.LoginAsync(email, "WrongPassword12345"))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized).Should().BeLessOrEqualTo(5);
        responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests).Should().BeGreaterOrEqualTo(5);

        var upperCaseResponse = await client.LoginAsync(email.ToUpperInvariant(), "WrongPassword12345");
        upperCaseResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await ApiClient.ProblemAsync(upperCaseResponse)).GetProperty("code").GetString().Should().Be("LOGIN_LOCKED_OUT");
    }

    [Fact]
    public async Task Concurrent_registration_with_same_email_prevents_duplicates_gracefully()
    {
        var email = ApiClient.NewEmail();
        var client = await ApiClient.CreateAsync(factory);
        var password = "ValidPassword123456";

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.RegisterAsync(email, password))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        responses.Count(r => r.StatusCode == HttpStatusCode.NoContent).Should().Be(1);
        responses.Where(r => r.StatusCode != HttpStatusCode.NoContent)
                 .Should().AllSatisfy(r => r.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Malformed_json_body_does_not_leak_internal_errors()
    {
        var client = await ApiClient.CreateAsync(factory);
        var content = new StringContent("{ \"email\": \"test@examp", System.Text.Encoding.UTF8, "application/json");

        var response = await client.Http.PostAsync("/api/v1/auth/register", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        body.Should().NotContain("StackTrace");
        body.Should().NotContain("Exception");
        body.Should().NotContain("Npgsql");
        body.Should().NotContain("KamraApp");

        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static Dictionary<string, string> WithoutRequestIds(JsonElement problem) =>
        problem.EnumerateObject()
            .Where(p => p.Name is not ("traceId" or "correlationId"))
            .ToDictionary(p => p.Name, p => p.Value.GetRawText());
}
