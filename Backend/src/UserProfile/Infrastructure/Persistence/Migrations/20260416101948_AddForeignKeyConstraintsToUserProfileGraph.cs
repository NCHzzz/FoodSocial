using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.UserProfile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyConstraintsToUserProfileGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_user_follows_FollowedUserProfileShellId",
                table: "user_follows",
                column: "FollowedUserProfileShellId");

            migrationBuilder.AddForeignKey(
                name: "FK_saved_dishes_user_profile_shells_UserProfileShellId",
                table: "saved_dishes",
                column: "UserProfileShellId",
                principalTable: "user_profile_shells",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_places_user_profile_shells_UserProfileShellId",
                table: "saved_places",
                column: "UserProfileShellId",
                principalTable: "user_profile_shells",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_posts_user_profile_shells_UserProfileShellId",
                table: "saved_posts",
                column: "UserProfileShellId",
                principalTable: "user_profile_shells",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_follows_user_profile_shells_FollowedUserProfileShellId",
                table: "user_follows",
                column: "FollowedUserProfileShellId",
                principalTable: "user_profile_shells",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_follows_user_profile_shells_FollowerUserProfileShellId",
                table: "user_follows",
                column: "FollowerUserProfileShellId",
                principalTable: "user_profile_shells",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_saved_dishes_user_profile_shells_UserProfileShellId",
                table: "saved_dishes");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_places_user_profile_shells_UserProfileShellId",
                table: "saved_places");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_posts_user_profile_shells_UserProfileShellId",
                table: "saved_posts");

            migrationBuilder.DropForeignKey(
                name: "FK_user_follows_user_profile_shells_FollowedUserProfileShellId",
                table: "user_follows");

            migrationBuilder.DropForeignKey(
                name: "FK_user_follows_user_profile_shells_FollowerUserProfileShellId",
                table: "user_follows");

            migrationBuilder.DropIndex(
                name: "IX_user_follows_FollowedUserProfileShellId",
                table: "user_follows");
        }
    }
}
