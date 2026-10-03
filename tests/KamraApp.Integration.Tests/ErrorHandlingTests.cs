using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KamraApp.Integration.Tests;

public class ErrorHandlingTests(KamraApiFactory factory) : IClassFixture<KamraApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_200()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unexpected_exception_returns_500_without_internal_details()
    {
        var response = await _client.GetAsync("/api/v1/test-errors/unexpected", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        Code(body).Should().Be("INTERNAL_ERROR");
        body.Should().NotContain("hunter2").And.NotContain("InvalidOperationException").And.NotContain(" at ");
    }

    [Fact]
    public async Task App_exception_returns_its_category_status_code_and_title()
    {
        var response = await _client.GetAsync("/api/v1/test-errors/conflict", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().Should().Be("TEST_CONFLICT");
        problem.GetProperty("title").GetString().Should().Be("Teszt ütközés.");
    }

    [Fact]
    public async Task Unknown_api_route_returns_404_problem_details()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        Code(body).Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task Error_response_correlation_id_matches_header_and_csp_is_set()
    {
        var response = await _client.GetAsync("/api/v1/test-errors/conflict", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        var headerId = response.Headers.GetValues("X-Correlation-Id").Single();
        headerId.Should().NotBeNullOrWhiteSpace();
        problem.GetProperty("correlationId").GetString().Should().Be(headerId);
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Be("default-src 'self'");
    }

    private static string? Code(string body) =>
        JsonDocument.Parse(body).RootElement.GetProperty("code").GetString();
}
