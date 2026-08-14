using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RanchoMqttApi.Migrations._08CongelarHorarioEnCorrida
{
    /// <inheritdoc />
    public partial class CongelarHorarioEnCorrida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "horaFin",
                table: "EjecucionesProgramacion",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "horaInicio",
                table: "EjecucionesProgramacion",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "modoEjecucion",
                table: "EjecucionesProgramacion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Las corridas que ya existian nacen con 00:00 y modo 0, y el motor
            // las marcaria como "ventana perdida". Se rellenan desde su regla.
            // Va al final a proposito: las columnas tienen que existir primero.
            migrationBuilder.Sql("""
                UPDATE "EjecucionesProgramacion" e
                SET "horaInicio" = p."horaInicio",
                    "horaFin" = p."horaFin",
                    "modoEjecucion" = p."modoEjecucion"
                FROM "ProgramacionesRiego" p
                WHERE e."idProgramacion" = p."idProgramacion";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "horaFin",
                table: "EjecucionesProgramacion");

            migrationBuilder.DropColumn(
                name: "horaInicio",
                table: "EjecucionesProgramacion");

            migrationBuilder.DropColumn(
                name: "modoEjecucion",
                table: "EjecucionesProgramacion");
        }
    }
}
