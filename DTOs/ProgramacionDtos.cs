namespace RanchoMqttApi;

// entrada
public record CrearProgramacionDto(
    string Nombre,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    int DiasSemana,
    ModoEjecucion ModoEjecucion,
    List<CrearProgramacionReleDto> Reles
);

public record CrearProgramacionReleDto(int IdRele, int DuracionMinutos, int Orden);

// salida
public record ProgramacionDto(
    int IdProgramacion,
    string Nombre,
    bool Habilitada,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    int DiasSemana,
    string ModoEjecucion,
    List<ProgramacionReleDto> Reles
);

public record ProgramacionReleDto(
    int IdRele, string NombreRele, string Tipo, int DuracionMinutos, int Orden
);