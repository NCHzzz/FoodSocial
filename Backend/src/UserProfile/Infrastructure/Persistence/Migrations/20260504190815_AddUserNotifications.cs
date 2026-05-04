using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.UserProfile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileShellId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientIdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorIdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_notifications_user_profile_shells_UserProfileShellId",
                        column: x => x.UserProfileShellId,
                        principalTable: "user_profile_shells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_CreatedAtUtc",
                table: "user_notifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_RecipientIdentityId",
                table: "user_notifications",
                column: "RecipientIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_RecipientIdentityId_Status",
                table: "user_notifications",
                columns: new[] { "RecipientIdentityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_UserProfileShellId",
                table: "user_notifications",
                column: "UserProfileShellId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_notifications");
        }
    }
}
