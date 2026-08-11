using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RanchoMqttApi.Migrations._07AddEjecucionesProgramacion
{
    /// <inheritdoc />
    public partial class AddEjecucionesProgramacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EjecucionesProgramacion",
                columns: table => new
                {
                    idEjecucion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idProgramacion = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    inicioReal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finReal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjecucionesProgramacion", x => x.idEjecucion);
                    table.ForeignKey(
                        name: "FK_EjecucionesProgramacion_ProgramacionesRiego_idProgramacion",
                        column: x => x.idProgramacion,
                        principalTable: "ProgramacionesRiego",
                        principalColumn: "idProgramacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EjecucionReleDetalles",
                columns: table => new
                {
                    idEjecucionDetalle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idEjecucion = table.Column<int>(type: "integer", nullable: false),
                    idRele = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    duracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    inicioReal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finPrevisto = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finReal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjecucionReleDetalles", x => x.idEjecucionDetalle);
                    table.ForeignKey(
                        name: "FK_EjecucionReleDetalles_EjecucionesProgramacion_idEjecucion",
                        column: x => x.idEjecucion,
                        principalTable: "EjecucionesProgramacion",
                        principalColumn: "idEjecucion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EjecucionReleDetalles_Rele_idRele",
                        column: x => x.idRele,
                        principalTable: "Rele",
                        principalColumn: "idRele",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionesProgramacion_idProgramacion_fecha",
                table: "EjecucionesProgramacion",
                columns: new[] { "idProgramacion", "fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionReleDetalles_idEjecucion",
                table: "EjecucionReleDetalles",
                column: "idEjecucion");

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionReleDetalles_idRele",
                table: "EjecucionReleDetalles",
                column: "idRele");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EjecucionReleDetalles");

            migrationBuilder.DropTable(
                name: "EjecucionesProgramacion");
        }
    }
}
