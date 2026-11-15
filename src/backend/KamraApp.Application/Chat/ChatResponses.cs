namespace KamraApp.Application.Chat;

// us6_minispec K2: the answer and the tools the model looked at, in order and without repeats (the page names them).
public sealed record AskChatResponse(string Answer, IReadOnlyList<string> ToolsUsed);

// K2, K8: the client shows the "Kérdezz" menu item only when the switch is on.
public sealed record ChatAvailability(bool Enabled);
