using KamraApp.McpServer;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Unit.Tests;

// T1 review J4: the per-request MCP connection is closed with the request scope; closing it must never fail the
// request, whether it was used or not, and closing it twice is harmless.
public class McpChatToolsTests
{
    [Fact]
    public async Task An_unused_connection_can_be_closed_twice()
    {
        var tools = new McpChatTools(new ServiceCollection().BuildServiceProvider());

        await tools.DisposeAsync();
        await FluentActions.Awaiting(() => tools.DisposeAsync().AsTask()).Should().NotThrowAsync();
    }
}
