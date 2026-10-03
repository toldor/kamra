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
}
