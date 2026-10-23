using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Domain;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recipes;
using ValidationException = KamraApp.Application.Common.ValidationException;

namespace KamraApp.Application.Recipes;

// N3 (M9): an own recipe from the household's visible ingredients; another household's ingredient is
// reported exactly like a missing one (QA-1, P17).
public sealed class CreateRecipe(IIngredientRepository ingredients, IRecipeCatalog catalog, ICurrentHousehold household, TimeProvider time)
{
    public async Task<RecipeResponse> ExecuteAsync(CreateRecipeRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        var householdId = household.HouseholdId;
        var rows = valid.Ingredients!;
        var lines = new List<RecipeLine>();
        var errors = new Dictionary<string, string[]>();
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i]!;
            // ponytail: one lookup per row; a recipe has at most 50 (N3).
            var ingredient = await ingredients.FindVisibleAsync(householdId, row.IngredientId!.Value, cancellationToken)
                ?? throw PantryErrors.IngredientNotFound();
            var unit = PantryInput.Parse<Unit>(row.Unit!);
            if (Units.DimensionOf(unit) != ingredient.Dimension)
            {
                errors[RecipeInput.Row(i, "unit")] = [PantryInput.UnitForMessage(ingredient.Name, ingredient.Dimension)];
            }

            lines.Add(new RecipeLine(ingredient, row.Amount!.Value, unit));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        if (lines.All(l => l.Ingredient.IsStaple))
        {
            throw ValidationException.ForField("ingredients", RecipeInput.NeedsIngredientMessage);
        }

        var (recipe, recipeLines) = Recipe.CreateOwn(householdId, valid.Name!, valid.Servings!.Value, valid.Instructions ?? "", lines,
            time.GetUtcNow());
        catalog.Add(recipe, recipeLines);
        await catalog.SaveChangesAsync(cancellationToken);
        return new RecipeResponse(recipe.Id, recipe.Name, recipe.Servings, Own: true,
            [.. lines.Select(l => l.Ingredient.Name).Order(Collation.Hungarian)]);
    }
}
