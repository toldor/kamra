using System.Text.Json;
using KamraApp.McpServer;
using ModelContextProtocol.Server;

namespace KamraApp.Unit.Tests;

// US-6 (mcp_tools.md, us6_minispec K4, ADR-0010 2.): the MCP tools as the model sees them. Exactly the four
// read-only tools; no argument can name a household; the category uses the REST API's codes.
public class KamraToolsTests
{
    private static readonly IReadOnlyList<McpServerTool> Tools = KamraToolCatalog.Create(new KamraTools(null!, null!, null!, new ToolFailures()));

    [Fact]
    public void The_server_has_exactly_the_four_tools_of_mcp_tools_md()
    {
        Tools.Select(t => t.ProtocolTool.Name).Should().BeEquivalentTo(
            "get_pantry_items", "get_expiring_items", "get_recommendations", "get_shopping_list");
        Tools.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.ProtocolTool.Description));
    }

    [Fact]
    public void Every_tool_is_marked_read_only_and_not_destructive()
    {
        Tools.Should().OnlyContain(t => t.ProtocolTool.Annotations!.ReadOnlyHint == true && t.ProtocolTool.Annotations.DestructiveHint == false);
    }

    [Fact]
    public void No_tool_takes_a_household_or_any_id()
    {
        foreach (var tool in Tools)
        {
            Properties(tool).Should().NotContain(name => name.Contains("household", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("id", StringComparison.OrdinalIgnoreCase), tool.ProtocolTool.Name);
        }
    }

    [Fact]
    public void The_pantry_tool_takes_an_optional_category_with_the_API_codes()
    {
        var tool = Tool("get_pantry_items");

        Properties(tool).Should().Equal("category");
        Required(tool).Should().BeEmpty();
        var schema = tool.ProtocolTool.InputSchema.GetRawText();
        schema.Should().Contain("\"dairy\"").And.Contain("\"freshMeatFish\"").And.Contain("\"other\"")
            .And.NotContain("\"Dairy\"", "the REST API's codes are camelCase");
    }

    [Fact]
    public void The_recommendation_tool_takes_an_optional_integer_max_results()
    {
        var tool = Tool("get_recommendations");

        Properties(tool).Should().Equal("max_results");
        Required(tool).Should().BeEmpty();
        tool.ProtocolTool.InputSchema.GetProperty("properties").GetProperty("max_results").GetRawText().Should().Contain("integer");
    }

    // T1 review J6 (us6_minispec K4, mcp_tools.md "JSON-sémával validált"): the range is in the schema, not only in code.
    [Fact]
    public void The_schema_limits_max_results_to_1_to_10()
    {
        var maxResults = Tool("get_recommendations").ProtocolTool.InputSchema.GetProperty("properties").GetProperty("max_results");

        maxResults.GetProperty("minimum").GetInt32().Should().Be(1);
        maxResults.GetProperty("maximum").GetInt32().Should().Be(10);
    }

    [Theory]
    [InlineData("get_expiring_items")]
    [InlineData("get_shopping_list")]
    public void The_other_tools_take_no_arguments(string name)
    {
        Properties(Tool(name)).Should().BeEmpty();
    }

    private static McpServerTool Tool(string name) => Tools.Single(t => t.ProtocolTool.Name == name);

    private static string[] Properties(McpServerTool tool) =>
        tool.ProtocolTool.InputSchema.TryGetProperty("properties", out var properties)
            ? [.. properties.EnumerateObject().Select(p => p.Name)]
            : [];

    private static string[] Required(McpServerTool tool) =>
        tool.ProtocolTool.InputSchema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array
            ? [.. required.EnumerateArray().Select(r => r.GetString()!)]
            : [];
}
