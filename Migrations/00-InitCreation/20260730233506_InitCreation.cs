using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RanchoMqttApi.Migrations._00InitCreation
{
    /// <inheritdoc />
    public partial class InitCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Zona",
                columns: table => new
                {
                    idZona = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    zonaName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zona", x => x.idZona);
                });

            migrationBuilder.CreateTable(
                name: "Rele",
                columns: table => new
                {
                    idRele = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    idZona = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rele", x => x.idRele);
                    table.ForeignKey(
                        name: "FK_Rele_Zona_idZona",
                        column: x => x.idZona,
                        principalTable: "Zona",
                        principalColumn: "idZona",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sensor",
                columns: table => new
                {
                    idSensor = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombreSensor = table.Column<string>(type: "text", nullable: false),
                    idZona = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensor", x => x.idSensor);
                    table.ForeignKey(
                        name: "FK_Sensor_Zona_idZona",
                        column: x => x.idZona,
                        principalTable: "Zona",
                        principalColumn: "idZona",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialEstadoReleis",
                columns: table => new
                {
                    idHistorialEstadoRelei = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idRele = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    exito = table.Column<bool>(type: "boolean", nullable: false),
                    fechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstadoReleis", x => x.idHistorialEstadoRelei);
                    table.ForeignKey(
                        name: "FK_HistorialEstadoReleis_Rele_idRele",
                        column: x => x.idRele,
                        principalTable: "Rele",
                        principalColumn: "idRele",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LecturaTemperaturas",
                columns: table => new
                {
                    idLecturaTemperatura = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idSensor = table.Column<int>(type: "integer", nullable: false),
                    sensoridSensor = table.Column<int>(type: "integer", nullable: false),
                    temperatura = table.Column<double>(type: "double precision", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LecturaTemperaturas", x => x.idLecturaTemperatura);
                    table.ForeignKey(
                        name: "FK_LecturaTemperaturas_Sensor_sensoridSensor",
                        column: x => x.sensoridSensor,
                        principalTable: "Sensor",
                        principalColumn: "idSensor",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadoReleis_idRele",
                table: "HistorialEstadoReleis",
                column: "idRele");

            migrationBuilder.CreateIndex(
                name: "IX_LecturaTemperaturas_sensoridSensor",
                table: "LecturaTemperaturas",
                column: "sensoridSensor");

            migrationBuilder.CreateIndex(
                name: "IX_Rele_idZona",
                table: "Rele",
                column: "idZona");

            migrationBuilder.CreateIndex(
                name: "IX_Sensor_idZona",
                table: "Sensor",
                column: "idZona");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialEstadoReleis");

            migrationBuilder.DropTable(
                name: "LecturaTemperaturas");

            migrationBuilder.DropTable(
                name: "Rele");

            migrationBuilder.DropTable(
                name: "Sensor");

            migrationBuilder.DropTable(
                name: "Zona");
        }
    }
}
