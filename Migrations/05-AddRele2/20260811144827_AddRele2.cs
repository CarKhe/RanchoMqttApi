using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RanchoMqttApi.Migrations._05AddRele2
{
    /// <inheritdoc />
    public partial class AddRele2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Rele",
                columns: new[] { "idRele", "Nombre", "idTipoRele", "idZona" },
                values: new object[] { 2, "Riego zona 2", 1, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rele",
                keyColumn: "idRele",
                keyValue: 2);
        }
    }
}
