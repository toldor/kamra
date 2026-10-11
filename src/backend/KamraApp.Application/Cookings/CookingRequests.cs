using System.ComponentModel.DataAnnotations;
using KamraApp.Application.Pantry;

namespace KamraApp.Application.Cookings;

// US-4: the confirmed cooking. Used amounts are in the recipe's unit for each ingredient (S5-D2).
public sealed class ConfirmCookingRequest : IValidatableObject
{
    // ADR-0008: created when the confirmation screen opens and kept until the cooking succeeds.
    [Required(ErrorMessage = CookingInput.RequestIdMessage)]
    public Guid? RequestId { get; init; }

    [Required(ErrorMessage = CookingInput.RecipeMessage)]
    public Guid? RecipeId { get; init; }

    [Required(ErrorMessage = CookingInput.ServingsMessage)]
    [Range(1, CookingInput.MaxServings, ErrorMessage = CookingInput.ServingsMessage)]
    public int? Servings { get; init; }

    [Required(ErrorMessage = CookingInput.UsedMessage)]
    public IReadOnlyList<UsedAmount>? Used { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
}

public sealed class UsedAmount
{
    public Guid? IngredientId { get; init; }

    public decimal? Amount { get; init; }
}

public static class CookingInput
{
    public const int MaxServings = 100;
    public const string ServingsMessage = "Az adagszám 1 és 100 között legyen.";
    public const string AmountMessage = "A felhasznált mennyiség 0 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.";
    public const string UsedMessage = "A felhasznált mennyiségeket a recept minden hozzávalójához add meg.";
    public const string RequestIdMessage = "Hiányzik a főzés azonosítója. Töltsd újra az oldalt.";
    public const string RecipeMessage = "Válassz receptet.";

    public static bool IsValidUsedAmount(decimal amount) => PantryInput.IsValidAmount(amount, minimum: 0m);
}
