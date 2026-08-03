using Microsoft.EntityFrameworkCore;
using RanchoMqttApi;
using RanchoMqttApi.Workers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//Connection SQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DBContext>(options =>
    options.UseNpgsql(connectionString));

//SerialLog
builder.Host.UseSerilog((context, config) =>
{
    config
        .WriteTo.Console()
        .WriteTo.File("Logs/rancho-.log", rollingInterval: RollingInterval.Day);
});

//Agregando SignalR para envio de datos al front sin la necesidad de una solicitud
builder.Services.AddSignalR();

//Worker del MQTT
builder.Services.AddHostedService<MqttWorker>();

//Interfaces
builder.Services.AddSingleton<IMqttPublisherService, MqttPublisherService>();
builder.Services.AddScoped<IReleService,ReleService>();
builder.Services.AddSingleton<IReleCacheService, ReleCacheService>();

builder.Services.AddSingleton<IComandoTimeoutService, ComandoTimeoutService>();

builder.Services.AddScoped<IMqttTopicHandler, ReleEstadoHandler>();
builder.Services.AddScoped<IMqttTopicHandler, TemperaturaHandler>();
builder.Services.AddScoped<IMqttTopicHandler, ConexionHandler>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

//Politicas CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AngularPolicy");  

app.MapHub<RelesHub>("/hubs/reles");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

//app.UseHttpsRedirection();
app.MapControllers();
app.Run();


