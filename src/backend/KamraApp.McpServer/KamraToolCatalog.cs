using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ModelContextProtocol.Server;

namespace KamraApp.McpServer;

// The MCP tools of one KamraTools instance (one request), with the API's JSON conventions: enums as camelCase
// names ("dkg", "dairy"), numbers only as JSON numbers, so the schema and the results use the REST API's codes.
public static class KamraToolCatalog
{
    private static readonly JsonSerializerOptions Json = CreateJson();

    public static IReadOnlyList<McpServerTool> Create(KamraTools target) =>
        [.. typeof(KamraTools).GetMethods()
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .Select(method => McpServerTool.Create(method, target, new McpServerToolCreateOptions { SerializerOptions = Json }))];

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            NumberHandling = JsonNumberHandling.Strict,
            // us6_minispec K5: a missing value is left out (a makeable recipe has no shortages, a list row may have no amount).
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.MakeReadOnly();
        return options;
    }
}
