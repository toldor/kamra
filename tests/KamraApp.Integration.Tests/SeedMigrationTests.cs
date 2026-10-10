using KamraApp.Infrastructure.Persistence;
using KamraApp.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// ADR-0005: the system ingredient list reaches the database only through the migrations (HasData).
[Collection("api")]
public class SeedMigrationTests(KamraApiFactory factory)
{
    [Fact]
    public async Task Migrations_seed_the_ingredient_list()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();

        var seedKeys = await db.Database
            .SqlQuery<string>($"""SELECT "SeedKey" AS "Value" FROM "Ingredients" WHERE "HouseholdId" IS NULL""")
            .ToListAsync(TestContext.Current.CancellationToken);

        seedKeys.Should().BeEquivalentTo(SeedData.Ingredients.Select(i => i.SeedKey));
    }
}
