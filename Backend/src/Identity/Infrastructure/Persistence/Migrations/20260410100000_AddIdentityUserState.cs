using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSocial.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityUserState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'identity_users'
                          AND column_name = 'State') THEN
                        ALTER TABLE identity_users
                        ADD COLUMN "State" character varying(32) NOT NULL DEFAULT 'PendingBootstrap';
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'identity_users'
                          AND column_name = 'State') THEN
                        ALTER TABLE identity_users
                        DROP COLUMN "State";
                    END IF;
                END
                $$;
                """);
        }
    }
}
