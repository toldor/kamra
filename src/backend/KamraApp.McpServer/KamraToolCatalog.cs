using ModelContextProtocol.Server;

namespace KamraApp.McpServer;

// The MCP tools of one KamraTools instance (one request), with the API's JSON conventions: enums as camelCase
// names ("dkg", "dairy"), so the schema and the results use the same codes as the REST API.
public static class KamraToolCatalog
{
    public static IReadOnlyList<McpServerTool> Create(KamraTools target) =>
        throw new NotImplementedException();
}
