using System.ComponentModel.DataAnnotations;
using KamraApp.Application.Cookings;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Recipes;
using QuantityUnit = KamraApp.Domain.Quantities.Unit;

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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Ingredient.Tidy(Name ?? "").Length is 0 or > Recipe.MaxNameLength)
        {
            yield return new ValidationResult(RecipeInput.NameMessage, [nameof(Name)]);
        }

        if (Servings is not (>= 1 and <= Cooking.MaxServings))
        {
            yield return new ValidationResult(CookingInput.ServingsMessage, [nameof(Servings)]);
        }

        if (Instructions?.Length > Recipe.MaxInstructionsLength)
        {
            yield return new ValidationResult(RecipeInput.InstructionsMessage, [nameof(Instructions)]);
        }

        var rows = Ingredients ?? [];
        if (rows.Count == 0 || rows.Count > Recipe.MaxIngredients)
        {
            yield return new ValidationResult(rows.Count == 0 ? RecipeInput.NeedsIngredientMessage : RecipeInput.TooManyMessage, [nameof(Ingredients)]);
            yield break;
        }

        var seen = new HashSet<Guid>();
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i] is not { } row)
            {
                yield return new ValidationResult(PantryInput.IngredientMessage, [RecipeInput.Row(i, "ingredientId")]);
                continue;
            }

            if (row.IngredientId is not { } id || id == Guid.Empty)
            {
                yield return new ValidationResult(PantryInput.IngredientMessage, [RecipeInput.Row(i, "ingredientId")]);
            }
            else if (!seen.Add(id))
            {
                yield return new ValidationResult(RecipeInput.DuplicateMessage, [RecipeInput.Row(i, "ingredientId")]);
            }

            if (row.Amount is not { } amount || !PantryInput.IsValidAmount(amount, minimum: 0.001m))
            {
                yield return new ValidationResult(PantryInput.AmountMessage, [RecipeInput.Row(i, "amount")]);
            }

            if (!PantryInput.TryParse<QuantityUnit>(row.Unit, out _))
            {
                yield return new ValidationResult(PantryInput.UnitMessage, [RecipeInput.Row(i, "unit")]);
            }
        }
    }
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
