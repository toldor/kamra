using KamraApp.Domain.Households;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KamraApp.Infrastructure.Persistence;

// IdentityUserContext: users, claims, logins and tokens only - the app has no roles (ADR-0006).
public sealed class KamraDbContext(DbContextOptions<KamraDbContext> options)
    : IdentityUserContext<AppUser, Guid>(options)
{
    public DbSet<Household> Households => Set<Household>();

    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    public DbSet<PantryItem> PantryItems => Set<PantryItem>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

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

        // ADR-0003 / data_model.md. Enums are stored as text. Ingredient references use NO ACTION (checked at
        // the end of the statement), so deleting a household can cascade to its items and own ingredients.
        builder.Entity<Ingredient>(ingredient =>
        {
            ingredient.HasKey(i => i.Id);
            ingredient.Property(i => i.Id).ValueGeneratedNever();
            ingredient.Property(i => i.Dimension).HasConversion<string>();
            ingredient.Property(i => i.DefaultCategory).HasConversion<string>();
            ingredient.HasOne<Household>().WithMany().HasForeignKey(i => i.HouseholdId).OnDelete(DeleteBehavior.Cascade);
            // Names are unique among system ingredients and within one household.
            ingredient.HasIndex(i => i.NormalizedName).IsUnique().HasFilter("\"HouseholdId\" IS NULL");
            ingredient.HasIndex(i => new { i.HouseholdId, i.NormalizedName }).IsUnique().HasFilter("\"HouseholdId\" IS NOT NULL");
            ingredient.HasIndex(i => i.SeedKey).IsUnique().HasFilter("\"SeedKey\" IS NOT NULL");
            ingredient.HasData(SeedData.Ingredients);
        });

        builder.Entity<PantryItem>(item =>
        {
            item.HasKey(p => p.Id);
            item.Property(p => p.Id).ValueGeneratedNever();
            item.Property(p => p.Quantity).HasPrecision(12, 3);
            item.Property(p => p.EnteredUnit).HasConversion<string>();
            item.Property(p => p.Category).HasConversion<string>();
            // ADR-0008: the version is PostgreSQL's xmin system column (Npgsql: uint + IsRowVersion).
            item.Property(p => p.Version).IsRowVersion();
            item.ToTable(t => t.HasCheckConstraint("CK_PantryItems_Quantity_NonNegative", "\"Quantity\" >= 0"));
            item.HasOne<Household>().WithMany().HasForeignKey(p => p.HouseholdId).OnDelete(DeleteBehavior.Cascade);
            item.HasOne<Ingredient>().WithMany().HasForeignKey(p => p.IngredientId).OnDelete(DeleteBehavior.NoAction);
            // FEFO deduction and recipe matching read a household's items per ingredient by expiry.
            item.HasIndex(p => new { p.HouseholdId, p.IngredientId, p.ExpiryDate });
        });

        builder.Entity<StockMovement>(movement =>
        {
            movement.HasKey(m => m.Id);
            movement.Property(m => m.Id).ValueGeneratedNever();
            movement.Property(m => m.Delta).HasPrecision(12, 3);
            movement.Property(m => m.Reason).HasConversion<string>();
            movement.HasOne<Household>().WithMany().HasForeignKey(m => m.HouseholdId).OnDelete(DeleteBehavior.Cascade);
            // Append-only log (data_model.md): a pantry item with movements cannot be deleted; a household
            // deletion still removes both through their own household cascades.
            movement.HasOne<PantryItem>().WithMany().HasForeignKey(m => m.PantryItemId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
