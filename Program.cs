using Microsoft.EntityFrameworkCore;
using RanchoMqttApi;
using RanchoMqttApi.Workers;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DBContext>(options =>
    options.UseNpgsql(connectionString));

//Agregando SignalR para envio de datos al front sin la necesidad de una solicitud
builder.Services.AddSignalR();

//Worker del MQTT
builder.Services.AddHostedService<MqttWorker>();

//Interfaces
builder.Services.AddSingleton<IMqttPublisherService, MqttPublisherService>();
builder.Services.AddScoped<IReleService,ReleService>();
builder.Services.AddSingleton<IReleCacheService, ReleCacheService>();

builder.Services.AddScoped<IMqttTopicHandler, ReleEstadoHandler>();
builder.Services.AddScoped<IMqttTopicHandler, TemperaturaHandler>();
builder.Services.AddScoped<IMqttTopicHandler, ConexionHandler>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapHub<RelesHub>("/hubs/reles");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();


