using Microsoft.Extensions.Logging;

namespace KamraApp.Unit.Tests;

// Shared by the two Gemini adapter tests (US-2, US-6): the real API is never called (AGENTS.md 6.).
internal sealed record SentRequest(string Url, string? ApiKey, string Body);

// Answers in order: a response, a hang (until the attempt's timeout cancels it) or a network error.
internal sealed class ScriptedHandler : HttpMessageHandler
{
    private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _script = new();

    public List<SentRequest> Requests { get; } = [];

    public void Answer(HttpResponseMessage response) => _script.Enqueue(_ => Task.FromResult(response));

    public void Hang() => _script.Enqueue(async ct =>
    {
        await Task.Delay(Timeout.Infinite, ct);
        throw new InvalidOperationException("unreachable");
    });

    public void Fail(Exception error) => _script.Enqueue(_ => Task.FromException<HttpResponseMessage>(error));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new SentRequest(request.RequestUri!.ToString(),
            request.Headers.TryGetValues("x-goog-api-key", out var keys) ? keys.Single() : null,
            await request.Content!.ReadAsStringAsync(cancellationToken)));
        return await _script.Dequeue()(cancellationToken);
    }
}

// Every log entry as its message, its properties and its exception, so a test can check what is (not) logged.
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs ? string.Join(";", pairs.Select(p => $"{p.Key}={p.Value}")) : "";
        Entries.Add($"{formatter(state, exception)} | {properties} | {exception}");
    }
}
