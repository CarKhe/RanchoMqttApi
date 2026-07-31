namespace RanchoMqttApi;

public record ReleEstadoDto(
    int Id,
    string Nombre,
    string Tipo,
    string Zona,
    string? Estado,
    DateTime? UltimaConfirmacion
);
