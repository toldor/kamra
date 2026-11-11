namespace KamraApp.McpServer;

// T1 review J3: what went wrong inside a tool of this request. The MCP SDK turns every tool exception into an error
// result; the chat needs to tell a refused argument (the model's fault, 502) from the app's own failure (500).
public sealed class ToolFailures
{
    private Exception? _last;

    public void Record(Exception failure) => _last = failure;

    // The failure of the last call, once: the next call starts clean.
    public Exception? Take()
    {
        var failure = _last;
        _last = null;
        return failure;
    }
}
