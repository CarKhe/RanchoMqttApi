using MQTTnet;

namespace RanchoMqttApi;

/// <summary>
/// Un solo lugar donde se arma la conexion al broker. Lo usan el worker que
/// escucha y el publicador, para que no se puedan desincronizar.
/// </summary>
public static class MqttOpciones
{
    public static MqttClientOptionsBuilder Construir(IConfiguration config)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(config["Mqtt:Host"], config.GetValue<int>("Mqtt:Port"))
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));

        // Si no hay usuario configurado se conecta anonimo: comodo en desarrollo,
        // pero el broker de produccion tiene allow_anonymous false y lo rechazara.
        var usuario = config["Mqtt:Usuario"];
        if (!string.IsNullOrWhiteSpace(usuario))
            builder = builder.WithCredentials(usuario, config["Mqtt:Password"] ?? "");

        return builder;
    }
}
