using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.UserProfile.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_profile_shells_IdentityId",
                table: "user_profile_shells");

            migrationBuilder.DropIndex(
                name: "IX_user_profile_shells_Subject",
                table: "user_profile_shells");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "user_profile_shells",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_shells_TenantId_IdentityId",
                table: "user_profile_shells",
                columns: new[] { "TenantId", "IdentityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_shells_TenantId_Subject",
                table: "user_profile_shells",
                columns: new[] { "TenantId", "Subject" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_profile_shells_TenantId_IdentityId",
                table: "user_profile_shells");

            migrationBuilder.DropIndex(
                name: "IX_user_profile_shells_TenantId_Subject",
                table: "user_profile_shells");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "user_profile_shells");

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
    }
}
