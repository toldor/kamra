using System.ComponentModel.DataAnnotations;
using KamraApp.Domain.Pantry;

namespace KamraApp.Application.Pantry;

// The filters of the pantry list (query string), validated like any request (ADR-0004).
public sealed class ListPantryItemsQuery : IValidatableObject
{
    [StringLength(PantryInput.MaxSearchLength, ErrorMessage = PantryInput.SearchMessage)]
    public string? Search { get; init; }

    public string? Category { get; init; }

    public bool? ExpiringSoon { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Category is not null && !PantryInput.TryParse<Category>(Category, out _))
        {
            yield return new ValidationResult(PantryInput.CategoryMessage, [nameof(Category)]);
        }
    }
}
