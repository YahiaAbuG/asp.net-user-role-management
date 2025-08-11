using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication5.Data.Migrations
{
    public partial class SplitAttendanceTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create AttendanceSessions
            migrationBuilder.CreateTable(
                name: "AttendanceSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Activity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_ActivityId_Date",
                table: "AttendanceSessions",
                columns: new[] { "ActivityId", "Date" },
                unique: true);

            // 2) Make room on AttendanceRecords for new FK (nullable for now)
            migrationBuilder.AddColumn<int>(
                name: "AttendanceSessionId",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            // 3) Build sessions from existing records' ActivityId/Date
            // (Create distinct sessions)
            migrationBuilder.Sql(@"
INSERT INTO AttendanceSessions (ActivityId, Date)
SELECT DISTINCT ar.ActivityId, CAST(ar.[Date] AS date)
FROM AttendanceRecords ar
WHERE ar.ActivityId IS NOT NULL;
");

            // 4) Map AttendanceRecords to new sessions
            migrationBuilder.Sql(@"
UPDATE ar
SET ar.AttendanceSessionId = s.Id
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions s
    ON s.ActivityId = ar.ActivityId
   AND CONVERT(date, s.[Date]) = CONVERT(date, ar.[Date]);
");

            // 5) Remove absent rows (IsPresent = 0)
            migrationBuilder.Sql(@"
DELETE FROM AttendanceRecords
WHERE IsPresent = 0;
");

            // 6) Drop old FK/index/columns tied to ActivityId/Date/IsPresent
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Activity_ActivityId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ActivityId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ActivityId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "IsPresent",
                table: "AttendanceRecords");

            // 7) AttendanceSessionId -> required, add FK + unique index
            migrationBuilder.AlterColumn<int>(
                name: "AttendanceSessionId",
                table: "AttendanceRecords",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AttendanceSessionId_UserId",
                table: "AttendanceRecords",
                columns: new[] { "AttendanceSessionId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_AttendanceSessions_AttendanceSessionId",
                table: "AttendanceRecords",
                column: "AttendanceSessionId",
                principalTable: "AttendanceSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate old columns
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_AttendanceSessions_AttendanceSessionId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_AttendanceSessionId_UserId",
                table: "AttendanceRecords");

            migrationBuilder.AddColumn<int>(
                name: "ActivityId",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "AttendanceRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPresent",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // Map back ActivityId/Date from session
            migrationBuilder.Sql(@"
UPDATE ar
SET ar.ActivityId = s.ActivityId,
    ar.[Date]     = s.[Date]
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions s
    ON s.Id = ar.AttendanceSessionId;
");

            // Restore FK/index on ActivityId
            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ActivityId",
                table: "AttendanceRecords",
                column: "ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Activity_ActivityId",
                table: "AttendanceRecords",
                column: "ActivityId",
                principalTable: "Activity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Drop the new column and the sessions table
            migrationBuilder.DropColumn(
                name: "AttendanceSessionId",
                table: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AttendanceSessions");
        }
    }
}
