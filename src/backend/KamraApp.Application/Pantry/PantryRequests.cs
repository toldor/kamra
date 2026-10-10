using System.ComponentModel.DataAnnotations;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Pantry;

// US-1 requests. Unit, category and reason arrive as lower-case strings, so an unknown value gets a
// field-level Hungarian message instead of a generic "malformed body" binding error. Every rule the
// Domain enforces is checked here first, so invalid input is a 400, never a 500 (V-25).
public sealed class AddPantryItemRequest : IValidatableObject
{
    [Required(ErrorMessage = PantryInput.IngredientMessage)]
    public Guid? IngredientId { get; init; }

    [Required(ErrorMessage = PantryInput.AmountMessage)]
    public decimal? Amount { get; init; }

    [Required(ErrorMessage = PantryInput.UnitMessage)]
    public string? Unit { get; init; }

    // Missing: the ingredient's default category.
    public string? Category { get; init; }

    // Missing: estimated from the category.
    public DateOnly? ExpiryDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        throw new NotImplementedException();
}

public sealed class UpdatePantryItemRequest : IValidatableObject
{
    [Required(ErrorMessage = PantryInput.AmountUpdateMessage)]
    public decimal? Amount { get; init; }

    [Required(ErrorMessage = PantryInput.UnitMessage)]
    public string? Unit { get; init; }

    [Required(ErrorMessage = PantryInput.CategoryMessage)]
    public string? Category { get; init; }

    // Missing: estimated from the entry day and the category.
    public DateOnly? ExpiryDate { get; init; }

    // consumed / discarded / corrected; required when the amount decreases (US-1).
    public string? Reason { get; init; }

    // ADR-0008: the version the client saw; a stale one is rejected with PANTRY_ITEM_MODIFIED.
    [Required(ErrorMessage = PantryInput.VersionMessage)]
    public uint? Version { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        throw new NotImplementedException();
}

// Parsing and the user-facing messages of the pantry inputs, shared by the requests and use cases.
public static class PantryInput
{
    public const decimal MaxAmount = 100_000m;
    public const string IngredientMessage = "Válassz hozzávalót a listából.";
    public const string AmountMessage = "A mennyiség 0,001 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.";
    public const string AmountUpdateMessage = "A mennyiség 0 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.";
    public const string UnitMessage = "Válassz mértékegységet: g, dkg, kg, ml, dl, l vagy db.";
    public const string CategoryMessage = "Válassz kategóriát a listából.";
    public const string OtherNeedsExpiryMessage = "Az „egyéb” kategóriánál add meg a lejáratot.";
    public const string DecreaseNeedsReasonMessage = "Add meg, miért csökken a mennyiség: elfogyott, kidobtam vagy hibás rögzítés.";
    public const string IncreaseReasonMessage = "Növelésnél csak a hibás rögzítés javítása adható meg okként.";
    public const string VersionMessage = "Hiányzik a tétel verziója. Töltsd újra a listát.";

    // Only enum names are accepted (case-insensitive), never numbers.
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum =>
        throw new NotImplementedException();

    public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum =>
        TryParse<TEnum>(value, out var result) ? result : throw new ArgumentException($"Unvalidated {typeof(TEnum).Name}: {value}", nameof(value));

    // "A(z) tejföl tömegben mérhető: g, dkg vagy kg."
    public static string UnitForMessage(string ingredientName, Dimension dimension) => throw new NotImplementedException();
}
