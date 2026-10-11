using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KamraApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Servings = table.Column<int>(type: "integer", nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    SeedKey = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RetiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.CheckConstraint("CK_Recipes_Servings_Positive", "\"Servings\" > 0");
                    table.ForeignKey(
                        name: "FK_Recipes_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    EnteredUnit = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => new { x.RecipeId, x.IngredientId });
                    table.CheckConstraint("CK_RecipeIngredients_Quantity_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Recipes",
                columns: new[] { "Id", "CreatedAt", "HouseholdId", "Instructions", "Name", "RetiredAt", "SeedKey", "Servings", "Source" },
                values: new object[,]
                {
                    { new Guid("01a12a66-c629-7eea-a91a-3998c61b506e"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A hagymát megtisztítjuk, felaprítjuk, és az olajon üvegesre pároljuk. Hozzáadjuk a csíkokra vágott paprikát, és néhány percig pirítjuk. Belekerül a felkockázott paradicsom és a só, majd fedő alatt, közepes lángon addig főzzük, amíg a zöldségek megpuhulnak és a leve besűrűsödik.", "Lecsó", null, "lecso", 4, "Seed" },
                    { new Guid("01a12a66-c62a-78fc-8abb-f65daf385a30"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A tésztát sós vízben kifőzzük és leszűrjük. A szalonnát felkockázzuk, és a zsírját kisütjük; a pörcöt félretesszük. A tésztát a zsírral összeforgatjuk, a tetejére morzsolt túró, tejföl és a pörc kerül.", "Túrós csusza", null, "turos-csusza", 4, "Seed" },
                    { new Guid("01a12a66-c62b-736b-9d11-e73ca344ab68"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A tojást a tejjel, a vízzel és egy csipet sóval összekeverjük, majd csomómentesre dolgozzuk bele a lisztet és az olaj felét. Fél órát pihentetjük. Forró, kevés olajjal kikent serpenyőben vékony palacsintákat sütünk, mindkét oldalukat aranybarnára.", "Palacsinta", null, "palacsinta", 4, "Seed" },
                    { new Guid("01a12a66-c62c-70f3-99c5-e37c3007d5fb"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A tojásokat egy tálban sóval, borssal felverjük. A vajat serpenyőben felolvasztjuk, ráöntjük a tojást, és alacsony lángon, folyamatosan kevergetve addig sütjük, amíg krémesre szilárdul.", "Rántotta", null, "rantotta", 2, "Seed" },
                    { new Guid("01a12a66-c62d-74a2-9140-cf77c5877302"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A hagymát az olajon megdinszteljük, levesszük a tűzről, és elkeverjük benne a pirospaprikát. Hozzáadjuk a felkockázott burgonyát és a karikára vágott kolbászt, felöntjük annyi vízzel, hogy épp ellepje, megsózzuk, és fedő alatt puhára főzzük.", "Paprikás krumpli", null, "paprikas-krumpli", 4, "Seed" }
                });

            migrationBuilder.InsertData(
                table: "RecipeIngredients",
                columns: new[] { "IngredientId", "RecipeId", "EnteredUnit", "Quantity" },
                values: new object[,]
                {
                    { new Guid("01a126a0-f496-7926-80c2-fffa4640a3c7"), new Guid("01a12a66-c629-7eea-a91a-3998c61b506e"), "G", 500m },
                    { new Guid("01a126a0-f497-711a-a974-dff897cec5ad"), new Guid("01a12a66-c629-7eea-a91a-3998c61b506e"), "G", 300m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a12a66-c629-7eea-a91a-3998c61b506e"), "G", 150m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a12a66-c629-7eea-a91a-3998c61b506e"), "Ml", 30m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a12a66-c629-7eea-a91a-3998c61b506e"), "G", 5m },
                    { new Guid("01a126a0-f49b-7dc6-bf52-af8623e29f9b"), new Guid("01a12a66-c62a-78fc-8abb-f65daf385a30"), "G", 400m },
                    { new Guid("01a126a0-f49c-7f22-8df0-24c8b87a3b6d"), new Guid("01a12a66-c62a-78fc-8abb-f65daf385a30"), "G", 500m },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a12a66-c62a-78fc-8abb-f65daf385a30"), "G", 200m },
                    { new Guid("01a126a0-f49e-7d6a-b637-443ba18a4984"), new Guid("01a12a66-c62a-78fc-8abb-f65daf385a30"), "G", 150m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a12a66-c62a-78fc-8abb-f65daf385a30"), "G", 10m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a12a66-c62b-736b-9d11-e73ca344ab68"), "Ml", 30m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a12a66-c62b-736b-9d11-e73ca344ab68"), "G", 200m },
                    { new Guid("01a126a0-f4a0-7528-a0fa-827975890a2c"), new Guid("01a12a66-c62b-736b-9d11-e73ca344ab68"), "Dl", 500m },
                    { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), new Guid("01a12a66-c62b-736b-9d11-e73ca344ab68"), "Db", 2m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a12a66-c62b-736b-9d11-e73ca344ab68"), "G", 2m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a12a66-c62b-736b-9d11-e73ca344ab68"), "Dl", 100m },
                    { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), new Guid("01a12a66-c62c-70f3-99c5-e37c3007d5fb"), "Db", 3m },
                    { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a12a66-c62c-70f3-99c5-e37c3007d5fb"), "G", 20m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a12a66-c62c-70f3-99c5-e37c3007d5fb"), "G", 2m },
                    { new Guid("01a126a0-f4a6-79c2-a6b4-083b402ee44a"), new Guid("01a12a66-c62c-70f3-99c5-e37c3007d5fb"), "G", 1m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a12a66-c62d-74a2-9140-cf77c5877302"), "G", 150m },
                    { new Guid("01a126a0-f499-70f0-baca-644f79523c11"), new Guid("01a12a66-c62d-74a2-9140-cf77c5877302"), "Kg", 1000m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a12a66-c62d-74a2-9140-cf77c5877302"), "Ml", 30m },
                    { new Guid("01a126a0-f4a3-761c-ba7a-3ba388ba03e9"), new Guid("01a12a66-c62d-74a2-9140-cf77c5877302"), "G", 150m },
                    { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a12a66-c62d-74a2-9140-cf77c5877302"), "G", 10m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a12a66-c62d-74a2-9140-cf77c5877302"), "G", 10m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a12a66-c62d-74a2-9140-cf77c5877302"), "Dl", 500m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_HouseholdId",
                table: "Recipes",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_SeedKey",
                table: "Recipes",
                column: "SeedKey",
                unique: true,
                filter: "\"SeedKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "Recipes");
        }
    }
}
