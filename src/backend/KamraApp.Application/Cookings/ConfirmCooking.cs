using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recommendations;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;
using ValidationException = KamraApp.Application.Common.ValidationException;

namespace KamraApp.Application.Cookings;

// US-4 / ADR-0008: validate, answer a repeated request from the stored cooking, check the stock, deduct
// FEFO and save in one transaction. After any conflict the request id is looked up first: a concurrent
// request with the same id may have won, and then this one is a repeat (F3).
public sealed class ConfirmCooking(IRecipeRepository recipes, IPantryRepository pantry, ICookingRepository cookings,
    IIngredientLocks locks, SuggestionEvaluator suggestions, ICurrentHousehold household, TimeProvider time)
{
    public async Task<ConfirmCookingResult> ExecuteAsync(ConfirmCookingRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var requestId = valid.RequestId!.Value;
        if (await cookings.FindByRequestAsync(householdId, requestId, cancellationToken) is { } existing)
        {
            return await RepeatAsync(existing, valid, cancellationToken);
        }

        var details = await recipes.FindAsync(householdId, valid.RecipeId!.Value, cancellationToken) ?? throw CookingErrors.RecipeNotFound();
        // M8 / D-c: only a new cooking is refused; a repeated request was answered above.
        if (details.Archived)
        {
            throw CookingErrors.RecipeArchived();
        }

        var recipe = details.Candidate;
        var used = UsedInBaseUnits(recipe, valid) ?? throw ValidationException.ForField("used", CookingInput.UsedMessage);
        if (!used.Values.All(CookingInput.IsStorable))
        {
            throw ValidationException.ForField("used", CookingInput.PrecisionMessage);
        }

        try
        {
            // ADR-0008 6.: the stock is read inside the ingredient lock, so the suggestions see every other change.
            await locks.LockAsync(householdId, [.. used.Keys], cancellationToken);
            var items = await pantry.ListForCookingAsync(householdId, [.. used.Keys], cancellationToken);
            EnsureStockCovers(recipe, used, items);
            // Taken before Cooking.Create deducts from these tracked items (ToDictionary evaluates at once).
            var before = items.GroupBy(i => i.IngredientId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
            var now = time.GetUtcNow();
            var (cooking, movements) = Cooking.Create(householdId, recipe, valid.Servings!.Value, requestId, used, items, now);
            foreach (var (ingredientId, amount) in used.Where(u => u.Value > 0))
            {
                var total = before.GetValueOrDefault(ingredientId);
                if (await suggestions.AfterQuantityChangeAsync(householdId, ingredientId, total, total - amount, MovementReason.Consumed, now,
                    cancellationToken, cooking.Id))
                {
                    cooking.MarkSuggested(ingredientId);
                }
            }

            cookings.Add(cooking, movements);
            await cookings.SaveChangesAsync(cancellationToken);
        }
        catch (AppException exception) when (exception.Category == ErrorCategory.Conflict)
        {
            var winner = await cookings.FindByRequestAsync(householdId, requestId, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return await RepeatAsync(winner, valid, cancellationToken);
        }

        // Read back, so the first answer is built exactly like a repeated one (stored values, stored time).
        var saved = await cookings.FindByRequestAsync(householdId, requestId, cancellationToken)
            ?? throw new InvalidOperationException("The saved cooking was not found.");
        return new ConfirmCookingResult(Respond(saved, recipe), Created: true);
    }

    private async Task<ConfirmCookingResult> RepeatAsync(Cooking existing, ConfirmCookingRequest request, CancellationToken cancellationToken)
    {
        var recipe = (await recipes.FindAsync(household.HouseholdId, existing.RecipeId, cancellationToken)
            ?? throw new InvalidOperationException("The cooked recipe was not found.")).Candidate;
        var used = existing.RecipeId == request.RecipeId ? UsedInBaseUnits(recipe, request) : null;
        if (used is null || !existing.Matches(request.RecipeId!.Value, request.Servings!.Value, used))
        {
            throw CookingErrors.IdempotencyConflict();
        }

        return new ConfirmCookingResult(Respond(existing, recipe), Created: false);
    }

    // Null unless the request lists exactly the recipe's non-staple ingredients (duplicates are rejected earlier).
    private static Dictionary<Guid, decimal>? UsedInBaseUnits(RecipeCandidate recipe, ConfirmCookingRequest request)
    {
        var needs = recipe.NonStapleNeeds.ToDictionary(n => n.IngredientId);
        var used = request.Used!;
        if (used.Count != needs.Count || used.Any(u => !needs.ContainsKey(u.IngredientId!.Value)))
        {
            return null;
        }

        return used.ToDictionary(u => u.IngredientId!.Value, u => Units.ToBase(u.Amount!.Value, needs[u.IngredientId!.Value].Unit));
    }

    private static void EnsureStockCovers(RecipeCandidate recipe, Dictionary<Guid, decimal> used, IReadOnlyList<PantryItem> items)
    {
        var stock = items.ToLookup(i => i.IngredientId);
        if (used.All(u => u.Value <= stock[u.Key].Sum(i => i.Quantity)))
        {
            return;
        }

        throw new InsufficientStockException([.. recipe.NonStapleNeeds
            .OrderBy(n => n.IngredientName, Collation.Hungarian)
            .Select(n => new AvailableAmount(n.IngredientId, n.IngredientName, Units.FromBase(stock[n.IngredientId].Sum(i => i.Quantity), n.Unit), n.Unit))]);
    }

    // Names and units come from the recipe, which never changes after creation (data_model: no recipe
    // editing in the MVP); every amount comes from the stored cooking.
    private static CookingResponse Respond(Cooking cooking, RecipeCandidate recipe)
    {
        var needs = recipe.Needs.ToDictionary(n => n.IngredientId);
        var lines = cooking.Lines.Select(l => (Need: needs[l.IngredientId], Line: l))
            .OrderBy(x => x.Need.IngredientName, Collation.Hungarian)
            .ToList();
        return new CookingResponse(cooking.Id, cooking.RecipeId, recipe.Name, cooking.Servings, cooking.CookedAt,
            [.. lines.Select(x => new CookingResponseLine(x.Need.IngredientName, x.Need.Unit, Units.FromBase(x.Line.RequiredQuantity, x.Need.Unit),
                Units.FromBase(x.Line.UsedQuantity, x.Need.Unit), Units.FromBase(x.Line.Shortage, x.Need.Unit)))],
            [.. lines.Where(x => x.Line.Depleted).Select(x => x.Need.IngredientName)],
            [.. lines.Where(x => x.Line.Suggested).Select(x => x.Need.IngredientName)]);
    }
}
