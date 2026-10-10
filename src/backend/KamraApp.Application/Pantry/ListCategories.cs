using KamraApp.Domain.Pantry;

namespace KamraApp.Application.Pantry;

// ShelfLifeDays is null for Other: there is no estimate, the expiry date is required.
public sealed record CategoryResponse(Category Category, int? ShelfLifeDays);

// The fixed category list with its default shelf life (data_model.md, Kategórialista).
public static class ListCategories
{
    public static IReadOnlyList<CategoryResponse> Execute() =>
        [.. Enum.GetValues<Category>().Select(c => new CategoryResponse(c, Categories.ShelfLifeDays(c)))];
}
