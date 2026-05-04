using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Post.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRepostsAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "post_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: true),
                    Message = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "post_reposts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Caption = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_reposts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_reposts_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_post_notifications_CreatedAtUtc",
                table: "post_notifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_post_notifications_RecipientId",
                table: "post_notifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_post_notifications_RecipientId_Status",
                table: "post_notifications",
                columns: new[] { "RecipientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_post_reposts_ActorId",
                table: "post_reposts",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_post_reposts_CreatedAtUtc",
                table: "post_reposts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_post_reposts_PostId_ActorId",
                table: "post_reposts",
                columns: new[] { "PostId", "ActorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_notifications");

            migrationBuilder.DropTable(
                name: "post_reposts");
        }
    }
}
