using KamraApp.Application.Chat;

namespace KamraApp.Infrastructure.Llm;

// Llm:Enabled = false (us6_minispec K8): the "Kérdezz" page is hidden, and nothing ever reaches an LLM.
public sealed class DisabledChatModel : IChatModel
{
    public bool Enabled => false;

    public Task<ChatModelReply> CompleteAsync(DateOnly today, IReadOnlyList<ChatTurn> turns, IReadOnlyList<ChatTool> tools,
        CancellationToken cancellationToken) =>
        throw ChatErrors.LlmDisabled();
}
