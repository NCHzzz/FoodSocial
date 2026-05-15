using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Dish.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dishes_Category",
                table: "dishes");

            migrationBuilder.DropIndex(
                name: "IX_dishes_CuisineType",
                table: "dishes");

            migrationBuilder.DropIndex(
                name: "IX_dishes_NormalizedName",
                table: "dishes");

            migrationBuilder.DropIndex(
                name: "IX_dishes_Status",
                table: "dishes");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "dishes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "IX_dishes_TenantId_Category",
                table: "dishes",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_dishes_TenantId_CuisineType",
                table: "dishes",
                columns: new[] { "TenantId", "CuisineType" });

            migrationBuilder.CreateIndex(
                name: "IX_dishes_TenantId_NormalizedName",
                table: "dishes",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dishes_TenantId_Status",
                table: "dishes",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_dishes_NormalizedName_trgm"
                ON "dishes" USING GIN ("NormalizedName" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_dishes_aliases_search_text_trgm"
                ON "dishes" USING GIN ("aliases_search_text" gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dishes_TenantId_Category",
                table: "dishes");

            migrationBuilder.DropIndex(
                name: "IX_dishes_TenantId_CuisineType",
                table: "dishes");

            migrationBuilder.DropIndex(
                name: "IX_dishes_TenantId_NormalizedName",
                table: "dishes");

            migrationBuilder.DropIndex(
                name: "IX_dishes_TenantId_Status",
                table: "dishes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "dishes");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_dishes_NormalizedName_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_dishes_aliases_search_text_trgm"";");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

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
    }
}
