using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_identity_users_Subject",
                table: "identity_users");

            migrationBuilder.DropIndex(
                name: "IX_identity_role_assignments_Subject_Role",
                table: "identity_role_assignments");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "identity_users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "identity_role_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "IX_identity_users_TenantId_Subject",
                table: "identity_users",
                columns: new[] { "TenantId", "Subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_identity_role_assignments_TenantId_Subject_Role",
                table: "identity_role_assignments",
                columns: new[] { "TenantId", "Subject", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_identity_users_TenantId_Subject",
                table: "identity_users");

            migrationBuilder.DropIndex(
                name: "IX_identity_role_assignments_TenantId_Subject_Role",
                table: "identity_role_assignments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "identity_users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "identity_role_assignments");

            migrationBuilder.CreateIndex(
                name: "IX_identity_users_Subject",
                table: "identity_users",
                column: "Subject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_identity_role_assignments_Subject_Role",
                table: "identity_role_assignments",
                columns: new[] { "Subject", "Role" },
                unique: true);
        }
    }
}
