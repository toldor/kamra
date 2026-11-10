using KamraApp.Application.Chat;

namespace KamraApp.McpServer;

// ADR-0010 1.: per request, an MCP server with the tools of this request's scope and an MCP client connected to it
// over an in-memory stream pair. The household is the signed-in one of the scope (ADR-0010 2.); nothing goes
// over the network. Disposed (both ends closed) with the request scope.
public sealed class McpChatTools(IServiceProvider services) : IChatTools, IAsyncDisposable
{
    public Task<IReadOnlyList<ChatTool>> ListAsync(CancellationToken cancellationToken)
    {
        _ = services; // red stage (spec-first): the T1 implementation uses it
        throw new NotImplementedException();
    }

    public Task<ChatToolOutcome> CallAsync(string name, string argumentsJson, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
