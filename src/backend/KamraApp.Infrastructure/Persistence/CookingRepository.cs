using KamraApp.Application.Cookings;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Pantry;

namespace KamraApp.Infrastructure.Persistence;

public sealed class CookingRepository(KamraDbContext db) : ICookingRepository
{
    public Task<Cooking?> FindByRequestAsync(Guid householdId, Guid requestId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"red phase {db.GetHashCode()}");

    public void Add(Cooking cooking, IEnumerable<StockMovement> movements) => throw new NotImplementedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
