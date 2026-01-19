using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class DropTargetAchievementsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TargetAchievements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TargetAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssociationId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ConsolidationPagesAchieved = table.Column<int>(type: "integer", nullable: false),
                    ConsolidationPagesTarget = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MemorizationLinesAchieved = table.Column<int>(type: "integer", nullable: false),
                    MemorizationLinesTarget = table.Column<int>(type: "integer", nullable: true),
                    RevisionPagesAchieved = table.Column<int>(type: "integer", nullable: false),
                    RevisionPagesTarget = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetAchievements_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TargetAchievements_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TargetAchievements_AssociationId",
                table: "TargetAchievements",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetAchievements_Date",
                table: "TargetAchievements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TargetAchievements_StudentId_Date",
                table: "TargetAchievements",
                columns: new[] { "StudentId", "Date" },
                unique: true);
        }
    }
}
