using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RanchoMqttApi.Migrations._06AddProgramacionRiego
{
    /// <inheritdoc />
    public partial class AddProgramacionRiego : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgramacionesRiego",
                columns: table => new
                {
                    idProgramacion = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    habilitada = table.Column<bool>(type: "boolean", nullable: false),
                    horaInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    horaFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    diasSemana = table.Column<int>(type: "integer", nullable: false),
                    modoEjecucion = table.Column<int>(type: "integer", nullable: false),
                    fechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramacionesRiego", x => x.idProgramacion);
                });

            migrationBuilder.CreateTable(
                name: "ProgramacionReles",
                columns: table => new
                {
                    idProgramacionRele = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idProgramacion = table.Column<int>(type: "integer", nullable: false),
                    idRele = table.Column<int>(type: "integer", nullable: false),
                    duracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramacionReles", x => x.idProgramacionRele);
                    table.ForeignKey(
                        name: "FK_ProgramacionReles_ProgramacionesRiego_idProgramacion",
                        column: x => x.idProgramacion,
                        principalTable: "ProgramacionesRiego",
                        principalColumn: "idProgramacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramacionReles_Rele_idRele",
                        column: x => x.idRele,
                        principalTable: "Rele",
                        principalColumn: "idRele",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramacionReles_idProgramacion",
                table: "ProgramacionReles",
                column: "idProgramacion");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramacionReles_idRele",
                table: "ProgramacionReles",
                column: "idRele");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramacionReles");

            migrationBuilder.DropTable(
                name: "ProgramacionesRiego");
        }
    }
}
