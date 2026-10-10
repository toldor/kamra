using KamraApp.Application.Common;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Ingredients;

public sealed record IngredientResponse(Guid Id, string Name, Dimension Dimension, Category DefaultCategory);

public sealed class ListIngredients(IIngredientRepository ingredients, ICurrentHousehold household)
{
    public Task<IReadOnlyList<IngredientResponse>> ExecuteAsync(string? search, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {ingredients.GetHashCode() + household.GetHashCode()}");
}
