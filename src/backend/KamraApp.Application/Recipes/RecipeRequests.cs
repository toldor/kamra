using System.ComponentModel.DataAnnotations;

namespace KamraApp.Application.Recipes;

// M9 (T3 mini-spec N3): an own recipe. A row error has the key "ingredients[2].amount", so the page puts it
// under that row's field. The rules that need the ingredient (visible, dimension, staple) are checked by
// the use case.
public sealed class CreateRecipeRequest : IValidatableObject
{
    public string? Name { get; init; }

    public int? Servings { get; init; }

    // Not required (N3).
    public string? Instructions { get; init; }

    public IReadOnlyList<RecipeIngredientRequest?>? Ingredients { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
}

public sealed class RecipeIngredientRequest
{
    public Guid? IngredientId { get; init; }

    public decimal? Amount { get; init; }

    public string? Unit { get; init; }
}

public static class RecipeInput
{
    public const string NameMessage = "A recept neve 1 és 100 karakter között legyen.";
    public const string NeedsIngredientMessage = "Adj meg legalább egy hozzávalót a víz, a só és a bors mellett.";
    public const string DuplicateMessage = "Ez a hozzávaló már szerepel a receptben.";
    public const string TooManyMessage = "Egy receptben legfeljebb 50 hozzávaló lehet.";
    public const string InstructionsMessage = "Az elkészítés legfeljebb 4000 karakter lehet.";

    public static string Row(int index, string field) => $"ingredients[{index}].{field}";
}
