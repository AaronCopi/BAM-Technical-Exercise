using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StargateAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Person",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "John Doe" },
                    { 2, "Jane Doe" }
                });
            migrationBuilder.InsertData(
                table: "AstronautDetail",
                columns: new[] { "Id", "PersonId", "CurrentRank", "CurrentDutyTitle", "CareerStartDate" },
                values: new object[] { 1, 1, "1LT", "Commander", DateTime.Now });
            migrationBuilder.InsertData(
                table: "AstronautDuty",
                columns: new[] { "Id", "PersonId", "DutyTitle", "Rank", "DutyStartDate" },
                values: new object[] { 1, 1, "Commander", "1LT", DateTime.Now });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AstronautDuty",
                keyColumn: "Id",
                keyValue: 1);
            migrationBuilder.DeleteData(
                table: "AstronautDetail",
                keyColumn: "Id",
                keyValue: 1);
            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 1);
            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
