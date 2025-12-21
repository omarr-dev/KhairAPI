using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHalaqaDescription : Migration
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
                        WHERE table_name = 'Halaqat' AND column_name = 'Description'
                    ) THEN
                        ALTER TABLE ""Halaqat"" DROP COLUMN ""Description"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Halaqat",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
