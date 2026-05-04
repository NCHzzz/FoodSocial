using System;
using FoodSocial.Dish.Domain;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Dish.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDishCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CuisineType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aliases = table.Column<string>(type: "text", nullable: false),
                    aliases_search_text = table.Column<string>(type: "text", nullable: false),
                    Ingredients = table.Column<string>(type: "text", nullable: false),
                    DietaryFlags = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeprecatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dishes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dishes_Category",
                table: "dishes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_dishes_CuisineType",
                table: "dishes",
                column: "CuisineType");

            migrationBuilder.CreateIndex(
                name: "IX_dishes_NormalizedName",
                table: "dishes",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dishes_Status",
                table: "dishes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dishes");
        }
    }
}
