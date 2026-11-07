using KamraApp.Application.Pantry;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Quantities;

namespace KamraApp.Application.ItemProposals;

// us2_minispec K4. UnitMismatch: the model's unit does not measure the ingredient ("3 darab paprika"), so Unit is
// null and the amount stays. InStock: the household's stock of the ingredient, from the database (the LLM gets none).
public sealed record ItemProposalResponse(
    Guid Id,
    string Text,
    Guid? IngredientId,
    string? IngredientName,
    Dimension? Dimension,
    decimal? Amount,
    Unit? Unit,
    bool UnitMismatch,
    DateOnly? ExpiryDate,
    bool ExpiryEstimated,
    string? ExpiryText,
    InStockResponse? InStock);

// The total in the unit of the earliest expiring item, and that item's expiry date.
public sealed record InStockResponse(AmountResponse Amount, DateOnly EarliestExpiry);

public sealed record SubmitItemProposalsResponse(IReadOnlyList<ItemProposalResponse> Items);

public sealed record ItemProposalAvailability(bool Enabled);

// ADR-0007: Created is false for a repeated approval, which returns the first one's pantry item (200 instead of 201).
public sealed record ApprovedItemProposal(PantryItemResponse Item, bool Created);
