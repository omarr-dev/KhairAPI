using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhairAPI.Migrations
{
    /// <inheritdoc />
    public partial class ClearInvalidTeacherCheckOutTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data cleanup: self check-out used to allow a check-out in the same minute as
            // the check-in, storing CheckOutTime == CheckInTime. ValidateTimes rejects such
            // records, which blocked supervisors' bulk saves. Clear the invalid check-outs.
            migrationBuilder.Sql(
                @"UPDATE ""TeacherAttendances""
                  SET ""CheckOutTime"" = NULL
                  WHERE ""CheckOutTime"" IS NOT NULL
                    AND ""CheckInTime"" IS NOT NULL
                    AND ""CheckOutTime"" <= ""CheckInTime"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
