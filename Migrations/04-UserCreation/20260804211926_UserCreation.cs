using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RanchoMqttApi.Migrations._04UserCreation
{
    /// <inheritdoc />
    public partial class UserCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "idUser", "createDate", "passwordHash", "updatedLogin", "userMail", "userName" },
                values: new object[] { 1, new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Utc), "AQAAAAIAAYagAAAAEN/I0zj/0K1lzubB26Cp9BhShozc3XkiSW7abwfFgKxIKyXwzo0OxwDJhsJmmwZaLQ==", new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Utc), "admin@rancho.com", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "idUser",
                keyValue: 1);
        }
    }
}
