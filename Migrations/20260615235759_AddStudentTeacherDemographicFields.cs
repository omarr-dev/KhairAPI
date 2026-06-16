using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentTeacherDemographicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContractType",
                table: "Teachers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Teachers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Teachers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayrollGroup",
                table: "Teachers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Center",
                table: "Students",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "Students",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Office",
                table: "Students",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialStatus",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "PayrollGroup",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "Center",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Office",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SocialStatus",
                table: "Students");
        }
    }
}
