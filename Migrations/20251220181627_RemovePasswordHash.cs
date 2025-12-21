using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemovePasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop column only if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Users' AND column_name = 'PasswordHash'
                    ) THEN
                        ALTER TABLE ""Users"" DROP COLUMN ""PasswordHash"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
