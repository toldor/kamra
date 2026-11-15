using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using KamraApp.Application.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KamraApp.Infrastructure.Llm;

// US-6 (ADR-0010 3.-4., us6_minispec K6-K7): Gemini generateContent with function calling over plain HTTP (no SDK).
// The MCP tools become function declarations (parametersJsonSchema takes their JSON Schema as it is); the model's
// step goes back unchanged (thought signatures, https://aistudio.google.com/docs/function-calling); the versioned
// prompt chat.v2 (v1 + plain text only, after the LiveAI run of 2026-11-11; v1 stays as the measured baseline) gets
// today's Budapest date. The US-2 retry rules (QA-3) and a log without any text (K10).
public sealed class GeminiChatModel(HttpClient http, IOptions<LlmOptions> options, ILogger<GeminiChatModel> logger) : IChatModel
{
    public const string PromptVersion = "chat.v2";

    // A short answer of a few sentences; a runaway answer is cut and refused (MAX_TOKENS).
    private const int MaxOutputTokens = 1024;

    private static readonly Lazy<string> Prompt = new(() => GeminiHttp.LoadPrompt("chat.v2.txt"));

    public bool Enabled => true;

    public async Task<ChatModelReply> CompleteAsync(DateOnly today, IReadOnlyList<ChatTurn> turns, IReadOnlyList<ChatTool> tools,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        // K8: Llm:ChatModel, or the quick entry's model when it is not set (Docker Compose passes an unset one as "").
        var model = string.IsNullOrWhiteSpace(settings.ChatModel) ? settings.Model : settings.ChatModel;
        var answer = await GeminiHttp.GenerateAsync(http, settings, model, PromptVersion, Body(today, turns, tools).ToJsonString(), logger,
            ChatErrors.LlmUnavailable, cancellationToken);

        var (reply, tokens) = Read(answer.Body);
        GeminiHttp.Log(logger, model, PromptVersion, answer.Attempt, answer.Started, reply is null ? "invalid" : "ok", tokens);
        return reply ?? throw ChatErrors.LlmInvalidResponse();
    }

    // AGENTS.md 5.: the answer is checked before anything uses it: a finished candidate (STOP) with a content; every
    // function call has a name and an object of arguments. Null reply: outside the expected shape.
    private static (ChatModelReply? Reply, (int Input, int Output)? Tokens) Read(string answer)
    {
        JsonNode? root;
        (int, int)? tokens;
        try
        {
            root = JsonNode.Parse(answer);
            var usage = root?["usageMetadata"];
            tokens = usage is null ? null : ((int?)usage["promptTokenCount"] ?? 0, (int?)usage["candidatesTokenCount"] ?? 0);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return (null, null);
        }

        try
        {
            var candidate = root?["candidates"]?.AsArray().FirstOrDefault();
            if (candidate is null || (string?)candidate["finishReason"] != "STOP" || candidate["content"] is not JsonObject content)
            {
                return (null, tokens);
            }

            var calls = new List<FunctionCall>();
            var text = new StringBuilder();
            foreach (var part in content["parts"]?.AsArray() ?? [])
            {
                if (part?["functionCall"] is JsonObject call)
                {
                    var name = (string?)call["name"];
                    var args = call["args"];
                    if (string.IsNullOrWhiteSpace(name) || (args is not null && args is not JsonObject))
                    {
                        return (null, tokens);
                    }

                    calls.Add(new FunctionCall((string?)call["id"], name, args?.ToJsonString() ?? "{}"));
                }
                else if ((bool?)part?["thought"] != true && (string?)part?["text"] is { } piece)
                {
                    text.Append(piece);
                }
            }

            return (new ChatModelReply(text.Length == 0 ? null : text.ToString(), calls, content.ToJsonString()), tokens);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return (null, tokens);
        }
    }

    private static JsonObject Body(DateOnly today, IReadOnlyList<ChatTurn> turns, IReadOnlyList<ChatTool> tools)
    {
        var body = new JsonObject
        {
            ["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject
                {
                    ["text"] = Prompt.Value
                        .Replace("{today}", today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
                        .Replace("{weekday}", today.DayOfWeek.ToString(), StringComparison.Ordinal),
                }),
            },
            ["contents"] = new JsonArray([.. turns.Select(Content)]),
            ["generationConfig"] = new JsonObject
            {
                ["thinkingConfig"] = new JsonObject { ["thinkingLevel"] = "minimal" },
                ["maxOutputTokens"] = MaxOutputTokens,
            },
        };
        if (tools.Count > 0)
        {
            body["tools"] = new JsonArray(new JsonObject
            {
                ["functionDeclarations"] = new JsonArray([.. tools.Select(tool => (JsonNode)new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parametersJsonSchema"] = JsonNode.Parse(tool.InputSchemaJson),
                })]),
            });
        }

        return body;
    }

    private static JsonNode Content(ChatTurn turn) => turn switch
    {
        UserTurn user => Message("user", new JsonObject { ["text"] = user.Text }),
        AssistantTurn assistant => Message("model", new JsonObject { ["text"] = assistant.Text }),
        // ADR-0010 4.: exactly as received, thought signatures included.
        ModelStepTurn step => JsonNode.Parse(step.RawContent)!,
        ToolResultsTurn results => Message("user", [.. results.Results.Select(FunctionResponse)]),
        _ => throw new ArgumentOutOfRangeException(nameof(turn), turn.GetType().Name, "Unknown chat turn."),
    };

    private static JsonObject Message(string role, params JsonNode[] parts) => new()
    {
        ["role"] = role,
        ["parts"] = new JsonArray(parts),
    };

    // The API takes the response as a JSON object: a tool result that is not one is wrapped as {"result": ...}.
    private static JsonNode FunctionResponse(ToolResult result)
    {
        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(result.Content);
        }
        catch (JsonException)
        {
            parsed = JsonValue.Create(result.Content);
        }

        var response = new JsonObject { ["name"] = result.Name, ["response"] = parsed as JsonObject ?? new JsonObject { ["result"] = parsed } };
        if (result.CallId is not null)
        {
            response["id"] = result.CallId;
        }

        return new JsonObject { ["functionResponse"] = response };
    }

}
