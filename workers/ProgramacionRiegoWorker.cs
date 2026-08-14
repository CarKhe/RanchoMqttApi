
namespace RanchoMqttApi;

public class ProgramacionRiegoWorker : BackgroundService
{
    // el ESP32 necesita un momento para republicar sus estados retenidos;
    // sin esto el cache estaría vacío y la reconciliación no sabría nada
    private static readonly TimeSpan EsperaAntesDeReconciliar = TimeSpan.FromSeconds(10);

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

        try
        {
            await Task.Delay(EsperaAntesDeReconciliar, ct);
            await EjecutarEnScopeAsync(m => m.ReconciliarTrasReinicioAsync(ct), "la reconciliación", ct);

            // el temporizador arranca aquí, no antes: si naciera antes de la espera,
            // su primer periodo ya habría vencido y el segundo tick saldría de inmediato
            using var timer = new PeriodicTimer(intervalo);

            while (true)
            {
                await EjecutarEnScopeAsync(m => m.TickAsync(ct), "el tick de riego", ct);
                if (!await timer.WaitForNextTickAsync(ct)) break;
            }
        }
        catch (OperationCanceledException)
        {
            // apagado normal de la API
        }

        _logger.LogInformation("Worker de riego detenido");
    }

    private async Task EjecutarEnScopeAsync(
        Func<IMotorProgramacionService, Task> accion, string queHacia, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var motor = scope.ServiceProvider.GetRequiredService<IMotorProgramacionService>();
            await accion(motor);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // NUNCA relanzar: si una excepción escapa de ExecuteAsync,
            // .NET tumba el host COMPLETO (BackgroundServiceExceptionBehavior.StopHost)
            _logger.LogError(ex, "Error en {Que}", queHacia);
        }
    }
}
