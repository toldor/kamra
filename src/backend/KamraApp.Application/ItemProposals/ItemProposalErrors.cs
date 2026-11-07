using KamraApp.Application.Common;

namespace KamraApp.Application.ItemProposals;

// Stable codes and safe Hungarian titles of US-2 (ADR-0007, error_handling.md). The frontend shows its own,
// longer text for each code (us2_minispec, Felhasználói szövegek).
public static class ItemProposalErrors
{
    public static InvalidInputException NoItems() =>
        new("ITEM_PROPOSAL_NO_ITEMS", "Nem találtam élelmiszert a mondatban.");

    public static NotFoundException NotFound() =>
        new("ITEM_PROPOSAL_NOT_FOUND", "Ez a javaslat már nem érhető el.");

    public static ConflictException StateConflict() =>
        new("ITEM_PROPOSAL_STATE_CONFLICT", "Ezt a javaslatot közben már kihagytad vagy jóváhagytad.");

    public static UnavailableException LlmUnavailable() =>
        new("LLM_UNAVAILABLE", "A szövegfeldolgozó most nem válaszol.");

    public static BadGatewayException LlmInvalidResponse() =>
        new("LLM_INVALID_RESPONSE", "Nem sikerült értelmezni a mondatot.");

    public static UnavailableException LlmDisabled() =>
        new("LLM_DISABLED", "A mondatos bevitel ki van kapcsolva.");
}
