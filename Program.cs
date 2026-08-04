using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RanchoMqttApi;
using RanchoMqttApi.Workers;
using Serilog;
using Microsoft.OpenApi;
using System.Text;
using Microsoft.AspNetCore.Identity;

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
builder.Services.AddScoped<IReleService, ReleService>();
builder.Services.AddSingleton<IReleCacheService, ReleCacheService>();
builder.Services.AddSingleton<IComandoTimeoutService, ComandoTimeoutService>();
builder.Services.AddScoped<IMqttTopicHandler, ReleEstadoHandler>();
builder.Services.AddScoped<IMqttTopicHandler, TemperaturaHandler>();
builder.Services.AddScoped<IMqttTopicHandler, ConexionHandler>();

//JWT: settings + servicio
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtService, JwtService>();

//Politicas CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
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

app.UseCors("AngularPolicy");

app.UseAuthentication();
app.UseAuthorization();

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

if (args.Contains("--hash"))
{
    var hasher = new PasswordHasher<Users>();
    Console.WriteLine(hasher.HashPassword(null!, "admin123"));
    return;
}

