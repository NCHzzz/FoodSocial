using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Post.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostContentCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModeratorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Caption = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    PriceRange = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    tags_search_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LocationText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "post_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_comments_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_likes",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_likes", x => new { x.PostId, x.ActorId });
                    table.ForeignKey(
                        name: "FK_post_likes_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_media_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_CreatedAtUtc",
                table: "content_reports",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_Status",
                table: "content_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_TargetType_TargetId",
                table: "content_reports",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_AuthorId",
                table: "post_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_PostId",
                table: "post_comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_Status",
                table: "post_comments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_post_likes_ActorId",
                table: "post_likes",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_post_media_PostId_IsPrimary",
                table: "post_media",
                columns: new[] { "PostId", "IsPrimary" });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_reports");

            migrationBuilder.DropTable(
                name: "post_comments");

            migrationBuilder.DropTable(
                name: "post_likes");

            migrationBuilder.DropTable(
                name: "post_media");

            migrationBuilder.DropTable(
                name: "posts");
        }
    }
}
