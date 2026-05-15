using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Place.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_places_CuisineTypes",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_CuisineTypesSearchText",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_NormalizedCity_NormalizedArea",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_NormalizedName",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_NormalizedName_NormalizedCity_NormalizedArea",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_PriceRange",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_Status",
                table: "places");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "places",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "IX_places_TenantId_CuisineTypesSearchText",
                table: "places",
                columns: new[] { "TenantId", "CuisineTypesSearchText" });

            migrationBuilder.CreateIndex(
                name: "IX_places_TenantId_NormalizedCity_NormalizedArea",
                table: "places",
                columns: new[] { "TenantId", "NormalizedCity", "NormalizedArea" });

            migrationBuilder.CreateIndex(
                name: "IX_places_TenantId_NormalizedName",
                table: "places",
                columns: new[] { "TenantId", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_places_TenantId_NormalizedName_NormalizedCity_NormalizedArea",
                table: "places",
                columns: new[] { "TenantId", "NormalizedName", "NormalizedCity", "NormalizedArea" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_places_TenantId_PriceRange",
                table: "places",
                columns: new[] { "TenantId", "PriceRange" });

            migrationBuilder.CreateIndex(
                name: "IX_places_TenantId_Status",
                table: "places",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_places_NormalizedName_trgm"
                ON "places" USING GIN ("NormalizedName" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_places_NormalizedAddressText_trgm"
                ON "places" USING GIN ("NormalizedAddressText" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_places_NormalizedCity_trgm"
                ON "places" USING GIN ("NormalizedCity" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_places_NormalizedArea_trgm"
                ON "places" USING GIN ("NormalizedArea" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_places_NormalizedCountry_trgm"
                ON "places" USING GIN ("NormalizedCountry" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_places_CuisineTypesSearchText_trgm"
                ON "places" USING GIN ("CuisineTypesSearchText" gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_places_TenantId_CuisineTypesSearchText",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_TenantId_NormalizedCity_NormalizedArea",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_TenantId_NormalizedName",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_TenantId_NormalizedName_NormalizedCity_NormalizedArea",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_TenantId_PriceRange",
                table: "places");

            migrationBuilder.DropIndex(
                name: "IX_places_TenantId_Status",
                table: "places");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "places");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_places_NormalizedName_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_places_NormalizedAddressText_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_places_NormalizedCity_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_places_NormalizedArea_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_places_NormalizedCountry_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_places_CuisineTypesSearchText_trgm"";");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

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
    }
}
