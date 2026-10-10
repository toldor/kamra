using KamraApp.Application.Pantry;
using KamraApp.Domain.Pantry;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// Every query filters on the household (QA-1); a depleted item (Quantity 0) is never returned.
public sealed class PantryRepository(KamraDbContext db) : IPantryRepository
{
    public async Task<IReadOnlyList<PantryEntry>> ListAsync(Guid householdId, PantryFilter filter, CancellationToken cancellationToken)
    {
        var query = from item in db.PantryItems.AsNoTracking()
                    join ingredient in db.Ingredients on item.IngredientId equals ingredient.Id
                    where item.HouseholdId == householdId && item.Quantity > 0
                    select new { Item = item, ingredient.Name, ingredient.NormalizedName };
        if (filter.NormalizedSearch is { } search)
        {
            query = query.Where(x => x.NormalizedName.Contains(search));
        }

        if (filter.Category is { } category)
        {
            query = query.Where(x => x.Item.Category == category);
        }

        if (filter.SoonExpiringOn is { } today)
        {
            var lastDay = today.AddDays(Expiry.SoonExpiringDays);
            query = query.Where(x => x.Item.ExpiryDate >= today && x.Item.ExpiryDate <= lastDay);
        }

        var rows = await query.OrderBy(x => x.Item.ExpiryDate).ThenBy(x => x.Name).ThenBy(x => x.Item.Id).ToListAsync(cancellationToken);
        return [.. rows.Select(x => new PantryEntry(x.Item, x.Name))];
    }

    public async Task<PantryEntry?> FindForUpdateAsync(Guid householdId, Guid id, uint expectedVersion, CancellationToken cancellationToken)
    {
        var row = await (from item in db.PantryItems
                         join ingredient in db.Ingredients on item.IngredientId equals ingredient.Id
                         where item.Id == id && item.HouseholdId == householdId && item.Quantity > 0
                         select new { Item = item, ingredient.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        // ADR-0008: the UPDATE runs with WHERE xmin = <the version the client saw>, so a change made since
        // then (another tab, a cooking) makes the save fail instead of being overwritten.
        db.Entry(row.Item).Property(p => p.Version).OriginalValue = expectedVersion;
        return new PantryEntry(row.Item, row.Name);
    }

    public void Add(PantryItem item, StockMovement movement)
    {
        db.PantryItems.Add(item);
        db.StockMovements.Add(movement);
    }

    public void AddMovement(StockMovement movement) => db.StockMovements.Add(movement);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw PantryErrors.ItemModified();
        }
    }
}
