using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAssociationSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "Associations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                // Existing associations are grandfathered onto the Free plan.
                defaultValue: "Free");

            // Backfill existing Free associations with the Free student cap (25).
            migrationBuilder.AddColumn<int>(
                name: "StudentLimit",
                table: "Associations",
                type: "integer",
                nullable: true,
                defaultValue: 25);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Associations",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Associations");

            migrationBuilder.DropColumn(
                name: "StudentLimit",
                table: "Associations");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Associations");
        }
    }
}
