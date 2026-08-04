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
    public DbSet<TipoRele> TipoReles { get; set; }
    public DbSet<TipoSensor> TipoSensores { get; set; }
    public DbSet<Users> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Zona>().HasData(
        new Zona { idZona = 1, zonaName = "Zona 1" }
    );

    modelBuilder.Entity<TipoRele>().HasData(
        new TipoRele { idTipoRele = 1, nombreRele = "riego" },
        new TipoRele { idTipoRele = 2, nombreRele = "focos" }
    );

    modelBuilder.Entity<TipoSensor>().HasData(
        new TipoSensor { idTipoSensor = 1, nombreSensor = "temperatura" }
    );

    modelBuilder.Entity<Rele>().HasData(
        new Rele { idRele = 1, Nombre = "Riego zona 1", idZona = 1, idTipoRele = 1 }
    );

    modelBuilder.Entity<Sensor>().HasData(
        new Sensor { idSensor = 1, nombreSensor = "Sensor temperatura", idZona = 1, idTipoSensor = 1 }
    );

    modelBuilder.Entity<Users>().HasData(new Users
    {
        idUser = 1,
        userName = "admin",
        userMail = "admin@rancho.com",
        passwordHash = "AQAAAAIAAYagAAAAEN/I0zj/0K1lzubB26Cp9BhShozc3XkiSW7abwfFgKxIKyXwzo0OxwDJhsJmmwZaLQ==",
        createDate = new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc),
        updatedLogin = new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc)
    });
}
 

}
