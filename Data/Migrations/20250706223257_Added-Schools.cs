using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebApplication5.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedSchools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "AspNetUserRoles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId", "SchoolId" });

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Schools",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "School 1" },
                    { 2, "School 2" },
                    { 3, "School 3" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_SchoolId",
                table: "AspNetUserRoles",
                column: "SchoolId");

            // Fix existing records with invalid SchoolId = 0
            migrationBuilder.Sql(
                "UPDATE AspNetUserRoles SET SchoolId = 1 WHERE SchoolId = 0;"
            );

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
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Schools_SchoolId",
                table: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_SchoolId",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AspNetUserRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });
        }
    }
}
