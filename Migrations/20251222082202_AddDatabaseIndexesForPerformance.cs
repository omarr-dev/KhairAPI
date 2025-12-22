using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIndexesForPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TeacherAttendances_Date",
                table: "TeacherAttendances",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAttendances_Date_HalaqaId",
                table: "TeacherAttendances",
                columns: new[] { "Date", "HalaqaId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAttendances_Date_TeacherId",
                table: "TeacherAttendances",
                columns: new[] { "Date", "TeacherId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAttendances_TeacherId",
                table: "TeacherAttendances",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentHalaqat_HalaqaId_TeacherId",
                table: "StudentHalaqat",
                columns: new[] { "HalaqaId", "TeacherId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressRecords_Date",
                table: "ProgressRecords",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressRecords_Date_HalaqaId",
                table: "ProgressRecords",
                columns: new[] { "Date", "HalaqaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressRecords_Date_StudentId",
                table: "ProgressRecords",
                columns: new[] { "Date", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_HalaqaTeachers_HalaqaId",
                table: "HalaqaTeachers",
                column: "HalaqaId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_Date",
                table: "Attendances",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_Date_HalaqaId",
                table: "Attendances",
                columns: new[] { "Date", "HalaqaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherAttendances_Date",
                table: "TeacherAttendances");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAttendances_Date_HalaqaId",
                table: "TeacherAttendances");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAttendances_Date_TeacherId",
                table: "TeacherAttendances");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAttendances_TeacherId",
                table: "TeacherAttendances");

            migrationBuilder.DropIndex(
                name: "IX_StudentHalaqat_HalaqaId_TeacherId",
                table: "StudentHalaqat");

            migrationBuilder.DropIndex(
                name: "IX_ProgressRecords_Date",
                table: "ProgressRecords");

            migrationBuilder.DropIndex(
                name: "IX_ProgressRecords_Date_HalaqaId",
                table: "ProgressRecords");

            migrationBuilder.DropIndex(
                name: "IX_ProgressRecords_Date_StudentId",
                table: "ProgressRecords");

            migrationBuilder.DropIndex(
                name: "IX_HalaqaTeachers_HalaqaId",
                table: "HalaqaTeachers");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_Date",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_Date_HalaqaId",
                table: "Attendances");
        }
    }
}
