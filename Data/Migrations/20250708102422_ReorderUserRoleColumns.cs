using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication5.Data.Migrations
{
    public partial class ReorderUserRoleColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign key to Activity
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Activity_ActivityId",
                table: "AspNetUserRoles");

            // 2. Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_ActivityId",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_UserId_RoleId_SchoolId_ActivityId",
                table: "AspNetUserRoles");

            // 3. Drop primary key
            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            // 4. Rename current table (backup)
            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles_Old");

            // 5. Recreate new table with Id as first column
            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(nullable: false),
                    RoleId = table.Column<string>(nullable: false),
                    SchoolId = table.Column<int>(nullable: true),
                    ActivityId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_Activity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 6. Copy data from old to new (optional)
            migrationBuilder.Sql(@"
                INSERT INTO AspNetUserRoles (UserId, RoleId, SchoolId, ActivityId)
                SELECT UserId, RoleId, SchoolId, ActivityId
                FROM AspNetUserRoles_Old
            ");

            // 7. Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_ActivityId",
                table: "AspNetUserRoles",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_UserId_RoleId_SchoolId_ActivityId",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId", "SchoolId", "ActivityId" },
                unique: true,
                filter: "[SchoolId] IS NOT NULL AND [ActivityId] IS NOT NULL");

            // 8. Drop old table
            migrationBuilder.DropTable(
                name: "AspNetUserRoles_Old");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversal: You can reverse this using a similar approach
            // But typically not needed unless you really care about ordering again
        }
    }
}
