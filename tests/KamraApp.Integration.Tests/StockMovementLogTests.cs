using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Domain.Pantry;
using KamraApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KamraApp.Integration.Tests;

// data_model.md: the stock movement log is append-only, and a pantry item's row stays while the log
// refers to it - the database itself refuses to delete it.
[Collection("api")]
public class StockMovementLogTests(KamraApiFactory factory)
{
    [Fact]
    public async Task A_pantry_item_with_logged_movements_cannot_be_deleted()
    {
        var client = await ApiClient.CreateAsync(factory);
        await client.RegisterAsync(ApiClient.NewEmail());
        var me = await (await client.MeAsync()).Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var householdId = me.GetProperty("householdId").GetGuid();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        var milk = await db.Ingredients.SingleAsync(i => i.SeedKey == "tej", TestContext.Current.CancellationToken);
        var (item, movement) = PantryItem.Create(householdId, milk, 1m, KamraApp.Domain.Quantities.Unit.L, Category.Dairy,
            expiryDate: null, new DateOnly(2026, 10, 10), DateTimeOffset.UtcNow);
        db.AddRange(item, movement);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var delete = () => db.Database.ExecuteSqlAsync($"""DELETE FROM "PantryItems" WHERE "Id" = {item.Id}""", TestContext.Current.CancellationToken);

        (await delete.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
    }
}
