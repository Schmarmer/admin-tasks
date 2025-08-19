using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admin_Tasks.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailPathToTaskAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 8, 15, 8, 36, 26, 253, DateTimeKind.Utc).AddTicks(158), "$2a$11$J8d3tdGDzkcurrhjM8AC.Ooe4OvtFVqDYJR09Q/Al1h7ZpzXroQEC" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 8, 15, 8, 36, 26, 361, DateTimeKind.Utc).AddTicks(3186), "$2a$11$GEvfJGTNCKWcbQIPd/yN0eAoyfnPy7CMlIHUG706ZkBq8RmUSRbKS" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 8, 15, 8, 36, 26, 469, DateTimeKind.Utc).AddTicks(2108), "$2a$11$0yxiUAypzbK.NfHEkLB1WuelpyaF7L6m64GGl/6/Qji2rxvCTuZNm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 8, 15, 8, 31, 55, 704, DateTimeKind.Utc).AddTicks(571), "$2a$11$HZlhL3NZSUv6LUCrw242/ecQPk4sZAOmhFsih8WGvEHXO6X3x1zXK" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 8, 15, 8, 31, 55, 816, DateTimeKind.Utc).AddTicks(362), "$2a$11$S5w49OvaWP5/7R/uwOV7uO2zNKrCw2jg72iy9wHjgQUGZVlrFjCia" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 8, 15, 8, 31, 55, 926, DateTimeKind.Utc).AddTicks(7136), "$2a$11$8eHXiEVWAnmuoMdKlS986.sLxrlJo3.SL7DiwVy6IGICo10VkNaZ6" });
        }
    }
}
