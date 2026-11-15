using KamraApp.Application.Common;

namespace KamraApp.Application.Chat;

// Stable codes of ADR-0007 with titles for the chat (us6_minispec: no new code). The frontend shows its own,
// longer text for each code.
public static class ChatErrors
{
    public static UnavailableException LlmDisabled() =>
        new("LLM_DISABLED", "A kérdezés ki van kapcsolva.");

    public static UnavailableException LlmUnavailable() =>
        new("LLM_UNAVAILABLE", "A szövegfeldolgozó most nem válaszol.");

    public static BadGatewayException LlmInvalidResponse() =>
        new("LLM_INVALID_RESPONSE", "Erre most nem sikerült válaszolni.");
}
