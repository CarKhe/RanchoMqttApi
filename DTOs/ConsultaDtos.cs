namespace RanchoMqttApi;

public record ZonaDto(int Id, string Nombre);

public record TipoDto(int Id, string Nombre);

public record ReleDto(int Id, string Nombre, string Tipo, string Zona);

public record SensorDto(int Id, string Nombre, string Tipo, string Zona);

public record HistorialReleDto(
    int Id,
    int IdRele,
    string Rele,
    string Estado,
    bool Exito,
    DateTime FechaHora
);

public record LecturaTemperaturaDto(
    int Id,
    int IdSensor,
    string Sensor,
    double Temperatura,
    DateTime FechaHora
);
