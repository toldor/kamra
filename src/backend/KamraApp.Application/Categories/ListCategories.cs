using KamraApp.Domain.Pantry;

namespace KamraApp.Application.Categories;

// ShelfLifeDays is null for Other: there is no estimate, the expiry date is required.
public sealed record CategoryResponse(Category Category, int? ShelfLifeDays);

// The fixed category list with its default shelf life (data_model.md, Kategórialista).
public sealed class ListCategories
{
    public IReadOnlyList<CategoryResponse> Execute() => throw new NotImplementedException();
}
