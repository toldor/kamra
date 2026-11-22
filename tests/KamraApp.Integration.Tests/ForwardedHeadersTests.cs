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

    // " , ": with both trust lists empty the framework would trust every proxy (V-105), so no network = off.
    [Theory]
    [InlineData(null)]
    [InlineData(" , ")]
    public async Task Without_a_configured_proxy_network_the_forwarded_header_is_ignored(string? knownNetworks)
    {
        var client = await ClientAsync(knownNetworks);
        await UseUpTheLimitAsync(client, Proxy, "203.0.113.1");

        (await LoginAsync(client, Proxy, "203.0.113.2")).Should().Be(HttpStatusCode.TooManyRequests);
    }

    // 127.0.0.1: the framework trusts loopback by default (F2); only the configured network may count.
    [Theory]
    [InlineData("198.51.100.7")]
    [InlineData("127.0.0.1")]
    public async Task A_forwarded_header_from_outside_the_proxy_network_is_ignored(string remote)
    {
        var client = await ClientAsync(ProxyNetwork);
        await UseUpTheLimitAsync(client, remote, "203.0.113.1");

        (await LoginAsync(client, remote, "203.0.113.2")).Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task The_proxy_is_trusted_when_its_address_arrives_as_an_ipv4_mapped_ipv6_address()
    {
        var client = await ClientAsync(ProxyNetwork);
        await UseUpTheLimitAsync(client, "::ffff:10.0.0.5", "203.0.113.1");

        (await LoginAsync(client, "::ffff:10.0.0.5", "203.0.113.2")).Should().Be(HttpStatusCode.Unauthorized);
    }

    // Security review hardening: behind Caddy the API sees plain HTTP, so without the forwarded scheme the antiforgery
    // cookie (SecurePolicy.SameAsRequest, V-12) would go out without the Secure flag on the public instance.
    [Fact]
    public async Task The_antiforgery_cookie_is_secure_when_the_trusted_proxy_forwards_https()
    {
        (await AntiforgeryCookieAsync(ProxyNetwork, Proxy)).Should().Contain("secure");
    }

    [Theory]
    [InlineData(null, Proxy)]
    [InlineData(ProxyNetwork, "198.51.100.7")]
    public async Task A_forwarded_https_is_ignored_without_the_trusted_proxy(string? knownNetworks, string remote)
    {
        (await AntiforgeryCookieAsync(knownNetworks, remote)).Should().NotContain("secure");
    }

    // Plain http, as Caddy talks to the API; the cookie header in lower case.
    private async Task<string> AntiforgeryCookieAsync(string? knownNetworks, string remote)
    {
        var http = App(knownNetworks).CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        http.DefaultRequestHeaders.Add(TestRemoteAddress.Header, remote);
        http.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        using var response = await http.GetAsync("/api/v1/auth/antiforgery", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return response.Headers.GetValues("Set-Cookie").Single(cookie => cookie.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal))
            .ToLowerInvariant();
    }

    private Task<ApiClient> ClientAsync(string? knownNetworks) => ApiClient.CreateAsync(App(knownNetworks));

    private WebApplicationFactory<Program> App(string? knownNetworks) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "3");
            if (knownNetworks is not null)
            {
                builder.UseSetting("ForwardedHeaders:KnownNetworks", knownNetworks);
            }

            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, TestRemoteAddress>());
        });

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
}
