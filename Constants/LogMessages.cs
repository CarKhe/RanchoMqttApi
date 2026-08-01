using System.Runtime.CompilerServices;

namespace RanchoMqttApi;

public static class LogMessages
{
    // MqttWorker
    public const string ApiConectada = "API conectada al broker MQTT";
    public const string ApiSuscrita = "API suscrita a todos los topics";
    public const string ConexionPerdida = "Conexión MQTT perdida. Reintentando...";
    public const string ConexionRestaurada = "Se restauró la Conexión";
    public const string ErrorConexionReintento = "No se pudo conectar/suscribir. Reintentando en 5 segundos...";
    public const string TopicNoReconocido = "Topic no reconocido: {Topic}";
    public const string APIPublicacion = "[API publicó] {topic} -> {payload}";

}
