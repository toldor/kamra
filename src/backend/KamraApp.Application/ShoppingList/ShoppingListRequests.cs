using System.ComponentModel.DataAnnotations;
using KamraApp.Application.Pantry;
using QuantityUnit = KamraApp.Domain.Quantities.Unit;

namespace KamraApp.Application.ShoppingList;

// US-5 requests. A list row may have no amount, so amount and unit are optional, but only together; the
// amount follows the pantry's input rules (PantryInput), the unit is checked against the ingredient later.
public sealed class AddShoppingListItemRequest : IValidatableObject
{
    [Required(ErrorMessage = PantryInput.IngredientMessage)]
    public Guid? IngredientId { get; init; }

    public decimal? Amount { get; init; }

    public string? Unit { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ShoppingListInput.ValidateAmount(Amount, Unit);
}

// D5: check or uncheck a row, and set its amount (both empty: no amount).
public sealed class UpdateShoppingListItemRequest : IValidatableObject
{
    [Required(ErrorMessage = ShoppingListInput.CheckedMessage)]
    public bool? Checked { get; init; }

    public decimal? Amount { get; init; }

    public string? Unit { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ShoppingListInput.ValidateAmount(Amount, Unit);
}

// M13: the amount to buy; empty puts the row on the list without an amount.
public sealed class AcceptShoppingSuggestionRequest : IValidatableObject
{
    public decimal? Amount { get; init; }

    public string? Unit { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ShoppingListInput.ValidateAmount(Amount, Unit);
}

// Both empty: the minimum is removed.
public sealed class SetIngredientMinimumRequest : IValidatableObject
{
    public decimal? Amount { get; init; }

    public string? Unit { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ShoppingListInput.ValidateAmount(Amount, Unit);
}

public static class ShoppingListInput
{
    public const string AmountWithUnitMessage = "Add meg a mennyiséget és a mértékegységet is, vagy hagyd üresen mindkettőt.";
    public const string CheckedMessage = "Add meg, hogy a tétel ki van-e pipálva.";
    public const string LimitMessage = "Összeadva legfeljebb 100 000 lehet egy tétel mennyisége. Írd át a tétel mennyiségét a listán.";

    // Amount 0,001–100 000 with at most 3 decimals and a known unit, or neither.
    public static IEnumerable<ValidationResult> ValidateAmount(decimal? amount, string? unit)
    {
        if (amount is null && unit is null)
        {
            yield break;
        }

        if (amount is not { } value)
        {
            yield return new ValidationResult(AmountWithUnitMessage, ["Amount"]);
        }
        else if (!PantryInput.IsValidAmount(value, minimum: 0.001m))
        {
            yield return new ValidationResult(PantryInput.AmountMessage, ["Amount"]);
        }

        if (unit is null)
        {
            yield return new ValidationResult(AmountWithUnitMessage, ["Unit"]);
        }
        else if (!PantryInput.TryParse<QuantityUnit>(unit, out _))
        {
            yield return new ValidationResult(PantryInput.UnitMessage, ["Unit"]);
        }
    }

    // A validated unit, or null when the request has no amount.
    public static QuantityUnit? ParseUnit(string? unit) => unit is null ? null : PantryInput.Parse<QuantityUnit>(unit);
}
