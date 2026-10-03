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
        // The exception handler clears response headers; the correlation id must survive it (ADR-0011).
        JsonDocument.Parse(body).RootElement.GetProperty("correlationId").GetString()
            .Should().Be(response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task Framework_bad_request_exception_keeps_its_4xx_status_without_details()
    {
        var response = await _client.GetAsync("/api/v1/test-errors/bad-request", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Code(body).Should().Be("REQUEST_REJECTED");
        body.Should().NotContain("hunter2");
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
        Title(body).Should().Be("A kért oldal vagy adat nem található.");
    }

    [Fact]
    public async Task Wrong_http_method_returns_405_problem_details()
    {
        var response = await _client.PostAsync("/api/v1/test-errors/conflict", content: null, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        Code(body).Should().Be("METHOD_NOT_ALLOWED");
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
        response.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
    }

    private static string? Code(string body) =>
        JsonDocument.Parse(body).RootElement.GetProperty("code").GetString();

    private static string? Title(string body) =>
        JsonDocument.Parse(body).RootElement.GetProperty("title").GetString();
}
