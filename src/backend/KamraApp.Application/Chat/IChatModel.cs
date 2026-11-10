namespace KamraApp.Application.Chat;

// What the model sees of one question (ADR-0010 3.-5.): the earlier messages as text, the question, and within
// the question the model's own steps exactly as received (thought signatures included) and the tool results.
public abstract record ChatTurn;

public sealed record UserTurn(string Text) : ChatTurn;

public sealed record AssistantTurn(string Text) : ChatTurn;

// The model's reply, opaque to the Application: sent back unchanged in the next call of the same question.
public sealed record ModelStepTurn(string RawContent) : ChatTurn;

public sealed record ToolResultsTurn(IReadOnlyList<ToolResult> Results) : ChatTurn;

public sealed record ToolResult(string? CallId, string Name, string Content);

// A tool the model may call: name, description and JSON input schema, as the MCP server lists them.
public sealed record ChatTool(string Name, string Description, string InputSchemaJson);

public sealed record FunctionCall(string? Id, string Name, string ArgumentsJson);

// Either a text answer or function calls; RawContent is the reply as received.
public sealed record ChatModelReply(string? Text, IReadOnlyList<FunctionCall> Calls, string RawContent);

// AGENTS.md 5.: the LLM sits behind this port, so tests never call it. Enabled is the Llm:Enabled switch
// (us6_minispec K8). The adapter owns the system prompt (chat.v1) and puts today's Budapest date into it (K7).
// CompleteAsync throws LLM_UNAVAILABLE (timeout or outage after one retry, refused request) or
// LLM_INVALID_RESPONSE (an answer outside the expected shape).
public interface IChatModel
{
    bool Enabled { get; }

    Task<ChatModelReply> CompleteAsync(DateOnly today, IReadOnlyList<ChatTurn> turns, IReadOnlyList<ChatTool> tools,
        CancellationToken cancellationToken);
}
