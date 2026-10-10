using KamraApp.Application.Common;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Pantry;

// US-1: edit, decrease (with a reason) or delete (decrease to 0) a pantry item, guarded by its version.
public sealed class UpdatePantryItem(IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public async Task<PantryItemResponse> ExecuteAsync(Guid id, UpdatePantryItemRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var unit = PantryInput.Parse<Unit>(valid.Unit!);
        var category = PantryInput.Parse<Category>(valid.Category!);
        MovementReason? reason = valid.Reason is null ? null : PantryInput.Parse<MovementReason>(valid.Reason);
        var entry = await pantry.FindForUpdateAsync(household.HouseholdId, id, valid.Version!.Value, cancellationToken)
            ?? throw PantryErrors.ItemNotFound();
        var item = entry.Item;

        // ADR-0008: a stale edit is a conflict before anything else - judging it against the current amount
        // would turn the other tab's change into a misleading validation error. The save re-checks the
        // version for a change between this read and the write.
        if (item.Version != valid.Version)
        {
            throw PantryErrors.ItemModified();
        }

        PantryInput.EnsureUnitFits(unit, Units.DimensionOf(item.EnteredUnit), entry.IngredientName);
        PantryInput.EnsureExpiryKnown(category, valid.ExpiryDate);

        var quantity = Units.ToBase(valid.Amount!.Value, unit);
        if (quantity < item.Quantity && reason is null)
        {
            throw ValidationException.ForField("reason", PantryInput.DecreaseNeedsReasonMessage);
        }

        if (quantity > item.Quantity && reason is not (null or MovementReason.Corrected))
        {
            throw ValidationException.ForField("reason", PantryInput.IncreaseReasonMessage);
        }

        var now = time.GetUtcNow();
        item.UpdateDetails(category, valid.ExpiryDate, entryDay: Today.Of(item.CreatedAt), now);
        if (item.ChangeQuantity(valid.Amount.Value, unit, reason, now) is { } movement)
        {
            pantry.AddMovement(movement);
        }

        await pantry.SaveChangesAsync(cancellationToken);
        return PantryItemResponse.From(item, entry.IngredientName, Today.Of(now));
    }
}
