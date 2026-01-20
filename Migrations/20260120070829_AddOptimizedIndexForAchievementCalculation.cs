using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizedIndexForAchievementCalculation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProgressRecords_StudentId_Date_Type",
                table: "ProgressRecords",
                columns: new[] { "StudentId", "Date", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProgressRecords_StudentId_Date_Type",
                table: "ProgressRecords");
        }
    }
}
