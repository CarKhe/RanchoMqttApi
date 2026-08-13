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

public record CorridaDto(
    int IdEjecucion,
    int IdProgramacion,
    string NombreProgramacion,
    DateOnly Fecha,
    string Estado,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateTime? InicioReal,
    DateTime? FinReal,
    List<CorridaReleDto> Detalles
);

public record CorridaReleDto(
    int IdRele,
    string NombreRele,
    string Tipo,
    int Orden,
    int DuracionMinutos,
    string Estado,
    DateTime? InicioReal,
    DateTime? FinPrevisto,
    DateTime? FinReal
);