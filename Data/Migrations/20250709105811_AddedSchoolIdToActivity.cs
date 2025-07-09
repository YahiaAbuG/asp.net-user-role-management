using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication5.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedSchoolIdToActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "Activity",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Activity_SchoolId",
                table: "Activity",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activity_Schools_SchoolId",
                table: "Activity",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activity_Schools_SchoolId",
                table: "Activity");

            migrationBuilder.DropIndex(
                name: "IX_Activity_SchoolId",
                table: "Activity");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Activity");
        }
    }
}
