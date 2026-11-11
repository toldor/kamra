using System.IO.Pipelines;
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
    private readonly Pipe _toServer = new();
    private readonly Pipe _toClient = new();
    private readonly CancellationTokenSource _stop = new();
    private McpClient? _client;
    private SdkMcpServer? _server;
    private Task? _serverRun;

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
        try
        {
            var result = await client.CallToolAsync(name, arguments, cancellationToken: cancellationToken);
            return new ChatToolOutcome(result.IsError == true, string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text)));
        }
        catch (McpException)
        {
            // An unknown tool or a protocol-level refusal: the same as a refused call.
            return new ChatToolOutcome(true, $"The tool '{name}' cannot be called.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        await _stop.CancelAsync();
        await _toServer.Writer.CompleteAsync();
        await _toClient.Writer.CompleteAsync();
        if (_serverRun is not null)
        {
            try
            {
                await _serverRun;
            }
            catch (OperationCanceledException)
            {
                // The server stops with the request.
            }
        }

        if (_server is not null)
        {
            await _server.DisposeAsync();
        }

        _stop.Dispose();
    }

    private async Task<McpClient> ClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            return _client;
        }

        var toolCollection = new McpServerPrimitiveCollection<McpServerTool>();
        foreach (var tool in KamraToolCatalog.Create(ActivatorUtilities.CreateInstance<KamraTools>(services)))
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
        _server = SdkMcpServer.Create(new StreamServerTransport(_toServer.Reader.AsStream(), _toClient.Writer.AsStream()), options, null, services);
        _serverRun = _server.RunAsync(_stop.Token);
        _client = await McpClient.CreateAsync(new StreamClientTransport(_toServer.Writer.AsStream(), _toClient.Reader.AsStream()),
            cancellationToken: cancellationToken);
        return _client;
    }
}
