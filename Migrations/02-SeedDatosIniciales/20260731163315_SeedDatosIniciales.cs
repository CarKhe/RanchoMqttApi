using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RanchoMqttApi.Migrations._02SeedDatosIniciales
{
    /// <inheritdoc />
    public partial class SeedDatosIniciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TipoReles",
                columns: new[] { "idTipoRele", "nombreRele" },
                values: new object[,]
                {
                    { 1, "riego" },
                    { 2, "focos" }
                });

            migrationBuilder.InsertData(
                table: "TipoSensores",
                columns: new[] { "idTipoSensor", "nombreSensor" },
                values: new object[] { 1, "temperatura" });

            migrationBuilder.InsertData(
                table: "Zona",
                columns: new[] { "idZona", "zonaName" },
                values: new object[] { 1, "Zona 1" });

            migrationBuilder.InsertData(
                table: "Rele",
                columns: new[] { "idRele", "Nombre", "idTipoRele", "idZona" },
                values: new object[] { 1, "Riego zona 1", 1, 1 });

            migrationBuilder.InsertData(
                table: "Sensor",
                columns: new[] { "idSensor", "idTipoSensor", "idZona", "nombreSensor" },
                values: new object[] { 1, 1, 1, "Sensor temperatura" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rele",
                keyColumn: "idRele",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Sensor",
                keyColumn: "idSensor",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TipoReles",
                keyColumn: "idTipoRele",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TipoReles",
                keyColumn: "idTipoRele",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TipoSensores",
                keyColumn: "idTipoSensor",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Zona",
                keyColumn: "idZona",
                keyValue: 1);
        }
    }
}
