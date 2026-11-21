using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace KamraApp.Api.Auth;

// t1_minispec K1: behind the Caddy proxy every connection comes from the proxy, so the per-IP auth limit would be
// shared by all clients. X-Forwarded-For is accepted only from the configured proxy network, one hop deep; without
// the setting (local, CI, e2e) nothing is trusted and nothing changes (api.md: a client could forge the header).
public static class TrustedProxySetup
{
    public const string KnownNetworksKey = "ForwardedHeaders:KnownNetworks";

    public static IServiceCollection AddTrustedProxy(this IServiceCollection services, IConfiguration configuration)
    {
        var networks = ParseNetworks(configuration[KnownNetworksKey]);
        services.AddOptions<ForwardedHeadersOptions>()
            .Configure(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
                options.ForwardLimit = 1;
                // The defaults trust loopback (F2); only the configured networks count.
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
                foreach (var network in networks ?? [])
                {
                    options.KnownIPNetworks.Add(network);
                }
            })
            .Validate(_ => networks is not null, $"{KnownNetworksKey} must be a comma-separated list of networks in CIDR notation, for example 172.30.0.0/24.")
            .ValidateOnStart();
        return services;
    }

    // Before every other middleware, so the rate limiter and the request log see the client's address.
    // Only with at least one network: with both trust lists empty the middleware trusts every proxy (V-105).
    public static WebApplication UseTrustedProxy(this WebApplication app)
    {
        if (ParseNetworks(app.Configuration[KnownNetworksKey]) is { Count: > 0 })
        {
            app.UseForwardedHeaders();
        }

        return app;
    }

    // Null means invalid (the app does not start); an empty list means switched off.
    private static List<IPNetwork>? ParseNetworks(string? value)
    {
        var networks = new List<IPNetwork>();
        foreach (var part in (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!IPNetwork.TryParse(part, out var network))
            {
                return null;
            }

            networks.Add(network);
        }

        return networks;
    }
}
