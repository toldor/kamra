using System.ComponentModel.DataAnnotations;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using QuantityDimension = KamraApp.Domain.Quantities.Dimension;

namespace KamraApp.Application.Ingredients;

// N7: dimension and category arrive as lower-case strings, like the pantry's unit and category.
public sealed class CreateIngredientRequest : IValidatableObject
{
    public string? Name { get; init; }

    public string? Dimension { get; init; }

    public string? DefaultCategory { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Ingredient.Tidy(Name ?? "").Length is 0 or > Ingredient.MaxNameLength)
        {
            yield return new ValidationResult(IngredientInput.NameMessage, [nameof(Name)]);
        }

        if (!PantryInput.TryParse<QuantityDimension>(Dimension, out _))
        {
            yield return new ValidationResult(IngredientInput.DimensionMessage, [nameof(Dimension)]);
        }

        if (!PantryInput.TryParse<Category>(DefaultCategory, out _))
        {
            yield return new ValidationResult(PantryInput.CategoryMessage, [nameof(DefaultCategory)]);
        }
    }
}

public static class IngredientInput
{
    public const string NameMessage = "A hozzávaló neve 1 és 100 karakter között legyen.";
    public const string DimensionMessage = "Válaszd ki, mivel méred: tömeg, térfogat vagy darab.";
}
