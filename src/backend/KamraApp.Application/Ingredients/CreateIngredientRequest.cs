using System.ComponentModel.DataAnnotations;

namespace KamraApp.Application.Ingredients;

// N7: dimension and category arrive as lower-case strings, like the pantry's unit and category.
public sealed class CreateIngredientRequest : IValidatableObject
{
    public string? Name { get; init; }

    public string? Dimension { get; init; }

    public string? DefaultCategory { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
}

public static class IngredientInput
{
    public const string NameMessage = "A hozzávaló neve 1 és 100 karakter között legyen.";
    public const string DimensionMessage = "Válaszd ki, mivel méred: tömeg, térfogat vagy darab.";
}
