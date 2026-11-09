using KamraApp.Application.ItemProposals;

namespace KamraApp.Infrastructure.Llm;

// Llm:Enabled = false (us2_minispec K10): the quick entry is hidden, and nothing ever reaches an LLM.
public sealed class DisabledIngredientParser : IIngredientParser
{
    public bool Enabled => false;

    public Task<IReadOnlyList<ParsedItem>> ParseAsync(string text, IReadOnlyList<string> ingredientNames, CancellationToken cancellationToken) =>
        throw ItemProposalErrors.LlmDisabled();
}
