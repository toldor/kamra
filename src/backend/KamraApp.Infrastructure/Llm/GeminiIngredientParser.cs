using System.Diagnostics;
using System.Net;
using System.Text;
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
public sealed partial class GeminiIngredientParser(HttpClient http, IOptions<LlmOptions> options, ILogger<GeminiIngredientParser> logger)
    : IIngredientParser
{
    public const string PromptVersion = "quick-entry.v3";
    public const string NoMatch = "-";
    public const int MaxItems = 20;

    // A ceiling on the answer: 20 items fit easily; a runaway answer (seen in T0 on another model) is cut and refused.
    private const int MaxOutputTokens = 2048;

    private static readonly Lazy<string> Prompt = new(() =>
    {
        using var stream = typeof(GeminiIngredientParser).Assembly.GetManifestResourceStream("quick-entry.v3.txt")
            ?? throw new InvalidOperationException("The embedded prompt quick-entry.v3.txt is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    });

    // Strict numbers: "2" is not an amount (General, unlike Web, does not read numbers from strings).
    private static readonly JsonSerializerOptions AnswerJson = new(JsonSerializerDefaults.General) { PropertyNameCaseInsensitive = true };

    public bool Enabled => true;

    public async Task<IReadOnlyList<ParsedItem>> ParseAsync(string text, IReadOnlyList<string> ingredientNames, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var url = new Uri(settings.BaseUrl, $"v1beta/models/{settings.Model}:generateContent");
        var body = Body(text, ingredientNames).ToJsonString();

        for (var attempt = 1; ; attempt++)
        {
            var started = Stopwatch.GetTimestamp();
            var (status, answer, failure) = await SendAsync(url, body, settings, cancellationToken);
            // No answer (timeout, network) or an outage (5xx, 429): one retry. A refused request (key, quota, request
            // shape) would fail again, so it is not retried.
            failure ??= status is HttpStatusCode.TooManyRequests || (int)status >= 500 ? "unavailable"
                : (int)status >= 400 ? "refused"
                : null;
            if (failure is not null)
            {
                LogAttempt(settings.Model, attempt, started, failure, null);
                if (attempt == 1 && failure != "refused")
                {
                    continue;
                }

                throw ItemProposalErrors.LlmUnavailable();
            }

            var (items, tokens) = Read(answer!, ingredientNames);
            LogAttempt(settings.Model, attempt, started, items is null ? "invalid" : "ok", tokens);
            return items ?? throw ItemProposalErrors.LlmInvalidResponse();
        }
    }

    // One attempt within its own timeout. Failure is set when no HTTP answer came (timeout, network error).
    private async Task<(HttpStatusCode Status, string? Answer, string? Failure)> SendAsync(Uri url, string body, LlmOptions settings,
        CancellationToken cancellationToken)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attempt.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            request.Headers.Add("x-goog-api-key", settings.ApiKey);
            using var response = await http.SendAsync(request, attempt.Token);
            return (response.StatusCode, await response.Content.ReadAsStringAsync(attempt.Token), null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (default, null, "timeout");
        }
        catch (HttpRequestException)
        {
            return (default, null, "unavailable");
        }
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

    private void LogAttempt(string model, int attempt, long started, string outcome, (int Input, int Output)? tokens)
    {
        var durationMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        LlmCall(logger, model, PromptVersion, attempt, outcome, durationMs, tokens?.Input, tokens?.Output);
    }

    // observability.md: one event per attempt; no sentence, no answer, no key.
    [LoggerMessage(Level = LogLevel.Information,
        Message = "LLM call {Model} {PromptVersion} attempt {Attempt}: {Outcome} in {DurationMs} ms, tokens {InputTokens}/{OutputTokens}")]
    private static partial void LlmCall(ILogger logger, string model, string promptVersion, int attempt, string outcome, long durationMs,
        int? inputTokens, int? outputTokens);

    private sealed record Answer(List<AnswerItem>? Items);

    private sealed record AnswerItem(string? Text, string? Ingredient, decimal? Amount, string? Unit, string? Expiry);
}
