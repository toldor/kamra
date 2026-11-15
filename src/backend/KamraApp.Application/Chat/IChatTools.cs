namespace KamraApp.Application.Chat;

// The result of one tool call: IsError when the tool refused the arguments or failed (MCP CallToolResult.isError).
public sealed record ChatToolOutcome(bool IsError, string Content);

// ADR-0010 1.: the MCP client side. The tools run on an MCP server in the same process, with the household of the
// signed-in request; no call selects a household.
public interface IChatTools
{
    Task<IReadOnlyList<ChatTool>> ListAsync(CancellationToken cancellationToken);

    Task<ChatToolOutcome> CallAsync(string name, string argumentsJson, CancellationToken cancellationToken);
}
