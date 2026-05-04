using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Place.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlaceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    AddressText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    NormalizedAddressText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Area = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedArea = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedCity = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedCountry = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    CuisineTypes = table.Column<string>(type: "text", nullable: false),
                    CuisineTypesSearchText = table.Column<string>(type: "text", nullable: false),
                    PriceRange = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_places", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_places_CuisineTypes",
                table: "places",
                column: "CuisineTypes");

            migrationBuilder.CreateIndex(
                name: "IX_places_CuisineTypesSearchText",
                table: "places",
                column: "CuisineTypesSearchText");

            migrationBuilder.CreateIndex(
                name: "IX_places_NormalizedCity_NormalizedArea",
                table: "places",
                columns: new[] { "NormalizedCity", "NormalizedArea" });

            migrationBuilder.CreateIndex(
                name: "IX_places_NormalizedName",
                table: "places",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_places_NormalizedName_NormalizedCity_NormalizedArea",
                table: "places",
                columns: new[] { "NormalizedName", "NormalizedCity", "NormalizedArea" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_places_PriceRange",
                table: "places",
                column: "PriceRange");

            migrationBuilder.CreateIndex(
                name: "IX_places_Status",
                table: "places",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "places");
        }
    }
}
