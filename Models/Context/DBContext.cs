using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options) : base(options){}

    public DbSet<Zona> Zona { get; set; }
    public DbSet<Rele> Rele { get; set; }
    public DbSet<Sensor> Sensor { get; set; }
    public DbSet<HistorialEstadoRelei> HistorialEstadoReleis { get; set; }
    public DbSet<LecturaTemperatura> LecturaTemperaturas { get; set; }


}
