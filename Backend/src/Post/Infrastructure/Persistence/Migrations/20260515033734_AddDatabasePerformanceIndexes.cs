using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Post.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabasePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_posts_AuthorId_Status_CreatedAtUtc_Id",
                table: "posts",
                columns: new[] { "AuthorId", "Status", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_DishId_Status_CreatedAtUtc_Id",
                table: "posts",
                columns: new[] { "DishId", "Status", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_PlaceId_Status_CreatedAtUtc_Id",
                table: "posts",
                columns: new[] { "PlaceId", "Status", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_Status_CreatedAtUtc_Id",
                table: "posts",
                columns: new[] { "Status", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_post_notifications_RecipientId_CreatedAtUtc",
                table: "post_notifications",
                columns: new[] { "RecipientId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_Status_CleanupDueAtUtc_Id",
                table: "post_media_uploads",
                columns: new[] { "Status", "CleanupDueAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_posts_AuthorId_Status_CreatedAtUtc_Id",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_DishId_Status_CreatedAtUtc_Id",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_PlaceId_Status_CreatedAtUtc_Id",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_Status_CreatedAtUtc_Id",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_post_notifications_RecipientId_CreatedAtUtc",
                table: "post_notifications");

            migrationBuilder.DropIndex(
                name: "IX_post_media_uploads_Status_CleanupDueAtUtc_Id",
                table: "post_media_uploads");
        }
    }
}
