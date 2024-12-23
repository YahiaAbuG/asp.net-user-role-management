using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebApplication5.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0fdb34b1-50c6-4587-90f5-712fa554c679");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "32319adf-5a8a-4109-97ec-9533f13ab446");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e36bec5c-7fc8-46bd-8394-cbf748188566");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0fdb34b1-50c6-4587-90f5-712fa554c679", null, "Manager", "MANAGER" },
                    { "32319adf-5a8a-4109-97ec-9533f13ab446", null, "Member", "MEMBER" },
                    { "e36bec5c-7fc8-46bd-8394-cbf748188566", null, "Admin", "ADMIN" }
                });
        }
    }
}
