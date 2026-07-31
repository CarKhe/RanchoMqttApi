using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RanchoMqttApi.Migrations._01AddTipoReleSensor
{
    /// <inheritdoc />
    public partial class AddTipoReleSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LecturaTemperaturas_Sensor_sensoridSensor",
                table: "LecturaTemperaturas");

            migrationBuilder.DropIndex(
                name: "IX_LecturaTemperaturas_sensoridSensor",
                table: "LecturaTemperaturas");

            migrationBuilder.DropColumn(
                name: "sensoridSensor",
                table: "LecturaTemperaturas");

            migrationBuilder.AddColumn<int>(
                name: "idTipoSensor",
                table: "Sensor",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "idTipoRele",
                table: "Rele",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TipoReles",
                columns: table => new
                {
                    idTipoRele = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombreRele = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoReles", x => x.idTipoRele);
                });

            migrationBuilder.CreateTable(
                name: "TipoSensores",
                columns: table => new
                {
                    idTipoSensor = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombreSensor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoSensores", x => x.idTipoSensor);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sensor_idTipoSensor",
                table: "Sensor",
                column: "idTipoSensor");

            migrationBuilder.CreateIndex(
                name: "IX_Rele_idTipoRele",
                table: "Rele",
                column: "idTipoRele");

            migrationBuilder.CreateIndex(
                name: "IX_LecturaTemperaturas_idSensor",
                table: "LecturaTemperaturas",
                column: "idSensor");

            migrationBuilder.AddForeignKey(
                name: "FK_LecturaTemperaturas_Sensor_idSensor",
                table: "LecturaTemperaturas",
                column: "idSensor",
                principalTable: "Sensor",
                principalColumn: "idSensor",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rele_TipoReles_idTipoRele",
                table: "Rele",
                column: "idTipoRele",
                principalTable: "TipoReles",
                principalColumn: "idTipoRele",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sensor_TipoSensores_idTipoSensor",
                table: "Sensor",
                column: "idTipoSensor",
                principalTable: "TipoSensores",
                principalColumn: "idTipoSensor",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LecturaTemperaturas_Sensor_idSensor",
                table: "LecturaTemperaturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Rele_TipoReles_idTipoRele",
                table: "Rele");

            migrationBuilder.DropForeignKey(
                name: "FK_Sensor_TipoSensores_idTipoSensor",
                table: "Sensor");

            migrationBuilder.DropTable(
                name: "TipoReles");

            migrationBuilder.DropTable(
                name: "TipoSensores");

            migrationBuilder.DropIndex(
                name: "IX_Sensor_idTipoSensor",
                table: "Sensor");

            migrationBuilder.DropIndex(
                name: "IX_Rele_idTipoRele",
                table: "Rele");

            migrationBuilder.DropIndex(
                name: "IX_LecturaTemperaturas_idSensor",
                table: "LecturaTemperaturas");

            migrationBuilder.DropColumn(
                name: "idTipoSensor",
                table: "Sensor");

            migrationBuilder.DropColumn(
                name: "idTipoRele",
                table: "Rele");

            migrationBuilder.AddColumn<int>(
                name: "sensoridSensor",
                table: "LecturaTemperaturas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LecturaTemperaturas_sensoridSensor",
                table: "LecturaTemperaturas",
                column: "sensoridSensor");

            migrationBuilder.AddForeignKey(
                name: "FK_LecturaTemperaturas_Sensor_sensoridSensor",
                table: "LecturaTemperaturas",
                column: "sensoridSensor",
                principalTable: "Sensor",
                principalColumn: "idSensor",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
