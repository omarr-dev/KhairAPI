using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddHalaqaSupervisorRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HalaqaSupervisorAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    HalaqaId = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssociationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HalaqaSupervisorAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HalaqaSupervisorAssignments_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HalaqaSupervisorAssignments_Halaqat_HalaqaId",
                        column: x => x.HalaqaId,
                        principalTable: "Halaqat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HalaqaSupervisorAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HalaqaSupervisorAssignments_AssociationId",
                table: "HalaqaSupervisorAssignments",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_HalaqaSupervisorAssignments_HalaqaId",
                table: "HalaqaSupervisorAssignments",
                column: "HalaqaId");

            migrationBuilder.CreateIndex(
                name: "IX_HalaqaSupervisorAssignments_UserId",
                table: "HalaqaSupervisorAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HalaqaSupervisorAssignments_UserId_HalaqaId",
                table: "HalaqaSupervisorAssignments",
                columns: new[] { "UserId", "HalaqaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HalaqaSupervisorAssignments");
        }
    }
}
