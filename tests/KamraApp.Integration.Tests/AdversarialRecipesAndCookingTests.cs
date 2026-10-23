using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Infrastructure.Persistence;
using KamraApp.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// Ellenséges integrációs tesztek – US-3 saját recept, saját hozzávaló és archiválás,
// valamint ezek kapcsolata az ajánlással és a főzéssel.
// Minden teszt egy mondatban leírja a modellezett támadást és a várt helyes viselkedést.
[Collection("api")]
public class AdversarialRecipesAndCookingTests(KamraApiFactory factory)
{
    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);

    // ──────────────────────────────────────────────────────────────
    // 1. IDOR: más háztartás saját hozzávalójának hivatkozása receptben, pantryban és minimumszintnél
    // ──────────────────────────────────────────────────────────────

    // Modellezett támadás: B háztartás a saját kérésében A háztartás privát hozzávalójának ID-ját használja
    // receptlétrehozásban, készlettételben és minimumszintben – mindegyik esetben a válasznak
    // megkülönböztethetetlennek kell lennie a nem létezőtől (404 INGREDIENT_NOT_FOUND).
    [Fact]
    public async Task IDOR_OtherHouseholdsOwnIngredient_IsIndistinguishableFromNotFound_InRecipeAndPantryAndMinimum()
    {
        // A háztartás létrehozza saját hozzávalóját
        var a = await ApiClient.SignedInAsync(factory);
        var aIngredientId = await CreateOwnIngredientIdAsync(a, "mangosztán", "mass", "fruit");

        // B háztartás (támadó) kísérli meg az A ID-jának felhasználását
        var b = await ApiClient.SignedInAsync(factory);

        // 1a) Recept-létrehozásban: 404 INGREDIENT_NOT_FOUND, ugyanúgy mint egy teljesen véletlenszerű ID-nál
        var recipeResponse = await CreateRecipeAsync(b, "Mangosztán desszert", 2, (aIngredientId, 200m, "g"));
        recipeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(recipeResponse)).GetProperty("code").GetString()
            .Should().Be("INGREDIENT_NOT_FOUND");

        var nonExistentId = Guid.CreateVersion7();
        var recipeNotExist = await CreateRecipeAsync(b, "Phantom", 2, (nonExistentId, 200m, "g"));
        recipeNotExist.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(recipeNotExist)).GetProperty("code").GetString()
            .Should().Be("INGREDIENT_NOT_FOUND",
                because: "egy létező, de idegen hozzávaló-ID 404-je megkülönbözhetetlen kell legyen a nem létezőtől");

        // 1b) Készlettételként: ugyancsak 404, nem szivárog ki, hogy a hozzávaló létezik
        var pantryResponse = await b.PostAsync("/api/v1/pantry-items",
            new { ingredientId = aIngredientId, amount = 1m, unit = "kg", expiryDate = Today.AddDays(7) });
        pantryResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(pantryResponse)).GetProperty("code").GetString()
            .Should().Be("INGREDIENT_NOT_FOUND");

        // 1c) Minimumszint-beállításban (PUT /ingredients/{id}/minimum): szintén 404
        var minResponse = await b.Http.PutAsJsonAsync($"/api/v1/ingredients/{aIngredientId}/minimum",
            new { amount = 500m, unit = "g" }, TestContext.Current.CancellationToken);
        minResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 1d) B hozzávaló-listájában A hozzávalója nem szerepel
        var bIngredients = (await b.GetJsonAsync("/api/v1/ingredients?search=mangosztán")).GetArrayLength();
        bIngredients.Should().Be(0, because: "B háztartás ne lásson A privát hozzávalóiból semmit");
    }

    // ──────────────────────────────────────────────────────────────
    // 2. Jogosultság archiválásnál: más háztartás saját receptje, nem létező recept
    // ──────────────────────────────────────────────────────────────

    // Modellezett támadás: B megpróbálja archiválni A saját receptjét vagy egy teljesen kitalált GUID-ot;
    // mindkét esetben 404 RECIPE_NOT_FOUND-ot kap (nem 403-at), és A ajánlásaiban a recept megmarad.
    [Fact]
    public async Task Archive_OtherHouseholdsOwnRecipeOrUnknownId_Returns404_WithoutLeakingExistence()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var (aRecipeId, _) = await SetUpOwnRecipeAsync(a);
        var b = await ApiClient.SignedInAsync(factory);

        var unknownId = Guid.CreateVersion7();

        foreach (var (id, label) in new[] { (aRecipeId, "A saját receptje"), (unknownId, "nem létező ID") })
        {
            var response = await b.PostAsync($"/api/v1/recipes/{id}/archive");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, because: label);
            (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString()
                .Should().Be("RECIPE_NOT_FOUND", because: label);
        }

        // A-nál a recept megmaradt: nincs lenyomata B hatásának
        var aRecipes = await RecipeListAsync(a);
        aRecipes.Should().Contain(r => r.GetProperty("id").GetGuid() == aRecipeId,
            because: "B sikertelen archiválási kísérlete nem hagyhat nyomot A receptlistájában");
    }

    // ──────────────────────────────────────────────────────────────
    // 3. Induló (system) recept archiválásának háztartás-szintű hatása
    // ──────────────────────────────────────────────────────────────

    // Modellezett támadás: A archiválja a „Rántotta" rendszer-receptet; B-nek ez a döntés sem az
    // ajánlásaiban, sem a receptlistájában, sem az előnézetben nem szabad érzékelhető legyen.
    [Fact]
    public async Task Archive_SystemRecipe_AffectsOnlyOwnHousehold_BSeesNoTrace()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var b = await ApiClient.SignedInAsync(factory);
        var rantottaId = RecipeId("Rántotta");

        (await a.PostAsync($"/api/v1/recipes/{rantottaId}/archive")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        // B receptlistájában a Rántotta megmarad
        var bRecipes = await RecipeListAsync(b);
        bRecipes.Should().Contain(r => r.GetProperty("id").GetGuid() == rantottaId,
            because: "A archíválása nem befolyásolhatja B receptlistáját");

        // B előnézete működik (nem kapja az RECIPE_ARCHIVED hibát)
        var bPreview = await b.Http.GetAsync($"/api/v1/recipes/{rantottaId}/cooking-preview",
            TestContext.Current.CancellationToken);
        bPreview.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "A archívuma nem okozhat RECIPE_ARCHIVED hibát B-nél");

        // A-nál valóban archivált
        var aRecipes = await RecipeListAsync(a);
        aRecipes.Should().NotContain(r => r.GetProperty("id").GetGuid() == rantottaId);
        var aPreview = await a.Http.GetAsync($"/api/v1/recipes/{rantottaId}/cooking-preview",
            TestContext.Current.CancellationToken);
        aPreview.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(aPreview)).GetProperty("code").GetString().Should().Be("RECIPE_ARCHIVED");
    }

    // ──────────────────────────────────────────────────────────────
    // 4. D-c kijátszása: archivált recept főzése új requestId-val
    // ──────────────────────────────────────────────────────────────

    // Modellezett támadás: Archiválás után új requestId-val próbálják megkerülni a letiltást;
    // helyes viselkedés: 409 RECIPE_ARCHIVED, nincs készletlevonás, nincs naplóbejegyzés.
    [Fact]
    public async Task DotC_CookingArchivedRecipeWithNewRequestId_IsRefused_NoStockDeducted()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var (recipeId, ingredientId) = await SetUpOwnRecipeAsync(a);
        var stockBefore = await PantryQuantityAsync(a, ingredientId);

        (await a.PostAsync($"/api/v1/recipes/{recipeId}/archive")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        var attempt = await a.PostAsync("/api/v1/cookings", new
        {
            requestId = Guid.CreateVersion7(),
            recipeId,
            servings = 1,
            used = new[] { new { ingredientId, amount = 250m } },
        });
        attempt.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(attempt)).GetProperty("code").GetString().Should().Be("RECIPE_ARCHIVED");

        // Készlet változatlan
        (await PantryQuantityAsync(a, ingredientId)).Should().Be(stockBefore,
            because: "archivált receptre küldött új főzés nem vonhat le semmit");

        // Naplóban sincs bejegyzés ehhez a recepthez
        var (cookings, movements) = await CookingsAndMovementsAsync(recipeId);
        cookings.Should().Be(0);
        movements.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────
    // 5. D-c kijátszása: ugyanaz a requestId, de más receptet küld
    // ──────────────────────────────────────────────────────────────

    // Modellezett támadás: Az első sikeres főzés requestId-ját újra felküldik másik recepttel;
    // ez idempotencia-ütközés (409 IDEMPOTENCY_CONFLICT), a készlet nem változik meg ismét.
    [Fact]
    public async Task DotC_SameRequestIdWithDifferentRecipeId_IsIdempotencyConflict_NoDoubleDeduction()
    {
        var a = await ApiClient.SignedInAsync(factory);
        await AddLecsoStockAsync(a);
        var requestId = Guid.CreateVersion7();
        var lecsoId = RecipeId("Lecsó");
        var palacintaId = RecipeId("Palacsinta");

        // Első sikeres főzés az előnézet alapértelmezett mennyiségeivel (a used a recept minden nem alaphozzávalóját felsorolja)
        var lecsoPreview = await a.GetJsonAsync($"/api/v1/recipes/{lecsoId}/cooking-preview?servings=4");
        var first = await a.PostAsync("/api/v1/cookings", new
        {
            requestId,
            recipeId = lecsoId,
            servings = 4,
            used = lecsoPreview.GetProperty("lines").EnumerateArray()
                .Select(l => new { ingredientId = l.GetProperty("ingredientId").GetGuid(), amount = l.GetProperty("defaultUsed").GetDecimal() }),
        });
        first.EnsureSuccessStatusCode();
        var pantryAfterFirst = await PantryAsync(a);

        // Ugyanaz a requestId, de más receptet kér
        var preview = await a.GetJsonAsync($"/api/v1/recipes/{palacintaId}/cooking-preview?servings=4");
        var reused = await a.PostAsync("/api/v1/cookings", new
        {
            requestId,
            recipeId = palacintaId,
            servings = 4,
            used = preview.GetProperty("lines").EnumerateArray()
                .Select(l => new { ingredientId = l.GetProperty("ingredientId").GetGuid(), amount = l.GetProperty("defaultUsed").GetDecimal() }),
        });

        reused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(reused)).GetProperty("code").GetString()
            .Should().Be("IDEMPOTENCY_CONFLICT");

        // Készlet nem változott a második kérés után
        (await PantryAsync(a)).Should().BeEquivalentTo(pantryAfterFirst,
            because: "az IDEMPOTENCY_CONFLICT nem vonhat le semmit");
    }

    // ──────────────────────────────────────────────────────────────
    // 6. Névnormalizálás: Unicode-csapdák és határhossz
    // ──────────────────────────────────────────────────────────────

    // Modellezett támadás: Az ütközésvizsgálatot próbálja megkerülni nem törhető szóközzel (U+00A0),
    // extra szóközökkel, ékezet-különbséggel, nagybetűkkel; a rendszer normalizálja ezeket és
    // INGREDIENT_NAME_TAKEN-nel válaszol. A 100- és 101-karakteres névhatárt is ellenőrizzük.
    [Theory]
    [InlineData("zabtej", "zabtej")]        // teljesen egyező
    [InlineData("zabtej", "ZABTEJ")]        // nagybetű
    [InlineData("zabtej", "Zabtej")]        // vegyes eset
    [InlineData("zabtej", " zabtej ")]      // vezető/záró szóköz
    [InlineData("zab tej", "zab\u00A0tej")] // nem törhető szóköz U+00A0
    [InlineData("zab tej", "zab  tej")]     // belső dupla szóköz
    public async Task NameNormalization_TrickyNames_AreRecognizedAsDuplicates(string firstName, string duplicateName)
    {
        var a = await ApiClient.SignedInAsync(factory);
        // Egyedi prefix, hogy ne ütközzön más tesztek adataival
        var prefix = Guid.CreateVersion7().ToString("N")[..6];
        var first = prefix + firstName;
        var dup = prefix + duplicateName.Trim();

        (await CreateIngredientAsync(a, first, "volume", "beverages")).StatusCode
            .Should().Be(HttpStatusCode.Created);

        var dupResponse = await CreateIngredientAsync(a, dup, "volume", "beverages");
        dupResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(dupResponse)).GetProperty("code").GetString()
            .Should().Be("INGREDIENT_NAME_TAKEN",
                because: $"'{duplicateName}' normalizálva ugyanaz mint '{firstName}'");
    }

    [Theory]
    [InlineData(100, true)]   // Pont 100 karakter – érvényes
    [InlineData(101, false)]  // 101 karakter – érvénytelen
    public async Task NameLength_At100And101Characters_AcceptedOrRejectedCorrectly(int length, bool shouldSucceed)
    {
        var a = await ApiClient.SignedInAsync(factory);
        var prefix = Guid.CreateVersion7().ToString("N")[..8];
        var name = prefix + new string('x', length - prefix.Length);
        name.Length.Should().Be(length);

        var response = await CreateIngredientAsync(a, name, "mass", "dairy");

        if (shouldSucceed)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created,
                because: $"{length}-karakteres név elfogadandó");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                because: $"{length}-karakteres név elutasítandó");
            (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString()
                .Should().Be("VALIDATION_FAILED");
        }
    }

    // ──────────────────────────────────────────────────────────────
    // 7. Receptbemenet szélsőértékek
    // ──────────────────────────────────────────────────────────────

    // Modellezett támadás: különböző torzított bemeneteket küld a kliens (0 sor, 51 sor, duplikált
    // hozzávaló, rossz dimenzió, érvénytelen mennyiségek, null sor, 4001 karakteres elkészítés);
    // minden esetben 400 VALIDATION_FAILED-et kell visszakapni, adatbázisba semmi érvénytelen nem kerülhet.
    [Theory]
    [InlineData("zero_rows")]
    [InlineData("fifty_one_rows")]
    [InlineData("duplicate_ingredient")]
    [InlineData("wrong_dimension")]
    [InlineData("amount_zero")]
    [InlineData("amount_negative")]
    [InlineData("amount_too_large")]
    [InlineData("amount_too_precise")]
    [InlineData("null_row")]
    [InlineData("instructions_4001")]
    public async Task RecipeInput_InvalidVariants_ReturnValidationFailed(string variant)
    {
        var a = await ApiClient.SignedInAsync(factory);
        var tejId = await IngredientIdAsync(a, "tej");

        HttpResponseMessage response = variant switch
        {
            "zero_rows" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                ingredients = Array.Empty<object>(),
            }),
            "fifty_one_rows" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                ingredients = Enumerable.Range(0, 51)
                    .Select(_ => new { ingredientId = tejId, amount = 1m, unit = "dl" })
                    .ToArray(),
            }),
            "duplicate_ingredient" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                ingredients = new[]
                {
                    new { ingredientId = tejId, amount = 1m, unit = "dl" },
                    new { ingredientId = tejId, amount = 2m, unit = "dl" },
                },
            }),
            "wrong_dimension" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                // tej volume-típusú, de kg (mass) egységet küldünk
                ingredients = new[] { new { ingredientId = tejId, amount = 1m, unit = "kg" } },
            }),
            "amount_zero" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                ingredients = new[] { new { ingredientId = tejId, amount = 0m, unit = "dl" } },
            }),
            "amount_negative" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                ingredients = new[] { new { ingredientId = tejId, amount = -1m, unit = "dl" } },
            }),
            "amount_too_large" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                ingredients = new[] { new { ingredientId = tejId, amount = 100001m, unit = "dl" } },
            }),
            "amount_too_precise" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                // 4 tizedesjegy (max 3 megengedett alapegységben)
                ingredients = new[] { new { ingredientId = tejId, amount = 1.0001m, unit = "dl" } },
            }),
            "null_row" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                ingredients = new object?[] { null },
            }),
            "instructions_4001" => await a.PostAsync("/api/v1/recipes", new
            {
                name = "Teszt",
                servings = 1,
                instructions = new string('x', 4001),
                ingredients = new[] { new { ingredientId = tejId, amount = 1m, unit = "dl" } },
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: variant);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString()
            .Should().Be("VALIDATION_FAILED", because: variant);
    }

    // ──────────────────────────────────────────────────────────────
    // 8. Hibaválasz-szivárgás
    // ──────────────────────────────────────────────────────────────

    // Modellezett támadás: IDOR és érvénytelen kérések küldése az US-3 végpontokra; a válasz törzse
    // nem tartalmazhat stack trace-t, belső útvonalat, SQL/EF-üzenetet, és más háztartás adatainak nevét.
    [Fact]
    public async Task ErrorResponses_US3Endpoints_DoNotLeakInternalOrCrossHouseholdData()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var aIngredientId = await CreateOwnIngredientIdAsync(a, "titkos fűszer", "mass", "dryGoods");
        var (aRecipeId, _) = await SetUpOwnRecipeAsync(a, ingredientId: aIngredientId, ingredientUnit: "g");

        var b = await ApiClient.SignedInAsync(factory);

        var responses = new List<HttpResponseMessage>
        {
            await CreateRecipeAsync(b, "Titkos leves", 1, (aIngredientId, 100m, "g")),
            await b.PostAsync($"/api/v1/recipes/{aRecipeId}/archive"),
            await b.Http.GetAsync($"/api/v1/recipes/{Guid.CreateVersion7()}/cooking-preview",
                TestContext.Current.CancellationToken),
            await b.PostAsync("/api/v1/recipes", null),
            await b.PostAsync("/api/v1/ingredients", null),
        };

        var leakKeywords = new[]
        {
            "StackTrace", "stack_trace", "Npgsql", "PostgresException", "EntityFrameworkCore",
            "Entity Framework", "KamraApp.Domain", "KamraApp.Infrastructure", ".cs:", "SELECT",
            "INSERT", "UPDATE", "NpgsqlException",
        };

        foreach (var resp in responses)
        {
            var raw = await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            foreach (var keyword in leakKeywords)
            {
                raw.Should().NotContainEquivalentOf(keyword,
                    because: $"a válasz nem szivárogtathat ki belső részletet ('{keyword}')");
            }

            // A más háztartás nevű hozzávalója ne szerepeljen a válaszban
            raw.Should().NotContain("titkos fűszer",
                because: "B háztartás kérésének válasza nem tartalmazhatja A adatainak nevét");

            if (resp.Content.Headers.ContentType?.MediaType?.Contains("problem") == true)
            {
                var problem = JsonDocument.Parse(raw).RootElement;
                problem.TryGetProperty("correlationId", out var corrId).Should().BeTrue();
                corrId.GetString().Should().NotBeNullOrWhiteSpace();
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════

    private static Guid RecipeId(string name) =>
        SeedData.Recipes.Recipes.Single(r => r.Name == name).Id;

    private static async Task<Guid> IngredientIdAsync(ApiClient client, string name) =>
        (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(name)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();

    private static async Task<Guid> CreateOwnIngredientIdAsync(
        ApiClient client, string name, string dimension, string category)
    {
        var response = await CreateIngredientAsync(client, name, dimension, category);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> CreateIngredientAsync(
        ApiClient client, string name, string dimension, string category) =>
        client.PostAsync("/api/v1/ingredients", new { name, dimension, defaultCategory = category });

    private static Task<HttpResponseMessage> CreateRecipeAsync(
        ApiClient client, string name, int servings,
        params (Guid IngredientId, decimal Amount, string Unit)[] rows) =>
        client.PostAsync("/api/v1/recipes", new
        {
            name,
            servings,
            instructions = "Elkészítés.",
            ingredients = rows.Select(r => new { ingredientId = r.IngredientId, amount = r.Amount, unit = r.Unit }),
        });

    // Saját recept felállítása: saját hozzávaló (ha nincs megadva), recept, 1 l/kg készlet.
    private static async Task<(Guid RecipeId, Guid IngredientId)> SetUpOwnRecipeAsync(
        ApiClient client, Guid? ingredientId = null, string ingredientUnit = "ml")
    {
        Guid ingId;
        if (ingredientId.HasValue)
        {
            ingId = ingredientId.Value;
        }
        else
        {
            var uniqueName = "zabtej-" + Guid.CreateVersion7().ToString("N")[..8];
            ingId = await CreateOwnIngredientIdAsync(client, uniqueName, "volume", "beverages");
        }

        var recipeName = "Kakaó-" + Guid.CreateVersion7().ToString("N")[..8];
        var recipe = await CreateRecipeAsync(client, recipeName, 1, (ingId, 250m, ingredientUnit));
        recipe.EnsureSuccessStatusCode();

        // Adj hozzá készletet
        var pantryUnit = ingredientUnit == "ml" ? "l" : "kg";
        (await client.PostAsync("/api/v1/pantry-items", new
        {
            ingredientId = ingId,
            amount = 1m,
            unit = pantryUnit,
            expiryDate = Today.AddDays(7),
        })).EnsureSuccessStatusCode();

        var recipeId = (await recipe.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetGuid();
        return (recipeId, ingId);
    }

    private static async Task<decimal> PantryQuantityAsync(ApiClient client, Guid ingredientId)
    {
        var items = (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray();
        return items.Where(i => i.GetProperty("ingredientId").GetGuid() == ingredientId)
            .Sum(i => i.GetProperty("quantity").GetDecimal());
    }

    private static async Task<List<(Guid IngredientId, decimal Quantity)>> PantryAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray()
            .Select(i => (i.GetProperty("ingredientId").GetGuid(), i.GetProperty("quantity").GetDecimal()))];

    private static async Task<List<JsonElement>> RecipeListAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/recipes")).EnumerateArray()];

    private static async Task AddLecsoStockAsync(ApiClient client)
    {
        foreach (var (name, amount, unit, days) in new[]
        {
            ("paradicsom", 400m, "g", 3), ("paprika", 500m, "g", 2),
            ("vöröshagyma", 300m, "g", 10), ("étolaj", 1m, "l", 90),
        })
        {
            (await client.PostAsync("/api/v1/pantry-items",
                new { ingredientId = await IngredientIdAsync(client, name), amount, unit, expiryDate = Today.AddDays(days) }))
                .EnsureSuccessStatusCode();
        }
    }

    private async Task<(int Cookings, int Movements)> CookingsAndMovementsAsync(Guid recipeId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        var cookings = await db.Cookings
            .Where(c => c.RecipeId == recipeId)
            .Select(c => c.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        var movements = await db.StockMovements.CountAsync(
            m => m.CookingId != null && cookings.Contains(m.CookingId.Value),
            TestContext.Current.CancellationToken);
        return (cookings.Count, movements);
    }
}
