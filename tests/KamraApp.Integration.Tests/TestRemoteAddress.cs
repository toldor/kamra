using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace KamraApp.Integration.Tests;

// The TestServer has no real connection; this sets the connection address before the app's own pipeline,
// so the test decides it, not a TestServer default (Antigravity review A-01).
internal sealed class TestRemoteAddress : IStartupFilter
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
