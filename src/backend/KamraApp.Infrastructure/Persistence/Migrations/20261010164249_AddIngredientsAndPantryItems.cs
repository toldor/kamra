using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KamraApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientsAndPantryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NormalizedName = table.Column<string>(type: "text", nullable: false),
                    Dimension = table.Column<string>(type: "text", nullable: false),
                    DefaultCategory = table.Column<string>(type: "text", nullable: false),
                    SeedKey = table.Column<string>(type: "text", nullable: true),
                    RetiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ingredients_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PantryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    EnteredUnit = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryEstimated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PantryItems", x => x.Id);
                    table.CheckConstraint("CK_PantryItems_Quantity_NonNegative", "\"Quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_PantryItems_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PantryItems_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    PantryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Delta = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ExpiryDateAtMovement = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMovements_PantryItems_PantryItemId",
                        column: x => x.PantryItemId,
                        principalTable: "PantryItems",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "DefaultCategory", "Dimension", "HouseholdId", "Name", "NormalizedName", "RetiredAt", "SeedKey" },
                values: new object[,]
                {
                    { new Guid("01a126a0-f496-7926-80c2-fffa4640a3c7"), "Vegetables", "Mass", null, "paprika", "paprika", null, "paprika" },
                    { new Guid("01a126a0-f497-711a-a974-dff897cec5ad"), "Vegetables", "Mass", null, "paradicsom", "paradicsom", null, "paradicsom" },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), "Vegetables", "Mass", null, "vöröshagyma", "vöröshagyma", null, "voroshagyma" },
                    { new Guid("01a126a0-f499-70f0-baca-644f79523c11"), "Vegetables", "Mass", null, "burgonya", "burgonya", null, "burgonya" },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), "DryGoods", "Volume", null, "étolaj", "étolaj", null, "etolaj" },
                    { new Guid("01a126a0-f49b-7dc6-bf52-af8623e29f9b"), "DryGoods", "Mass", null, "száraztészta", "száraztészta", null, "szarazteszta" },
                    { new Guid("01a126a0-f49c-7f22-8df0-24c8b87a3b6d"), "Dairy", "Mass", null, "túró", "túró", null, "turo" },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), "Dairy", "Mass", null, "tejföl", "tejföl", null, "tejfol" },
                    { new Guid("01a126a0-f49e-7d6a-b637-443ba18a4984"), "ProcessedMeat", "Mass", null, "szalonna", "szalonna", null, "szalonna" },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), "DryGoods", "Mass", null, "finomliszt", "finomliszt", null, "finomliszt" },
                    { new Guid("01a126a0-f4a0-7528-a0fa-827975890a2c"), "Dairy", "Volume", null, "tej", "tej", null, "tej" },
                    { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), "Eggs", "Count", null, "tojás", "tojás", null, "tojas" },
                    { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), "Dairy", "Mass", null, "vaj", "vaj", null, "vaj" },
                    { new Guid("01a126a0-f4a3-761c-ba7a-3ba388ba03e9"), "ProcessedMeat", "Mass", null, "kolbász", "kolbász", null, "kolbasz" },
                    { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), "DryGoods", "Mass", null, "őrölt pirospaprika", "őrölt pirospaprika", null, "orolt-pirospaprika" },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), "DryGoods", "Mass", null, "só", "só", null, "so" },
                    { new Guid("01a126a0-f4a6-79c2-a6b4-083b402ee44a"), "DryGoods", "Mass", null, "bors", "bors", null, "bors" },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), "Beverages", "Volume", null, "víz", "víz", null, "viz" },
                    { new Guid("01a126a0-f4a8-73cb-80de-f72ebcb3ba01"), "Bakery", "Count", null, "kenyér", "kenyér", null, "kenyer" },
                    { new Guid("01a126a0-f4a9-7c8f-a377-a19949681cea"), "Bakery", "Count", null, "kifli", "kifli", null, "kifli" },
                    { new Guid("01a126a0-f4aa-7019-8624-c1716ad4de7b"), "Dairy", "Mass", null, "joghurt", "joghurt", null, "joghurt" },
                    { new Guid("01a126a0-f4ab-7527-940b-7acd5b67aa7c"), "Dairy", "Volume", null, "tejszín", "tejszín", null, "tejszin" },
                    { new Guid("01a126a0-f4ac-74eb-8f00-38d37814a809"), "Cheese", "Mass", null, "trappista sajt", "trappista sajt", null, "trappista-sajt" },
                    { new Guid("01a126a0-f4ad-73e7-857a-7396a56d6d75"), "FreshMeatFish", "Mass", null, "csirkemell", "csirkemell", null, "csirkemell" },
                    { new Guid("01a126a0-f4ae-7bad-a26d-3db0c8e0bd77"), "FreshMeatFish", "Mass", null, "darált sertéshús", "darált sertéshús", null, "daralt-serteshus" },
                    { new Guid("01a126a0-f4af-7d94-a469-1140c47e4814"), "ProcessedMeat", "Mass", null, "sonka", "sonka", null, "sonka" },
                    { new Guid("01a126a0-f4b0-7909-8e1b-b4a28be99399"), "DryGoods", "Mass", null, "rizs", "rizs", null, "rizs" },
                    { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), "DryGoods", "Mass", null, "cukor", "cukor", null, "cukor" },
                    { new Guid("01a126a0-f4b2-74ab-b11f-864af17fbd0d"), "Fruit", "Count", null, "alma", "alma", null, "alma" },
                    { new Guid("01a126a0-f4b3-73fd-9f62-f5cab9dafa04"), "Fruit", "Count", null, "banán", "banán", null, "banan" },
                    { new Guid("01a126a0-f4b4-7686-bb4b-9d3298a72b52"), "Fruit", "Count", null, "citrom", "citrom", null, "citrom" },
                    { new Guid("01a126a0-f4b5-7e6f-80b9-dbcb7048edaa"), "Vegetables", "Mass", null, "sárgarépa", "sárgarépa", null, "sargarepa" },
                    { new Guid("01a126a0-f4b6-72dc-9d01-ddf60e84df77"), "Vegetables", "Count", null, "fokhagyma", "fokhagyma", null, "fokhagyma" },
                    { new Guid("01a126a0-f4b7-7546-a5a1-9b55e78e6dc0"), "CannedAndSauces", "Mass", null, "paradicsompüré", "paradicsompüré", null, "paradicsompure" },
                    { new Guid("01a126a0-f4b8-70a8-92e7-887762859205"), "Frozen", "Mass", null, "fagyasztott zöldborsó", "fagyasztott zöldborsó", null, "fagyasztott-zoldborso" },
                    { new Guid("01a126a0-f4b9-7b93-b485-297af03564e8"), "Beverages", "Volume", null, "narancslé", "narancslé", null, "narancsle" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_HouseholdId_NormalizedName",
                table: "Ingredients",
                columns: new[] { "HouseholdId", "NormalizedName" },
                unique: true,
                filter: "\"HouseholdId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_NormalizedName",
                table: "Ingredients",
                column: "NormalizedName",
                unique: true,
                filter: "\"HouseholdId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_SeedKey",
                table: "Ingredients",
                column: "SeedKey",
                unique: true,
                filter: "\"SeedKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PantryItems_HouseholdId_IngredientId_ExpiryDate",
                table: "PantryItems",
                columns: new[] { "HouseholdId", "IngredientId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PantryItems_IngredientId",
                table: "PantryItems",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_HouseholdId",
                table: "StockMovements",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PantryItemId",
                table: "StockMovements",
                column: "PantryItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "PantryItems");

            migrationBuilder.DropTable(
                name: "Ingredients");
        }
    }
}
