using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.UserProfile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialUserProfileBootstrap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_profile_shells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profile_shells", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_shells_IdentityId",
                table: "user_profile_shells",
                column: "IdentityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_shells_Subject",
                table: "user_profile_shells",
                column: "Subject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_profile_shells");
        }
    }
}
