namespace RanchoMqttApi;

public interface IReleCacheService
{
    void Actualizar(string tipo, int id, string estado);
    ReleCacheEntry? Obtener(string tipo, int id);
    IReadOnlyDictionary<string, ReleCacheEntry> ObtenerTodos();
}


public record ReleCacheEntry(string Tipo, int Id, string Estado, DateTime UltimaConfirmacion);
