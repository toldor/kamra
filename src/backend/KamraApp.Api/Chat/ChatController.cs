using KamraApp.Api.Auth;
using KamraApp.Application.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KamraApp.Api.Chat;

// US-6 (us6_minispec K2): one question of the read-only chat; the client keeps the conversation.
[ApiController]
[Route("api/v1/chat")]
public sealed class ChatController : ControllerBase
{
    // K2, K8: the client shows the "Kérdezz" menu item only when the switch is on.
    [HttpGet("availability")]
    [Produces("application/json")]
    public ChatAvailability Availability([FromServices] AskChat askChat) => askChat.Availability();

    // K9: limited per household; one question is at most 3 model calls (ADR-0010 4.).
    [HttpPost]
    [EnableRateLimiting(AuthenticationSetup.ChatRateLimitPolicy)]
    [Produces("application/json")]
    public Task<AskChatResponse> Ask(AskChatRequest? request, [FromServices] AskChat askChat, CancellationToken cancellationToken) =>
        askChat.ExecuteAsync(request, cancellationToken);
}
