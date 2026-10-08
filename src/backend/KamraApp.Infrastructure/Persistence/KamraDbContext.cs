using KamraApp.Domain.Households;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// IdentityUserContext: users, claims, logins and tokens only - the app has no roles (ADR-0006).
public sealed class KamraDbContext(DbContextOptions<KamraDbContext> options)
    : IdentityUserContext<AppUser, Guid>(options)
{
    public DbSet<Household> Households => Set<Household>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Household>(household =>
        {
            household.HasKey(h => h.Id);
            household.Property(h => h.Id).ValueGeneratedNever();
            household.HasIndex(h => h.OwnerUserId).IsUnique();
            household.HasOne<AppUser>().WithOne().HasForeignKey<Household>(h => h.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
