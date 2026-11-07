using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace KamraApp.Integration.Tests;

public class StartupTests
{
    // QA-7: missing required configuration stops the app at startup (fail fast).
    [Fact]
    public void App_does_not_start_without_a_connection_string()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Default", ""));

        var start = () => factory.CreateClient();

        start.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("ConnectionStrings:Default");
    }

    // us2_minispec K10: the quick entry switched on without an API key stops the app at startup, not at the first sentence.
    [Fact]
    public void App_does_not_start_with_the_LLM_on_and_no_api_key()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", "Host=127.0.0.1;Port=1;Database=none;Username=none");
                builder.UseSetting("Llm:Enabled", "true");
                builder.UseSetting("Llm:ApiKey", "");
            });

        var start = () => factory.CreateClient();

        start.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Llm:ApiKey");
    }

    // The local Docker Compose deployment is plain HTTP; the antiforgery token must still be issued (V-12).
    [Fact]
    public async Task Antiforgery_token_is_issued_over_plain_http()
    {
        await using var factory = new KamraApiFactory();
        await factory.InitializeAsync();

        var response = await factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") })
            .GetAsync("/api/v1/auth/antiforgery", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // The health check really queries the database: with no reachable database it reports 503.
    [Fact]
    public async Task Health_returns_503_when_the_database_is_unreachable()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting(
                "ConnectionStrings:Default", "Host=127.0.0.1;Port=1;Database=none;Username=none;Timeout=2"));

        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
