using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// t1_minispec K1: behind the Caddy proxy every request comes from the proxy's address, so the per-IP auth limit
// may only use X-Forwarded-For when the connection comes from the configured proxy network.
[Collection("api")]
public class ForwardedHeadersTests(KamraApiFactory factory)
{
    private const string ProxyNetwork = "10.0.0.0/8";
    private const string Proxy = "10.0.0.5";
    private const string WrongPassword = "a wrong but long password";

    [Fact]
    public async Task Clients_behind_the_trusted_proxy_get_their_own_auth_limit()
    {
        var client = await ClientAsync(ProxyNetwork);
        await UseUpTheLimitAsync(client, Proxy, "203.0.113.1");

        (await LoginAsync(client, Proxy, "203.0.113.1")).Should().Be(HttpStatusCode.TooManyRequests);
        (await LoginAsync(client, Proxy, "203.0.113.2")).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Without_a_configured_proxy_network_the_forwarded_header_is_ignored()
    {
        var client = await ClientAsync(knownNetworks: null);
        await UseUpTheLimitAsync(client, Proxy, "203.0.113.1");

        (await LoginAsync(client, Proxy, "203.0.113.2")).Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task A_forwarded_header_from_outside_the_proxy_network_is_ignored()
    {
        var client = await ClientAsync(ProxyNetwork);
        await UseUpTheLimitAsync(client, "198.51.100.7", "203.0.113.1");

        (await LoginAsync(client, "198.51.100.7", "203.0.113.2")).Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task The_proxy_is_trusted_when_its_address_arrives_as_an_ipv4_mapped_ipv6_address()
    {
        var client = await ClientAsync(ProxyNetwork);
        await UseUpTheLimitAsync(client, "::ffff:10.0.0.5", "203.0.113.1");

        (await LoginAsync(client, "::ffff:10.0.0.5", "203.0.113.2")).Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<ApiClient> ClientAsync(string? knownNetworks)
    {
        var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "3");
            if (knownNetworks is not null)
            {
                builder.UseSetting("ForwardedHeaders:KnownNetworks", knownNetworks);
            }

            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, TestRemoteAddress>());
        });
        return await ApiClient.CreateAsync(app);
    }

    private static async Task UseUpTheLimitAsync(ApiClient client, string remote, string forwardedFor)
    {
        for (var i = 0; i < 3; i++)
        {
            (await LoginAsync(client, remote, forwardedFor)).Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    private static async Task<HttpStatusCode> LoginAsync(ApiClient client, string remote, string forwardedFor)
    {
        client.Http.DefaultRequestHeaders.Remove(TestRemoteAddress.Header);
        client.Http.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.Http.DefaultRequestHeaders.Add(TestRemoteAddress.Header, remote);
        client.Http.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        using var response = await client.LoginAsync(ApiClient.NewEmail(), WrongPassword);
        return response.StatusCode;
    }

    // The TestServer has no real connection; this sets the connection address before the app's own pipeline,
    // so the test decides it, not a TestServer default (Antigravity review A-01).
    private sealed class TestRemoteAddress : IStartupFilter
    {
        public const string Header = "X-Test-Remote-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(Header, out var address))
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(address.ToString());
                }

                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
