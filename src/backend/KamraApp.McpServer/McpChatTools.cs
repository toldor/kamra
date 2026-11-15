using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using KamraApp.Application.Chat;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SdkMcpServer = ModelContextProtocol.Server.McpServer;

namespace KamraApp.McpServer;

// ADR-0010 1.: per request, an MCP server with the tools of this request's scope and an MCP client connected to it
// over an in-memory stream pair. The tools get their use cases, and so the signed-in household (ADR-0010 2.), from
// this request's services; nothing goes over the network. Disposed (both ends closed) with the request scope.
public sealed class McpChatTools(IServiceProvider services) : IChatTools, IAsyncDisposable
{
    private readonly ToolFailures _failures = new();
    private Connection? _connection;
    private bool _disposed;

    public async Task<IReadOnlyList<ChatTool>> ListAsync(CancellationToken cancellationToken)
    {
        var client = await ClientAsync(cancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        return [.. tools.Select(t => new ChatTool(t.Name, t.Description, t.JsonSchema.GetRawText()))];
    }

    public async Task<ChatToolOutcome> CallAsync(string name, string argumentsJson, CancellationToken cancellationToken)
    {
        Dictionary<string, object?>? arguments;
        try
        {
            arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);
        }
        catch (JsonException)
        {
            return new ChatToolOutcome(true, "The arguments are not a JSON object.");
        }

        var client = await ClientAsync(cancellationToken);
        CallToolResult result;
        try
        {
            result = await client.CallToolAsync(name, arguments, cancellationToken: cancellationToken);
        }
        catch (McpException)
        {
            // An unknown tool or a protocol-level refusal: the same as a refused call.
            return new ChatToolOutcome(true, $"The tool '{name}' cannot be called.");
        }

        // T1 review J3: the tool failed on its own (not on its arguments): the app's error, not the model's.
        if (result.IsError == true && _failures.Take() is { } failure)
        {
            ExceptionDispatchInfo.Throw(failure);
        }

        return new ChatToolOutcome(result.IsError == true, string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text)));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_connection is { } connection)
        {
            await connection.DisposeAsync();
        }
    }

    private async Task<McpClient> ClientAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_connection is not null)
        {
            return _connection.Client;
        }

        var toolCollection = new McpServerPrimitiveCollection<McpServerTool>();
        foreach (var tool in KamraToolCatalog.Create(ActivatorUtilities.CreateInstance<KamraTools>(services, _failures)))
        {
            toolCollection.Add(tool);
        }

        var options = new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "kamra", Version = "1.0" },
            ToolCollection = toolCollection,
            // The tools already hold this request's use cases; a new scope would lose the signed-in household.
            ScopeRequests = false,
        };
        // T1 review J4: the connection is kept only when both ends are up; a failed start is closed at once.
        var connection = new Connection();
        try
        {
            connection.Server = SdkMcpServer.Create(
                new StreamServerTransport(connection.ToServer.Reader.AsStream(), connection.ToClient.Writer.AsStream()), options, null, services);
            connection.ServerRun = connection.Server.RunAsync(connection.Stop.Token);
            connection.Client = await McpClient.CreateAsync(
                new StreamClientTransport(connection.ToServer.Writer.AsStream(), connection.ToClient.Reader.AsStream()),
                cancellationToken: cancellationToken);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        _connection = connection;
        return connection.Client;
    }

    // Both ends of one in-memory MCP connection.
    private sealed class Connection : IAsyncDisposable
    {
        public Pipe ToServer { get; } = new();

        public Pipe ToClient { get; } = new();

        public CancellationTokenSource Stop { get; } = new();

        public SdkMcpServer? Server { get; set; }

        public Task? ServerRun { get; set; }

        public McpClient Client { get; set; } = null!;

        // Closing never fails the request: the server stops with it, whatever state it is in.
        public async ValueTask DisposeAsync()
        {
            if (Client is not null)
            {
                await Client.DisposeAsync();
            }

            await Stop.CancelAsync();
            await ToServer.Writer.CompleteAsync();
            await ToClient.Writer.CompleteAsync();
            if (ServerRun is not null)
            {
                try
                {
                    await ServerRun;
                }
                catch (Exception error) when (error is OperationCanceledException or IOException or InvalidOperationException)
                {
                    // The server stops with the request.
                }
            }

            if (Server is not null)
            {
                await Server.DisposeAsync();
            }

            Stop.Dispose();
        }
    }
}
