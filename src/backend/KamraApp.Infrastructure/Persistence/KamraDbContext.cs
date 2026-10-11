using KamraApp.Domain.Cookings;
using KamraApp.Domain.Households;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Recipes;
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

    public DbSet<Recipe> Recipes => Set<Recipe>();

    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

    public DbSet<Cooking> Cookings => Set<Cooking>();

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
            movement.HasOne<Cooking>().WithMany().HasForeignKey(m => m.CookingId).OnDelete(DeleteBehavior.NoAction);
        });

        // data_model.md, Recipe and RecipeIngredient; the system recipes come from recipes.json (ADR-0005).
        builder.Entity<Recipe>(recipe =>
        {
            recipe.HasKey(r => r.Id);
            recipe.Property(r => r.Id).ValueGeneratedNever();
            recipe.Property(r => r.Source).HasConversion<string>();
            recipe.ToTable(t => t.HasCheckConstraint("CK_Recipes_Servings_Positive", "\"Servings\" > 0"));
            recipe.HasOne<Household>().WithMany().HasForeignKey(r => r.HouseholdId).OnDelete(DeleteBehavior.Cascade);
            recipe.HasIndex(r => r.SeedKey).IsUnique().HasFilter("\"SeedKey\" IS NOT NULL");
            recipe.HasData(SeedData.Recipes.Recipes);
        });

        builder.Entity<RecipeIngredient>(line =>
        {
            line.HasKey(l => new { l.RecipeId, l.IngredientId });
            line.Property(l => l.Quantity).HasPrecision(12, 3);
            line.Property(l => l.EnteredUnit).HasConversion<string>();
            line.ToTable(t => t.HasCheckConstraint("CK_RecipeIngredients_Quantity_Positive", "\"Quantity\" > 0"));
            line.HasOne<Recipe>().WithMany().HasForeignKey(l => l.RecipeId).OnDelete(DeleteBehavior.Cascade);
            line.HasOne<Ingredient>().WithMany().HasForeignKey(l => l.IngredientId).OnDelete(DeleteBehavior.NoAction);
            line.HasData(SeedData.Recipes.Ingredients);
        });

        // data_model.md, Cooking and CookingLine; ADR-0008: (HouseholdId, RequestId) is unique, so a repeated
        // confirmation cannot deduct twice.
        builder.Entity<Cooking>(cooking =>
        {
            cooking.HasKey(c => c.Id);
            cooking.Property(c => c.Id).ValueGeneratedNever();
            cooking.ToTable(t => t.HasCheckConstraint("CK_Cookings_Servings_Positive", "\"Servings\" > 0"));
            cooking.HasOne<Household>().WithMany().HasForeignKey(c => c.HouseholdId).OnDelete(DeleteBehavior.Cascade);
            cooking.HasOne<Recipe>().WithMany().HasForeignKey(c => c.RecipeId).OnDelete(DeleteBehavior.NoAction);
            cooking.HasIndex(c => new { c.HouseholdId, c.RequestId }).IsUnique();
            cooking.HasMany(c => c.Lines).WithOne().HasForeignKey(l => l.CookingId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CookingLine>(line =>
        {
            line.HasKey(l => new { l.CookingId, l.IngredientId });
            line.Property(l => l.RequiredQuantity).HasPrecision(12, 3);
            line.Property(l => l.UsedQuantity).HasPrecision(12, 3);
            line.ToTable("CookingLines", t => t.HasCheckConstraint("CK_CookingLines_UsedQuantity_NonNegative", "\"UsedQuantity\" >= 0"));
            line.HasOne<Ingredient>().WithMany().HasForeignKey(l => l.IngredientId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
