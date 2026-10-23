using System.ComponentModel.DataAnnotations;
using KamraApp.Application.ShoppingList;

namespace KamraApp.Application.Ingredients;

// Both empty: the minimum is removed.
public sealed class SetIngredientMinimumRequest : IValidatableObject
{
    public decimal? Amount { get; init; }

    public string? Unit { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ShoppingListInput.ValidateAmount(Amount, Unit);
}
