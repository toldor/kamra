using KamraApp.Application.Common;
using KamraApp.Application.Cookings;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recipes;
using KamraApp.Application.Recommendations;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recipes;
using KamraApp.Domain.Recommendations;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// T3 mini-spec N2-N4 and N7 (M9-M11): own recipes, archiving and own ingredients, with in-memory ports
// that filter on the household like the repositories (QA-1). The database rules (the archive row, another
// household's name) are checked by the API tests (P17, P18).
public class OwnRecipeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();
    private static readonly Guid OtherHouseholdId = Guid.CreateVersion7();

    private static readonly Ingredient Tej = Ingredient.CreateSystem(Guid.CreateVersion7(), "tej", "tej", Dimension.Volume, Category.Dairy);
    private static readonly Ingredient Kakao = Ingredient.CreateSystem(Guid.CreateVersion7(), "kakaopor", "kakaópor", Dimension.Mass, Category.DryGoods);
    private static readonly Ingredient Cukor = Ingredient.CreateSystem(Guid.CreateVersion7(), "cukor", "cukor", Dimension.Mass, Category.DryGoods);
    private static readonly Ingredient Viz = Ingredient.CreateSystem(Guid.CreateVersion7(), "viz", "víz", Dimension.Volume, Category.Beverages);
    private static readonly Ingredient So = Ingredient.CreateSystem(Guid.CreateVersion7(), "so", "só", Dimension.Mass, Category.DryGoods);

    private readonly FakeIngredients _ingredients = new(Tej, Kakao, Cukor, Viz, So);
    private readonly FakeCatalog _catalog = new();

    // ---- Domain (ADR-0004: the invariants hold for any caller) ----

    [Fact]
    public void An_own_recipe_belongs_to_the_household_and_stores_base_units()
    {
        var (recipe, lines) = Recipe.CreateOwn(HouseholdId, "  Kakaó   tejjel ", 2, "Melegítsd fel.",
            [new RecipeLine(Tej, 5, Unit.Dl), new RecipeLine(Kakao, 2, Unit.Dkg), new RecipeLine(Viz, 1, Unit.Dl)], Now);

        (recipe.HouseholdId, recipe.Name, recipe.Servings, recipe.Instructions, recipe.Source, recipe.SeedKey, recipe.CreatedAt)
            .Should().Be(((Guid?)HouseholdId, "Kakaó tejjel", 2, "Melegítsd fel.", RecipeSource.User, (string?)null, Now));
        lines.Select(l => (l.RecipeId, l.IngredientId, l.Quantity, l.EnteredUnit)).Should().Equal(
            (recipe.Id, Tej.Id, 500m, Unit.Dl), (recipe.Id, Kakao.Id, 20m, Unit.Dkg), (recipe.Id, Viz.Id, 100m, Unit.Dl));
    }

    [Fact]
    public void An_own_recipe_of_staples_only_is_rejected()
    {
        var create = () => Recipe.CreateOwn(HouseholdId, "Sós víz", 1, "", [new RecipeLine(Viz, 1, Unit.L), new RecipeLine(So, 5, Unit.G)], Now);

        create.Should().Throw<ArgumentException>("M9: at least one non-staple ingredient");
    }

    [Fact]
    public void An_ingredient_twice_in_an_own_recipe_is_rejected()
    {
        var create = () => Recipe.CreateOwn(HouseholdId, "Tej", 1, "", [new RecipeLine(Tej, 1, Unit.Dl), new RecipeLine(Tej, 2, Unit.Dl)], Now);

        create.Should().Throw<ArgumentException>("M9: an ingredient once per recipe");
    }

    [Fact]
    public void Another_households_ingredient_cannot_go_in_an_own_recipe()
    {
        var foreign = Ingredient.CreateOwn(OtherHouseholdId, "zabtej", Dimension.Volume, Category.Beverages);

        var create = () => Recipe.CreateOwn(HouseholdId, "Zabtejes kakaó", 1, "", [new RecipeLine(foreign, 250, Unit.Ml)], Now);

        create.Should().Throw<ArgumentException>("QA-1, the Domain guard deferred from S2");
    }

    [Fact]
    public void An_own_ingredient_keeps_its_tidy_name_and_is_unique_by_its_normalized_one()
    {
        var zabtej = Ingredient.CreateOwn(HouseholdId, "  Zab  tej ", Dimension.Volume, Category.Beverages);

        (zabtej.HouseholdId, zabtej.Name, zabtej.NormalizedName, zabtej.Dimension, zabtej.DefaultCategory, zabtej.SeedKey, zabtej.IsStaple)
            .Should().Be(((Guid?)HouseholdId, "Zab tej", "zab tej", Dimension.Volume, Category.Beverages, (string?)null, false));
    }

    // ---- CreateRecipe (N3) ----

    public static TheoryData<string, string, CreateRecipeRequest> InvalidRecipes()
    {
        var tooMany = Enumerable.Range(0, 51).Select(_ => Row(Guid.CreateVersion7(), 1, "g")).ToArray();
        return new()
        {
            { "name", RecipeInput.NameMessage, RecipeRequest(name: "   ") },
            { "name", RecipeInput.NameMessage, RecipeRequest(name: null) },
            { "name", RecipeInput.NameMessage, RecipeRequest(name: new string('a', 101)) },
            { "servings", CookingInput.ServingsMessage, RecipeRequest(servings: 0) },
            { "servings", CookingInput.ServingsMessage, RecipeRequest(servings: 101) },
            { "servings", CookingInput.ServingsMessage, RecipeRequest(servings: null) },
            { "instructions", RecipeInput.InstructionsMessage, RecipeRequest(instructions: new string('a', 4001)) },
            { "ingredients", RecipeInput.NeedsIngredientMessage, RecipeRequest(rows: []) },
            { "ingredients", RecipeInput.TooManyMessage, RecipeRequest(rows: tooMany) },
            { "ingredients[0].ingredientId", PantryInput.IngredientMessage, RecipeRequest(rows: [Row(null, 5, "dl")]) },
            { "ingredients[0].ingredientId", PantryInput.IngredientMessage, RecipeRequest(rows: [null]) },
            { "ingredients[1].ingredientId", RecipeInput.DuplicateMessage, RecipeRequest(rows: [Row(Tej.Id, 5, "dl"), Row(Tej.Id, 1, "dl")]) },
            { "ingredients[0].amount", PantryInput.AmountMessage, RecipeRequest(rows: [Row(Tej.Id, 0, "dl")]) },
            { "ingredients[0].amount", PantryInput.AmountMessage, RecipeRequest(rows: [Row(Tej.Id, 100_001, "ml")]) },
            { "ingredients[0].amount", PantryInput.AmountMessage, RecipeRequest(rows: [Row(Tej.Id, 0.0001m, "l")]) },
            { "ingredients[0].amount", PantryInput.AmountMessage, RecipeRequest(rows: [Row(Tej.Id, null, "dl")]) },
            { "ingredients[0].unit", PantryInput.UnitMessage, RecipeRequest(rows: [Row(Tej.Id, 5, "pohár")]) },
            { "ingredients[0].unit", PantryInput.UnitForMessage("tej", Dimension.Volume), RecipeRequest(rows: [Row(Tej.Id, 5, "g")]) },
            { "ingredients", RecipeInput.NeedsIngredientMessage, RecipeRequest(rows: [Row(Viz.Id, 1, "l"), Row(So.Id, 5, "g")]) },
        };
    }

    [Theory]
    [MemberData(nameof(InvalidRecipes))]
    public async Task An_invalid_own_recipe_is_rejected_with_a_field_error_and_nothing_is_saved(string field, string message,
        CreateRecipeRequest request)
    {
        var error = await FluentActions.Awaiting(() => CreateRecipeAsync(request)).Should().ThrowAsync<ValidationException>();

        error.Which.Errors.Should().ContainKey(field).WhoseValue.Should().Contain(message);
        _catalog.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task A_missing_body_is_a_validation_error()
    {
        await FluentActions.Awaiting(() => CreateRecipeAsync(null)).Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task P17_another_households_ingredient_is_reported_like_a_missing_one()
    {
        var foreign = Ingredient.CreateOwn(OtherHouseholdId, "zabtej", Dimension.Volume, Category.Beverages);
        _ingredients.Add(foreign);

        var error = await FluentActions.Awaiting(() => CreateRecipeAsync(RecipeRequest(rows: [Row(foreign.Id, 250, "ml")])))
            .Should().ThrowAsync<NotFoundException>();

        error.Which.Code.Should().Be("INGREDIENT_NOT_FOUND");
        _catalog.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task A_valid_own_recipe_is_saved_and_answered_with_its_row()
    {
        var zabtej = Ingredient.CreateOwn(HouseholdId, "zabtej", Dimension.Volume, Category.Beverages);
        _ingredients.Add(zabtej);

        var response = await CreateRecipeAsync(RecipeRequest(name: " Zabtejes  kakaó", servings: 1, instructions: null,
            rows: [Row(zabtej.Id, 250, "ml"), Row(Kakao.Id, 1, "dkg"), Row(Cukor.Id, 5, "g")]));

        var (recipe, lines) = _catalog.Added.Single();
        (recipe.HouseholdId, recipe.Name, recipe.Servings, recipe.Instructions, recipe.CreatedAt)
            .Should().Be(((Guid?)HouseholdId, "Zabtejes kakaó", 1, "", Now));
        lines.Select(l => (l.IngredientId, l.Quantity)).Should().Equal((zabtej.Id, 250m), (Kakao.Id, 10m), (Cukor.Id, 5m));
        response.Should().BeEquivalentTo(new RecipeResponse(recipe.Id, "Zabtejes kakaó", 1, Own: true, ["cukor", "kakaópor", "zabtej"]));
        _catalog.Saves.Should().Be(1);
    }

    // ---- ListRecipes (N2) ----

    [Fact]
    public async Task The_recipes_and_their_ingredients_are_in_Hungarian_alphabetical_order()
    {
        _catalog.Listed.AddRange(
        [
            new RecipeResponse(Guid.CreateVersion7(), "Zabtejes kakaó", 1, Own: true, ["zabtej", "cukor"]),
            new RecipeResponse(Guid.CreateVersion7(), "Áfonyás palacsinta", 4, Own: true, ["tojás", "áfonya", "finomliszt"]),
            new RecipeResponse(Guid.CreateVersion7(), "Babgulyás", 4, Own: false, ["szárazbab"]),
        ]);

        var result = await new ListRecipes(_catalog, new FixedHousehold(HouseholdId)).ExecuteAsync(CancellationToken.None);

        result.Select(r => r.Name).Should().Equal("Áfonyás palacsinta", "Babgulyás", "Zabtejes kakaó");
        result[0].Ingredients.Should().Equal("áfonya", "finomliszt", "tojás");
        _catalog.ListedFor.Should().Equal(HouseholdId);
    }

    // ---- ArchiveRecipe (N4) ----

    [Fact]
    public async Task Archiving_a_visible_recipe_records_the_household_and_the_time()
    {
        var recipes = new FakeRecipes();
        var lecso = recipes.System();

        await ArchiveAsync(recipes, lecso);

        _catalog.Archives.Should().Equal(new Dictionary<(Guid, Guid), DateTimeOffset> { [(HouseholdId, lecso)] = Now });
    }

    [Fact]
    public async Task Archiving_an_archived_recipe_again_changes_nothing()
    {
        var recipes = new FakeRecipes();
        var lecso = recipes.System();
        recipes.Archived.Add(lecso);

        await ArchiveAsync(recipes, lecso);

        _catalog.Archives.Should().BeEmpty();
    }

    [Fact]
    public async Task Another_households_own_recipe_cannot_be_archived()
    {
        var recipes = new FakeRecipes();
        var foreign = recipes.Own(OtherHouseholdId);

        var error = await FluentActions.Awaiting(() => ArchiveAsync(recipes, foreign)).Should().ThrowAsync<NotFoundException>();

        error.Which.Code.Should().Be("RECIPE_NOT_FOUND");
        _catalog.Archives.Should().BeEmpty();
    }

    // ---- CreateIngredient (N7, M11) ----

    [Fact]
    public async Task An_own_ingredient_is_created_with_its_tidy_name()
    {
        var own = new FakeOwnIngredients(Tej);

        var response = await CreateIngredientAsync(own, new CreateIngredientRequest { Name = " Zab  tej", Dimension = "volume", DefaultCategory = "beverages" });

        var saved = own.Added.Single();
        (saved.HouseholdId, saved.Name).Should().Be(((Guid?)HouseholdId, "Zab tej"));
        response.Should().Be(new IngredientResponse(saved.Id, "Zab tej", Dimension.Volume, Category.Beverages, Minimum: null));
        own.Saves.Should().Be(1);
    }

    [Theory]
    [InlineData("tej")]
    [InlineData(" TEJ ")]
    [InlineData("Tej")]
    public async Task M11_a_name_matching_a_visible_ingredient_after_normalizing_is_taken(string name)
    {
        var own = new FakeOwnIngredients(Tej);

        var error = await FluentActions.Awaiting(() => CreateIngredientAsync(own,
            new CreateIngredientRequest { Name = name, Dimension = "volume", DefaultCategory = "dairy" })).Should().ThrowAsync<ConflictException>();

        error.Which.Code.Should().Be("INGREDIENT_NAME_TAKEN");
        own.Asked.Should().Equal((HouseholdId, "tej"));
        own.Added.Should().BeEmpty();
    }

    public static TheoryData<string?, string?, string?, string, string> InvalidIngredients() => new()
    {
        { "  ", "volume", "beverages", "name", IngredientInput.NameMessage },
        { null, "volume", "beverages", "name", IngredientInput.NameMessage },
        { new string('a', 101), "volume", "beverages", "name", IngredientInput.NameMessage },
        { "zabtej", "weight", "beverages", "dimension", IngredientInput.DimensionMessage },
        { "zabtej", null, "beverages", "dimension", IngredientInput.DimensionMessage },
        { "zabtej", "volume", "drinks", "defaultCategory", PantryInput.CategoryMessage },
        { "zabtej", "volume", null, "defaultCategory", PantryInput.CategoryMessage },
    };

    [Theory]
    [MemberData(nameof(InvalidIngredients))]
    public async Task An_invalid_own_ingredient_is_rejected_with_a_field_error(string? name, string? dimension, string? category, string field,
        string message)
    {
        var own = new FakeOwnIngredients();

        var error = await FluentActions.Awaiting(() => CreateIngredientAsync(own,
            new CreateIngredientRequest { Name = name, Dimension = dimension, DefaultCategory = category })).Should().ThrowAsync<ValidationException>();

        error.Which.Errors[field].Should().Equal(message);
        own.Added.Should().BeEmpty();
    }

    // ---- helpers ----

    private static CreateRecipeRequest RecipeRequest(string? name = "Kakaó", int? servings = 2, string? instructions = "Keverd ki.",
        RecipeIngredientRequest?[]? rows = null) => new()
        {
            Name = name,
            Servings = servings,
            Instructions = instructions,
            Ingredients = rows ?? [Row(Tej.Id, 5, "dl"), Row(Kakao.Id, 2, "dkg")],
        };

    private static RecipeIngredientRequest Row(Guid? ingredientId, decimal? amount, string? unit) =>
        new() { IngredientId = ingredientId, Amount = amount, Unit = unit };

    private Task<RecipeResponse> CreateRecipeAsync(CreateRecipeRequest? request) =>
        new CreateRecipe(_ingredients, _catalog, new FixedHousehold(HouseholdId), new FixedTime(Now)).ExecuteAsync(request, CancellationToken.None);

    private Task ArchiveAsync(FakeRecipes recipes, Guid recipeId) =>
        new ArchiveRecipe(recipes, _catalog, new FixedHousehold(HouseholdId), new FixedTime(Now)).ExecuteAsync(recipeId, CancellationToken.None);

    private static Task<IngredientResponse> CreateIngredientAsync(FakeOwnIngredients own, CreateIngredientRequest request) =>
        new CreateIngredient(own, new FixedHousehold(HouseholdId)).ExecuteAsync(request, CancellationToken.None);

    private sealed class FixedHousehold(Guid id) : ICurrentHousehold
    {
        public Guid HouseholdId => id;
    }

    // Visible: system ingredients and the household's own ones, not retired (ADR-0003).
    private sealed class FakeIngredients(params Ingredient[] initial) : IIngredientRepository
    {
        private readonly List<Ingredient> _all = [.. initial];

        public void Add(Ingredient ingredient) => _all.Add(ingredient);

        public Task<IReadOnlyList<Ingredient>> ListVisibleAsync(Guid householdId, string? normalizedSearch, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Ingredient?> FindVisibleAsync(Guid householdId, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_all.SingleOrDefault(i => i.Id == id && (i.HouseholdId == null || i.HouseholdId == householdId) && i.RetiredAt == null));
    }

    private sealed class FakeCatalog : IRecipeCatalog
    {
        public List<RecipeResponse> Listed { get; } = [];

        public List<Guid> ListedFor { get; } = [];

        public List<(Recipe Recipe, IReadOnlyList<RecipeIngredient> Lines)> Added { get; } = [];

        public Dictionary<(Guid HouseholdId, Guid RecipeId), DateTimeOffset> Archives { get; } = [];

        public int Saves { get; private set; }

        public Task<IReadOnlyList<RecipeResponse>> ListAsync(Guid householdId, CancellationToken cancellationToken)
        {
            ListedFor.Add(householdId);
            return Task.FromResult<IReadOnlyList<RecipeResponse>>(Listed);
        }

        public void Add(Recipe recipe, IReadOnlyList<RecipeIngredient> ingredients) => Added.Add((recipe, ingredients));

        public Task ArchiveAsync(Guid householdId, Guid recipeId, DateTimeOffset archivedAt, CancellationToken cancellationToken)
        {
            Archives.TryAdd((householdId, recipeId), archivedAt);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saves++;
            return Task.CompletedTask;
        }
    }

    // System recipes are visible to every household, own ones to their household only.
    private sealed class FakeRecipes : IRecipeRepository
    {
        private readonly Dictionary<Guid, Guid?> _owners = [];

        public HashSet<Guid> Archived { get; } = [];

        public Guid System() => Register(null);

        public Guid Own(Guid householdId) => Register(householdId);

        public Task<IReadOnlyList<RecipeCandidate>> ListCandidatesAsync(Guid householdId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RecipeDetails?> FindAsync(Guid householdId, Guid recipeId, CancellationToken cancellationToken) =>
            Task.FromResult(_owners.TryGetValue(recipeId, out var owner) && (owner == null || owner == householdId)
                ? new RecipeDetails(new RecipeCandidate(recipeId, "recept", 4, []), "", Archived.Contains(recipeId))
                : null);

        private Guid Register(Guid? owner)
        {
            var id = Guid.CreateVersion7();
            _owners[id] = owner;
            return id;
        }
    }

    // NameTakenAsync answers from the visible names; the use case must ask with the normalized name (M11).
    private sealed class FakeOwnIngredients(params Ingredient[] visible) : IOwnIngredientRepository
    {
        public List<(Guid HouseholdId, string NormalizedName)> Asked { get; } = [];

        public List<Ingredient> Added { get; } = [];

        public int Saves { get; private set; }

        public Task<bool> NameTakenAsync(Guid householdId, string normalizedName, CancellationToken cancellationToken)
        {
            Asked.Add((householdId, normalizedName));
            return Task.FromResult(visible.Any(i => i.NormalizedName == normalizedName));
        }

        public void Add(Ingredient ingredient) => Added.Add(ingredient);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saves++;
            return Task.CompletedTask;
        }
    }
}
