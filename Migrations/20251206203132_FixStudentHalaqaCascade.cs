using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentHalaqaCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentHalaqat_Teachers_TeacherId",
                table: "StudentHalaqat");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentHalaqat_Teachers_TeacherId",
                table: "StudentHalaqat",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentHalaqat_Teachers_TeacherId",
                table: "StudentHalaqat");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentHalaqat_Teachers_TeacherId",
                table: "StudentHalaqat",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
