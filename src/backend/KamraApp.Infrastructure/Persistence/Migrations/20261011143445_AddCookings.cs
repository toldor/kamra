using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KamraApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CookingId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CookedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cookings", x => x.Id);
                    table.CheckConstraint("CK_Cookings_Servings_Positive", "\"Servings\" > 0");
                    table.ForeignKey(
                        name: "FK_Cookings_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cookings_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CookingLines",
                columns: table => new
                {
                    CookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    UsedQuantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    Depleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookingLines", x => new { x.CookingId, x.IngredientId });
                    table.CheckConstraint("CK_CookingLines_UsedQuantity_NonNegative", "\"UsedQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_CookingLines_Cookings_CookingId",
                        column: x => x.CookingId,
                        principalTable: "Cookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CookingLines_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CookingId",
                table: "StockMovements",
                column: "CookingId");

            migrationBuilder.CreateIndex(
                name: "IX_CookingLines_IngredientId",
                table: "CookingLines",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_Cookings_HouseholdId_RequestId",
                table: "Cookings",
                columns: new[] { "HouseholdId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cookings_RecipeId",
                table: "Cookings",
                column: "RecipeId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Cookings_CookingId",
                table: "StockMovements",
                column: "CookingId",
                principalTable: "Cookings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Cookings_CookingId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "CookingLines");

            migrationBuilder.DropTable(
                name: "Cookings");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CookingId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CookingId",
                table: "StockMovements");
        }
    }
}
