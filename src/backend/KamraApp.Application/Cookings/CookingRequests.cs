using System.ComponentModel.DataAnnotations;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Cookings;

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

    // The decimals are checked in base units once the recipe's units are known (ConfirmCooking).
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // V-31: an empty id would turn every later identical cooking into a silent repeat without deduction.
        if (RequestId == Guid.Empty)
        {
            yield return new ValidationResult(CookingInput.RequestIdMessage, [nameof(RequestId)]);
        }

        if (Used!.Any(u => u?.IngredientId is null) || Used!.GroupBy(u => u?.IngredientId).Any(g => g.Count() > 1))
        {
            yield return new ValidationResult(CookingInput.UsedMessage, [nameof(Used)]);
        }

        if (Used!.Any(u => u is not null && (u.Amount is not { } amount || !CookingInput.IsInRange(amount))))
        {
            yield return new ValidationResult(CookingInput.AmountMessage, [nameof(Used)]);
        }
    }
}

// US-4 preview: missing servings means the recipe's own number.
public sealed class CookingPreviewQuery
{
    [Range(1, CookingInput.MaxServings, ErrorMessage = CookingInput.ServingsMessage)]
    public int? Servings { get; init; }
}

public sealed class UsedAmount
{
    public Guid? IngredientId { get; init; }

    public decimal? Amount { get; init; }
}

public static class CookingInput
{
    public const int MaxServings = Cooking.MaxServings;
    public const string ServingsMessage = "Az adagszám 1 és 100 között legyen.";
    public const string AmountMessage = "A felhasznált mennyiség 0 és 100 000 között legyen.";
    public const string PrecisionMessage = "Ilyen apró mennyiséget nem tudunk rögzíteni: grammban és milliliterben legfeljebb 3 tizedesjegy lehet.";
    public const string UsedMessage = "A felhasznált mennyiségeket a recept minden hozzávalójához add meg.";
    public const string RequestIdMessage = "Hiányzik a főzés azonosítója. Töltsd újra az oldalt.";
    public const string RecipeMessage = "Válassz receptet.";

    public static bool IsInRange(decimal amount) => amount is >= 0 and <= PantryInput.MaxAmount;

    // D-a: the stored precision is 3 decimals in base units (numeric(12,3), ADR-0002, V-24), so 0.0125 kg
    // (12.5 g, a preview default) is accepted while 0.0000001 kg is not.
    public static bool IsStorable(decimal baseQuantity) => decimal.Round(baseQuantity, 3) == baseQuantity;
}
