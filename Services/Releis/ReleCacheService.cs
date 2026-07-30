using System.Collections.Concurrent;

namespace RanchoMqttApi;

public class ReleCacheService : IReleCacheService
{
    private readonly ConcurrentDictionary<string, ReleCacheEntry> _estados = new();

    public void Actualizar(string tipo, int id, string estado)
    {
        var clave = $"{tipo}/{id}";
        _estados[clave] = new ReleCacheEntry(tipo, id, estado, DateTime.UtcNow);
    }

    public ReleCacheEntry? Obtener(string tipo, int id) // NUEVO
    {
        var clave = $"{tipo}/{id}";
        return _estados.TryGetValue(clave, out var valor) ? valor : null;
    }

    public IReadOnlyDictionary<string, ReleCacheEntry> ObtenerTodos() => _estados;
}
