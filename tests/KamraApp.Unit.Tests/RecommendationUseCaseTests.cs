using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recommendations;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-3 use case: the household's recipes and stock go into the Recommender, and an empty pantry is
// reported separately (ux_flows empty state).
public class RecommendationUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();
    private static readonly Ingredient Egg = Ingredient.CreateSystem(Guid.CreateVersion7(), "tojas", "tojás", Dimension.Count, Category.Eggs);
    private static readonly RecipeCandidate Rantotta = new(Guid.CreateVersion7(), "Rántotta", 2,
        [new RecipeNeed(Egg.Id, "tojás", 3m, Unit.Db, IsStaple: false)]);

    [Fact]
    public async Task An_empty_pantry_is_reported_with_no_recommendation()
    {
        var result = await Run();

        result.PantryIsEmpty.Should().BeTrue();
        result.Makeable.Should().BeEmpty();
        result.AlmostMakeable.Should().BeEmpty();
    }

    [Fact]
    public async Task The_households_stock_is_matched_against_its_recipes()
    {
        var (eggs, _) = PantryItem.Create(HouseholdId, Egg, 6m, Unit.Db, Category.Eggs, expiryDate: null, new DateOnly(2026, 10, 11), Now);

        var result = await Run(new PantryEntry(eggs, Egg.Name));

        result.PantryIsEmpty.Should().BeFalse();
        result.Makeable.Should().ContainSingle(r => r.Name == "Rántotta");
    }

    private static Task<RecommendationsResponse> Run(params PantryEntry[] stock) =>
        new GetRecommendations(new FakeRecipes(), new FakePantry(stock), new FixedHousehold(), new FixedTime(Now))
            .ExecuteAsync(CancellationToken.None);

    private sealed class FixedHousehold : ICurrentHousehold
    {
        public Guid HouseholdId => RecommendationUseCaseTests.HouseholdId;
    }

    private sealed class FakeRecipes : IRecipeRepository
    {
        public Task<IReadOnlyList<RecipeCandidate>> ListCandidatesAsync(Guid householdId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecipeCandidate>>(householdId == HouseholdId ? [Rantotta] : []);

        public Task<RecipeDetails?> FindAsync(Guid householdId, Guid recipeId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePantry(PantryEntry[] stock) : IPantryRepository
    {
        public Task<IReadOnlyList<PantryEntry>> ListAsync(Guid householdId, PantryFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PantryEntry>>(householdId == HouseholdId ? stock : []);

        public Task<PantryEntry?> FindForUpdateAsync(Guid householdId, Guid id, uint expectedVersion, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PantryItem>> ListForCookingAsync(Guid householdId, IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<decimal> TotalAsync(Guid householdId, Guid ingredientId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(PantryItem item, StockMovement movement) => throw new NotSupportedException();

        public void AddMovement(StockMovement movement) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
