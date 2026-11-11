using KamraApp.Application.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KamraApp.Infrastructure.Llm;

// US-6 (ADR-0010 3.-4., us6_minispec K6-K7): Gemini generateContent with function calling over plain HTTP (no SDK).
// The MCP tools become function declarations (parametersJsonSchema); the model's step goes back unchanged; the
// versioned prompt chat.v1 gets today's Budapest date. The US-2 retry rules (QA-3) and a log without any text.
public sealed partial class GeminiChatModel(HttpClient http, IOptions<LlmOptions> options, ILogger<GeminiChatModel> logger) : IChatModel
{
    public const string PromptVersion = "chat.v1";

    public bool Enabled => true;

    public Task<ChatModelReply> CompleteAsync(DateOnly today, IReadOnlyList<ChatTurn> turns, IReadOnlyList<ChatTool> tools,
        CancellationToken cancellationToken)
    {
        _ = (http, options, logger); // red stage (spec-first): the T2 implementation uses them
        throw new NotImplementedException();
    }
}
