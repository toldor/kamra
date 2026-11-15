using System.Text.Json;
using System.Text.Json.Nodes;
using KamraApp.Application.ItemProposals;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KamraApp.Infrastructure.Llm;

// ADR-0009: Gemini generateContent over plain HTTP (no SDK), a fixed JSON schema, thinkingLevel minimal and the
// versioned prompt quick-entry.v3. QA-3: 15 s per attempt and one immediate retry on a timeout, 5xx, 429 or a network
// error; an answer outside the schema is not retried. The log has the model, the prompt version, the time and the
// token counts, never the sentence or the answer (AGENTS.md 5.).
public sealed class GeminiIngredientParser(HttpClient http, IOptions<LlmOptions> options, ILogger<GeminiIngredientParser> logger)
    : IIngredientParser
{
    public const string PromptVersion = "quick-entry.v3";
    public const string NoMatch = "-";
    public const int MaxItems = 20;

    // A ceiling on the answer: 20 items fit easily; a runaway answer (seen in T0 on another model) is cut and refused.
    private const int MaxOutputTokens = 2048;

    private static readonly Lazy<string> Prompt = new(() => GeminiHttp.LoadPrompt("quick-entry.v3.txt"));

    // Strict numbers: "2" is not an amount (General, unlike Web, does not read numbers from strings).
    private static readonly JsonSerializerOptions AnswerJson = new(JsonSerializerDefaults.General) { PropertyNameCaseInsensitive = true };

    public bool Enabled => true;

    public async Task<IReadOnlyList<ParsedItem>> ParseAsync(string text, IReadOnlyList<string> ingredientNames, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var answer = await GeminiHttp.GenerateAsync(http, settings, settings.Model, PromptVersion, Body(text, ingredientNames).ToJsonString(),
            logger, ItemProposalErrors.LlmUnavailable, cancellationToken);

        var (items, tokens) = Read(answer.Body, ingredientNames);
        GeminiHttp.Log(logger, settings.Model, PromptVersion, answer.Attempt, answer.Started, items is null ? "invalid" : "ok", tokens);
        return items ?? throw ItemProposalErrors.LlmInvalidResponse();
    }

    // AGENTS.md 5.: the answer is checked against the schema before anything uses it. Null items: outside the schema.
    // The token counts are read first, so an invalid answer is still measured (K12).
    private static (IReadOnlyList<ParsedItem>? Items, (int Input, int Output)? Tokens) Read(string answer, IReadOnlyList<string> ingredientNames)
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
            if (candidate is null || (string?)candidate["finishReason"] != "STOP")
            {
                return (null, tokens);
            }

            var text = string.Concat((candidate["content"]?["parts"]?.AsArray() ?? [])
                .Where(part => (bool?)part?["thought"] != true)
                .Select(part => (string?)part?["text"]));
            var items = JsonSerializer.Deserialize<Answer>(text, AnswerJson)?.Items;
            if (items is null || items.Count > MaxItems || items.Any(item => string.IsNullOrWhiteSpace(item.Text)))
            {
                return (null, tokens);
            }

            return ([.. items.Select(item => new ParsedItem(item.Text!.Trim(),
                item.Ingredient is { } name && name != NoMatch && ingredientNames.Contains(name, StringComparer.Ordinal) ? name : null,
                item.Amount, item.Unit, item.Expiry))], tokens);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return (null, tokens);
        }
    }

    private static JsonObject Body(string text, IReadOnlyList<string> ingredientNames) => new()
    {
        ["system_instruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = Prompt.Value }) },
        ["contents"] = new JsonArray(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray(new JsonObject { ["text"] = text }),
        }),
        ["generationConfig"] = new JsonObject
        {
            // ResponseFormatConfig -> TextResponseFormat: mimeType is an enum (APPLICATION_JSON), not a MIME string
            // (https://aistudio.google.com/api-reference/generate-content, checked 2026-11-08; measured in T0).
            ["responseFormat"] = new JsonObject
            {
                ["text"] = new JsonObject { ["mimeType"] = "APPLICATION_JSON", ["schema"] = Schema(ingredientNames) },
            },
            ["thinkingConfig"] = new JsonObject { ["thinkingLevel"] = "minimal" },
            ["maxOutputTokens"] = MaxOutputTokens,
        },
    };

    // The schema measured in T0 (prompt v3): every field required, null allowed, the ingredient from the list or "-".
    private static JsonObject Schema(IReadOnlyList<string> ingredientNames) => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["items"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["text"] = new JsonObject { ["type"] = "string" },
                        ["ingredient"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray([.. ingredientNames.Append(NoMatch).Select(name => (JsonNode)name!)]),
                        },
                        ["amount"] = new JsonObject { ["type"] = new JsonArray("number", "null") },
                        ["unit"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                        ["expiry"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                    },
                    ["required"] = new JsonArray("text", "ingredient", "amount", "unit", "expiry"),
                },
            },
        },
        ["required"] = new JsonArray("items"),
    };


    private sealed record Answer(List<AnswerItem>? Items);

    private sealed record AnswerItem(string? Text, string? Ingredient, decimal? Amount, string? Unit, string? Expiry);
}
