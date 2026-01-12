using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create Associations table FIRST
            migrationBuilder.CreateTable(
                name: "Associations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Subdomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Logo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SecondaryColor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Associations_Subdomain",
                table: "Associations",
                column: "Subdomain",
                unique: true);

            // Step 2: Insert default association "جمعية خير" with ID=1
            migrationBuilder.Sql(@"
                INSERT INTO ""Associations"" (""Id"", ""Name"", ""Subdomain"", ""IsActive"", ""CreatedAt"")
                VALUES (1, 'جمعية خير', 'khair', true, NOW());
                
                -- Reset sequence to start from 2
                SELECT setval('""Associations_Id_seq""', 2, false);
            ");

            // Step 3: Add AssociationId columns to all tables with DEFAULT 0
            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "Teachers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "TeacherAttendances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "StudentHalaqat",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "ProgressRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "HalaqaTeachers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "Halaqat",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "Attendances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Step 4: Update all existing records to use AssociationId = 1
            migrationBuilder.Sql(@"
                UPDATE ""Users"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
                UPDATE ""Teachers"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
                UPDATE ""Students"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
                UPDATE ""Halaqat"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
                UPDATE ""HalaqaTeachers"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
                UPDATE ""StudentHalaqat"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
                UPDATE ""Attendances"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
                UPDATE ""ProgressRecords"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
                UPDATE ""TeacherAttendances"" SET ""AssociationId"" = 1 WHERE ""AssociationId"" = 0;
            ");

            // Step 5: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Users_AssociationId",
                table: "Users",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_AssociationId",
                table: "Teachers",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAttendances_AssociationId",
                table: "TeacherAttendances",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_AssociationId",
                table: "Students",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentHalaqat_AssociationId",
                table: "StudentHalaqat",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressRecords_AssociationId",
                table: "ProgressRecords",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_HalaqaTeachers_AssociationId",
                table: "HalaqaTeachers",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_Halaqat_AssociationId",
                table: "Halaqat",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AssociationId",
                table: "Attendances",
                column: "AssociationId");

            // Step 6: Add foreign key constraints (NOW all data has valid AssociationId=1)
            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Associations_AssociationId",
                table: "Attendances",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Halaqat_Associations_AssociationId",
                table: "Halaqat",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HalaqaTeachers_Associations_AssociationId",
                table: "HalaqaTeachers",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressRecords_Associations_AssociationId",
                table: "ProgressRecords",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentHalaqat_Associations_AssociationId",
                table: "StudentHalaqat",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Associations_AssociationId",
                table: "Students",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherAttendances_Associations_AssociationId",
                table: "TeacherAttendances",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Associations_AssociationId",
                table: "Teachers",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Associations_AssociationId",
                table: "Users",
                column: "AssociationId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Associations_AssociationId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Halaqat_Associations_AssociationId",
                table: "Halaqat");

            migrationBuilder.DropForeignKey(
                name: "FK_HalaqaTeachers_Associations_AssociationId",
                table: "HalaqaTeachers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgressRecords_Associations_AssociationId",
                table: "ProgressRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentHalaqat_Associations_AssociationId",
                table: "StudentHalaqat");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Associations_AssociationId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherAttendances_Associations_AssociationId",
                table: "TeacherAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Associations_AssociationId",
                table: "Teachers");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Associations_AssociationId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Associations");

            migrationBuilder.DropIndex(
                name: "IX_Users_AssociationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_AssociationId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAttendances_AssociationId",
                table: "TeacherAttendances");

            migrationBuilder.DropIndex(
                name: "IX_Students_AssociationId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentHalaqat_AssociationId",
                table: "StudentHalaqat");

            migrationBuilder.DropIndex(
                name: "IX_ProgressRecords_AssociationId",
                table: "ProgressRecords");

            migrationBuilder.DropIndex(
                name: "IX_HalaqaTeachers_AssociationId",
                table: "HalaqaTeachers");

            migrationBuilder.DropIndex(
                name: "IX_Halaqat_AssociationId",
                table: "Halaqat");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_AssociationId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "TeacherAttendances");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "StudentHalaqat");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "ProgressRecords");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "HalaqaTeachers");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "Halaqat");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "Attendances");
        }
    }
}
