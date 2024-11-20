using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebApplication5.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedingRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "2c932e63-47de-4b31-9b70-4c509c2443a0", null, "Admin", "ADMIN" },
                    { "a0cb54da-fcb0-4e7f-ba90-0159981e5d29", null, "Manager", "MANAGER" },
                    { "bf8cb782-25d2-48df-98ca-cd869ea460f8", null, "Member", "MEMBER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2c932e63-47de-4b31-9b70-4c509c2443a0");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a0cb54da-fcb0-4e7f-ba90-0159981e5d29");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "bf8cb782-25d2-48df-98ca-cd869ea460f8");
        }
    }
}
