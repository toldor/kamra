namespace KamraApp.Application.Chat;

// US-6: one question of the read-only chat. The model may call the MCP tools of the own household; the loop is
// bounded (ADR-0010 4.): at most 3 model calls and at most 4 tool calls per round.
public sealed class AskChat(IChatModel model, IChatTools tools, TimeProvider time)
{
    public const int MaxModelCalls = 3;
    public const int MaxCallsPerRound = 4;

    public Task<AskChatResponse> ExecuteAsync(AskChatRequest? request, CancellationToken cancellationToken)
    {
        _ = (model, tools, time); // red stage (spec-first): the T1 implementation uses them
        throw new NotImplementedException();
    }

    public ChatAvailability Availability() => throw new NotImplementedException();
}
