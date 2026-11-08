using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.ItemProposals;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.ItemProposals;

// US-2: the sentence becomes item proposals; nothing goes into the pantry yet. The LLM gets the sentence and the
// names of the ingredients the household can see, nothing else (scope_contract.md, Külső API). The date, the unit
// and the ingredient id are decided here (us2_minispec K6-K8).
public sealed class SubmitItemProposals(IIngredientParser parser, IItemProposalRepository proposals, IIngredientRepository ingredients,
    IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public async Task<SubmitItemProposalsResponse> ExecuteAsync(SubmitItemProposalsRequest? request, CancellationToken cancellationToken)
    {
        var text = RequestValidator.Validate(request).Text!.Trim();
        if (!parser.Enabled)
        {
            throw ItemProposalErrors.LlmDisabled();
        }

        var householdId = household.HouseholdId;
        var visible = await ingredients.ListVisibleAsync(householdId, null, cancellationToken);
        var parsed = await parser.ParseAsync(text, [.. visible.Select(i => i.Name)], cancellationToken);
        if (parsed.Count == 0)
        {
            throw ItemProposalErrors.NoItems();
        }

        // The port promises a text for every item; one without it is an answer outside the schema (ADR-0007).
        if (parsed.Any(p => string.IsNullOrWhiteSpace(p.Text)))
        {
            throw ItemProposalErrors.LlmInvalidResponse();
        }

        var byName = visible.GroupBy(i => i.Name, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var stock = (await pantry.ListAsync(householdId, PantryFilter.None, cancellationToken)).ToLookup(e => e.Item.IngredientId);
        var now = time.GetUtcNow();
        var today = Today.Of(now);

        var created = parsed.Select(p => Propose(p, p.IngredientName is { } name ? byName.GetValueOrDefault(name) : null, householdId, today, now))
            .ToList();
        proposals.AddRange(created.Select(c => c.Proposal));
        await proposals.SaveChangesAsync(cancellationToken);

        return new SubmitItemProposalsResponse([.. created.Select(c => Response(c.Proposal, c.Ingredient, c.UnitMismatch, stock))]);
    }

    public ItemProposalAvailability Availability() => new(parser.Enabled);

    private static (ItemProposal Proposal, Ingredient? Ingredient, bool UnitMismatch) Propose(ParsedItem parsed, Ingredient? ingredient,
        Guid householdId, DateOnly today, DateTimeOffset now)
    {
        // AGENTS.md 5.: LLM output is validated before it is stored. An amount the manual form would refuse (outside
        // 0.001-100 000, more than 3 decimals: numeric(12,3), V-24) is treated as missing, never guessed.
        var amount = parsed.Amount is { } a && PantryInput.IsValidAmount(a, minimum: 0.001m) ? parsed.Amount : null;
        var unit = UnitWords.Map(parsed.UnitText);
        var mismatch = unit is { } u && ingredient is not null && Units.DimensionOf(u) != ingredient.Dimension;

        var resolved = ExpiryWords.Resolve(parsed.ExpiryText, today);
        var estimate = resolved is null && ingredient is not null ? Categories.EstimateExpiry(ingredient.DefaultCategory, today) : null;
        var unrecognised = resolved is null && !string.IsNullOrWhiteSpace(parsed.ExpiryText) ? parsed.ExpiryText.Trim() : null;

        var proposal = ItemProposal.Create(householdId, parsed.Text, ingredient?.Id, amount, mismatch ? null : unit,
            resolved ?? estimate, estimate is not null, unrecognised, now);
        return (proposal, ingredient, mismatch);
    }

    private static ItemProposalResponse Response(ItemProposal proposal, Ingredient? ingredient, bool unitMismatch,
        ILookup<Guid, PantryEntry> stock) =>
        new(proposal.Id, proposal.Text, ingredient?.Id, ingredient?.Name, ingredient?.Dimension, proposal.Amount, proposal.Unit, unitMismatch,
            proposal.ExpiryDate, proposal.ExpiryEstimated, proposal.ExpiryText, ingredient is null ? null : InStock(stock[ingredient.Id]));

    // The total, in the unit of the earliest expiring item, with that item's expiry (ux_flows 1. flow, 3. lépés).
    private static InStockResponse? InStock(IEnumerable<PantryEntry> entries)
    {
        var items = entries.Select(e => e.Item).ToList();
        if (items.Count == 0)
        {
            return null;
        }

        var earliest = items.MinBy(i => i.ExpiryDate)!;
        return new InStockResponse(AmountResponse.Of(items.Sum(i => i.Quantity), earliest.EnteredUnit)!, earliest.ExpiryDate, items.Count);
    }
}
