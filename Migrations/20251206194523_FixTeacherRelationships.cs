using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixTeacherRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgressRecords_Teachers_TeacherId",
                table: "ProgressRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentHalaqat_Teachers_TeacherId",
                table: "StudentHalaqat");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudentHalaqat",
                table: "StudentHalaqat");

            migrationBuilder.AlterColumn<int>(
                name: "TeacherId",
                table: "ProgressRecords",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentHalaqat",
                table: "StudentHalaqat",
                columns: new[] { "StudentId", "HalaqaId", "TeacherId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressRecords_Teachers_TeacherId",
                table: "ProgressRecords",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentHalaqat_Teachers_TeacherId",
                table: "StudentHalaqat",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgressRecords_Teachers_TeacherId",
                table: "ProgressRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentHalaqat_Teachers_TeacherId",
                table: "StudentHalaqat");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudentHalaqat",
                table: "StudentHalaqat");

            migrationBuilder.AlterColumn<int>(
                name: "TeacherId",
                table: "ProgressRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentHalaqat",
                table: "StudentHalaqat",
                columns: new[] { "StudentId", "HalaqaId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressRecords_Teachers_TeacherId",
                table: "ProgressRecords",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentHalaqat_Teachers_TeacherId",
                table: "StudentHalaqat",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
