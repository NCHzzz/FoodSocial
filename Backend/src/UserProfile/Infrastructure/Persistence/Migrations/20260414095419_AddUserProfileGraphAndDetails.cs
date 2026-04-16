using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.UserProfile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileGraphAndDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saved_dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileShellId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_dishes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "saved_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileShellId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_places", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "saved_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileShellId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_follows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowerUserProfileShellId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowedUserProfileShellId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_follows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_profile_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileShellId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreferredCuisines = table.Column<string>(type: "text", nullable: false),
                    DislikedIngredients = table.Column<string>(type: "text", nullable: false),
                    DietaryRestrictions = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profile_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_profile_details_user_profile_shells_UserProfileShellId",
                        column: x => x.UserProfileShellId,
                        principalTable: "user_profile_shells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saved_dishes_UserProfileShellId_DishId",
                table: "saved_dishes",
                columns: new[] { "UserProfileShellId", "DishId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_places_UserProfileShellId_PlaceId",
                table: "saved_places",
                columns: new[] { "UserProfileShellId", "PlaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_posts_UserProfileShellId_PostId",
                table: "saved_posts",
                columns: new[] { "UserProfileShellId", "PostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_follows_FollowerUserProfileShellId_FollowedUserProfile~",
                table: "user_follows",
                columns: new[] { "FollowerUserProfileShellId", "FollowedUserProfileShellId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_details_UserProfileShellId",
                table: "user_profile_details",
                column: "UserProfileShellId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_dishes");

            migrationBuilder.DropTable(
                name: "saved_places");

            migrationBuilder.DropTable(
                name: "saved_posts");

            migrationBuilder.DropTable(
                name: "user_follows");

            migrationBuilder.DropTable(
                name: "user_profile_details");
        }
    }
}
