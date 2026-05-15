using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Post.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedPostSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_posts_AuthorId",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_CreatedAtUtc",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_DishId",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_PlaceId",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_Status",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_post_media_uploads_CleanupDueAtUtc",
                table: "post_media_uploads");

            migrationBuilder.DropIndex(
                name: "IX_post_media_uploads_OwnerId",
                table: "post_media_uploads");

            migrationBuilder.DropIndex(
                name: "IX_post_media_uploads_Status",
                table: "post_media_uploads");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "post_media_uploads",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId_AuthorId_CreatedAtUtc",
                table: "posts",
                columns: new[] { "TenantId", "AuthorId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId_CreatedAtUtc",
                table: "posts",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId_DishId_CreatedAtUtc",
                table: "posts",
                columns: new[] { "TenantId", "DishId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId_PlaceId_CreatedAtUtc",
                table: "posts",
                columns: new[] { "TenantId", "PlaceId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_TenantId_Status_CreatedAtUtc",
                table: "posts",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_TenantId_CleanupDueAtUtc",
                table: "post_media_uploads",
                columns: new[] { "TenantId", "CleanupDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_TenantId_OwnerId",
                table: "post_media_uploads",
                columns: new[] { "TenantId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_TenantId_Status",
                table: "post_media_uploads",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_posts_search_vector"
                ON "posts" USING GIN (to_tsvector('simple',
                    coalesce("Caption", '') || ' ' ||
                    coalesce("tags_search_text", '') || ' ' ||
                    coalesce("LocationText", '')));
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_posts_Caption_trgm"
                ON "posts" USING GIN ("Caption" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_posts_tags_search_text_trgm"
                ON "posts" USING GIN ("tags_search_text" gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_posts_LocationText_trgm"
                ON "posts" USING GIN ("LocationText" gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_posts_TenantId_AuthorId_CreatedAtUtc",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_TenantId_CreatedAtUtc",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_TenantId_DishId_CreatedAtUtc",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_TenantId_PlaceId_CreatedAtUtc",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_TenantId_Status_CreatedAtUtc",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_post_media_uploads_TenantId_CleanupDueAtUtc",
                table: "post_media_uploads");

            migrationBuilder.DropIndex(
                name: "IX_post_media_uploads_TenantId_OwnerId",
                table: "post_media_uploads");

            migrationBuilder.DropIndex(
                name: "IX_post_media_uploads_TenantId_Status",
                table: "post_media_uploads");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "post_media_uploads");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_posts_search_vector"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_posts_Caption_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_posts_tags_search_text_trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_posts_LocationText_trgm"";");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_posts_AuthorId",
                table: "posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_posts_CreatedAtUtc",
                table: "posts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_posts_DishId",
                table: "posts",
                column: "DishId");

            migrationBuilder.CreateIndex(
                name: "IX_posts_PlaceId",
                table: "posts",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_posts_Status",
                table: "posts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_CleanupDueAtUtc",
                table: "post_media_uploads",
                column: "CleanupDueAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_OwnerId",
                table: "post_media_uploads",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_Status",
                table: "post_media_uploads",
                column: "Status");
        }
    }
}
