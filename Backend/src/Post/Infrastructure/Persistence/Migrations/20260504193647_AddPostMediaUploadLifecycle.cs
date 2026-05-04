using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Post.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostMediaUploadLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "post_media_uploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachedPostId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModerationHold = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CleanupDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttachedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_media_uploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_media_uploads_posts_AttachedPostId",
                        column: x => x.AttachedPostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_AttachedPostId",
                table: "post_media_uploads",
                column: "AttachedPostId");

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

            migrationBuilder.CreateIndex(
                name: "IX_post_media_uploads_Url",
                table: "post_media_uploads",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_media_uploads");
        }
    }
}
