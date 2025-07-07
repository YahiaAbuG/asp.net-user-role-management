using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication5.Data.Migrations
{
    /// <inheritdoc />
    public partial class MadeSchoolIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old primary key (which includes SchoolId)
            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            // Drop existing FK (if present — optional safety)
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Schools_SchoolId",
                table: "AspNetUserRoles");

            // Make SchoolId nullable
            migrationBuilder.AlterColumn<int>(
                name: "SchoolId",
                table: "AspNetUserRoles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Add new primary key without SchoolId
            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            // Re-add FK to Schools table
            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_Schools_SchoolId",
                table: "AspNetUserRoles",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FK and PK
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Schools_SchoolId",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            // Make SchoolId non-nullable again
            migrationBuilder.AlterColumn<int>(
                name: "SchoolId",
                table: "AspNetUserRoles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Restore PK to include SchoolId
            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId", "SchoolId" });

            // Re-add FK
            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_Schools_SchoolId",
                table: "AspNetUserRoles",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
