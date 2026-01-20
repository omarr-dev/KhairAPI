using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberLinesToProgressRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "NumberLines",
                table: "ProgressRecords",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberLines",
                table: "ProgressRecords");
        }
    }
}
