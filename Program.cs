using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RanchoMqttApi;
using RanchoMqttApi.Workers;
using Serilog;
using Microsoft.OpenApi;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

//Connection SQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexion. Define la variable de entorno " +
        "ConnectionStrings__DefaultConnection (o el valor en appsettings.Development.json).");
}
builder.Services.AddDbContext<DBContext>(options =>
    options.UseNpgsql(connectionString));

//SerialLog
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .WriteTo.Console()
        .WriteTo.File("Logs/rancho-.log", rollingInterval: RollingInterval.Day);
});

//Agregando SignalR para envio de datos al front sin la necesidad de una solicitud
builder.Services.AddSignalR();

//Worker del MQTT
builder.Services.AddHostedService<MqttWorker>();

//Interfaces
builder.Services.AddSingleton<IMqttPublisherService, MqttPublisherService>();
builder.Services.AddScoped<IReleService, ReleService>();
builder.Services.AddSingleton<IReleCacheService, ReleCacheService>();
builder.Services.AddSingleton<IComandoTimeoutService, ComandoTimeoutService>();
builder.Services.AddScoped<IMqttTopicHandler, ReleEstadoHandler>();
builder.Services.AddScoped<IMqttTopicHandler, TemperaturaHandler>();
builder.Services.AddScoped<IMqttTopicHandler, ConexionHandler>();
builder.Services.AddScoped<IProgramacionService, ProgramacionService>();
builder.Services.AddScoped<IMotorProgramacionService,MotorProgramacionService>();

//JWT: settings + servicio
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Falta la seccion 'Jwt'. Define Jwt__Key, Jwt__Issuer, Jwt__Audience y Jwt__ExpirationHours " +
        "como variables de entorno.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt__Key esta vacia o es demasiado corta (minimo 32 caracteres).");
}

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtService, JwtService>();

var riegoOptions = builder.Configuration.GetSection("Riego").Get<RiegoOptions>()
    ?? new RiegoOptions();
builder.Services.AddSingleton(riegoOptions);

//Politicas CORS (los origenes se configuran por Cors__AllowedOrigins__0, __1, ...)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//config JWT (autenticación)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/reles"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega solo el token, sin la palabra 'Bearer'"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

var app = builder.Build();

//Aplica las migraciones pendientes al arrancar.
//Reintenta porque el contenedor de Postgres puede tardar unos segundos en aceptar conexiones.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DBContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxIntentos = 10;
    for (var intento = 1; intento <= maxIntentos; intento++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Migraciones aplicadas correctamente.");
            break;
        }
        catch (Exception ex) when (intento < maxIntentos)
        {
            logger.LogWarning(ex,
                "Base de datos no disponible (intento {Intento}/{Max}). Reintento en 5s...",
                intento, maxIntentos);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}

app.UseCors("AngularPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<RelesHub>("/hubs/reles");

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

//Endpoint simple para healthchecks de Dokploy / Traefik
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));


//app.UseHttpsRedirection();
app.MapControllers();
app.Run();
