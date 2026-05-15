using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.UserProfile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabasePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_notifications_UserProfileShellId",
                table: "user_notifications");

            migrationBuilder.DropIndex(
                name: "IX_user_follows_FollowedUserProfileShellId",
                table: "user_follows");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_UserProfileShellId_CreatedAtUtc",
                table: "user_notifications",
                columns: new[] { "UserProfileShellId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_UserProfileShellId_Status",
                table: "user_notifications",
                columns: new[] { "UserProfileShellId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_FollowedUserProfileShellId_CreatedAtUtc",
                table: "user_follows",
                columns: new[] { "FollowedUserProfileShellId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_FollowerUserProfileShellId_CreatedAtUtc",
                table: "user_follows",
                columns: new[] { "FollowerUserProfileShellId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_saved_posts_UserProfileShellId_CreatedAtUtc",
                table: "saved_posts",
                columns: new[] { "UserProfileShellId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_saved_places_UserProfileShellId_CreatedAtUtc",
                table: "saved_places",
                columns: new[] { "UserProfileShellId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_saved_dishes_UserProfileShellId_CreatedAtUtc",
                table: "saved_dishes",
                columns: new[] { "UserProfileShellId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_notifications_UserProfileShellId_CreatedAtUtc",
                table: "user_notifications");

            migrationBuilder.DropIndex(
                name: "IX_user_notifications_UserProfileShellId_Status",
                table: "user_notifications");

            migrationBuilder.DropIndex(
                name: "IX_user_follows_FollowedUserProfileShellId_CreatedAtUtc",
                table: "user_follows");

            migrationBuilder.DropIndex(
                name: "IX_user_follows_FollowerUserProfileShellId_CreatedAtUtc",
                table: "user_follows");

            migrationBuilder.DropIndex(
                name: "IX_saved_posts_UserProfileShellId_CreatedAtUtc",
                table: "saved_posts");

            migrationBuilder.DropIndex(
                name: "IX_saved_places_UserProfileShellId_CreatedAtUtc",
                table: "saved_places");

            migrationBuilder.DropIndex(
                name: "IX_saved_dishes_UserProfileShellId_CreatedAtUtc",
                table: "saved_dishes");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_UserProfileShellId",
                table: "user_notifications",
                column: "UserProfileShellId");

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_FollowedUserProfileShellId",
                table: "user_follows",
                column: "FollowedUserProfileShellId");
        }
    }
}
