using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace KamraApp.Infrastructure.Llm;

// The answer of a successful generateContent call: its body, the attempt that brought it, and when that attempt started.
internal sealed record GeminiAnswer(string Body, int Attempt, long Started);

// QA-3 for both Gemini adapters (ADR-0009, ADR-0010 4.): every attempt has its own timeout; a timeout, a network error,
// a 5xx or a 429 is retried once at once; a refused request (any other 4xx: key, quota, prepaid balance, request shape)
// would fail again, so it is not retried. Both end in LLM_UNAVAILABLE. One log event per attempt (observability.md):
// the model, the prompt version, the outcome, the time and the token counts - never the text, the answer or the key.
internal static partial class GeminiHttp
{
    public static async Task<GeminiAnswer> GenerateAsync(HttpClient http, LlmOptions settings, string model, string promptVersion, string body,
        ILogger logger, Func<Exception> unavailable, CancellationToken cancellationToken)
    {
        var url = new Uri(settings.BaseUrl, $"v1beta/models/{model}:generateContent");
        for (var attempt = 1; ; attempt++)
        {
            var started = Stopwatch.GetTimestamp();
            var (status, answer, failure) = await SendAsync(http, url, body, settings, cancellationToken);
            failure ??= status is HttpStatusCode.TooManyRequests || (int)status >= 500 ? "unavailable"
                : (int)status >= 400 ? "refused"
                : null;
            if (failure is null)
            {
                return new GeminiAnswer(answer!, attempt, started);
            }

            Log(logger, model, promptVersion, attempt, started, failure, null);
            if (attempt > 1 || failure == "refused")
            {
                throw unavailable();
            }
        }
    }

    // The outcome of a received answer ("ok" or "invalid") is logged by the adapter, which reads the token counts.
    public static void Log(ILogger logger, string model, string promptVersion, int attempt, long started, string outcome,
        (int Input, int Output)? tokens)
    {
        var durationMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        LlmCall(logger, model, promptVersion, attempt, outcome, durationMs, tokens?.Input, tokens?.Output);
    }

    // AGENTS.md 5.: a versioned prompt file, embedded in this assembly.
    public static string LoadPrompt(string resourceName)
    {
        using var stream = typeof(GeminiHttp).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded prompt {resourceName} is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // One attempt within its own timeout. Failure is set when no HTTP answer came (timeout, network error).
    private static async Task<(HttpStatusCode Status, string? Answer, string? Failure)> SendAsync(HttpClient http, Uri url, string body,
        LlmOptions settings, CancellationToken cancellationToken)
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

    [LoggerMessage(Level = LogLevel.Information,
        Message = "LLM call {Model} {PromptVersion} attempt {Attempt}: {Outcome} in {DurationMs} ms, tokens {InputTokens}/{OutputTokens}")]
    private static partial void LlmCall(ILogger logger, string model, string promptVersion, int attempt, string outcome, long durationMs,
        int? inputTokens, int? outputTokens);
}
