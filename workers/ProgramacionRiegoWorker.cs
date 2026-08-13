
namespace RanchoMqttApi;

public class ProgramacionRiegoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RiegoOptions _opciones;
    private readonly ILogger<ProgramacionRiegoWorker> _logger;

    public ProgramacionRiegoWorker(
        IServiceScopeFactory scopeFactory,
        RiegoOptions opciones,
        ILogger<ProgramacionRiegoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(5, _opciones.IntervaloTickSegundos));

        _logger.LogInformation(
            "Worker de riego iniciado: tick cada {Seg}s, zona {Zona}, simulacion {Sim}",
            intervalo.TotalSeconds, _opciones.ZonaHoraria, _opciones.ModoSimulacion);

        using var timer = new PeriodicTimer(intervalo);

        try
        {
            while (true)
            {
                await EjecutarTickAsync(ct);
                if (!await timer.WaitForNextTickAsync(ct)) break;
            }
        }
        catch (OperationCanceledException)
        {
            // apagado normal de la API
        }

        _logger.LogInformation("Worker de riego detenido");
    }

    private async Task EjecutarTickAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var motor = scope.ServiceProvider.GetRequiredService<IMotorProgramacionService>();
            await motor.TickAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // NUNCA relanzar: si una excepción escapa de ExecuteAsync,
            // .NET tumba el host COMPLETO (BackgroundServiceExceptionBehavior.StopHost)
            _logger.LogError(ex, "Error en el tick de riego; se reintenta en el siguiente");
        }
    }
}
