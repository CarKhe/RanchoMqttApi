namespace RanchoMqttApi;

public class MqttTopics
{
    public const string ReleTopicPrefix = "rancho/reles/";
    public const string CmdSuffix = "/cmd";
    public const string EstadoSuffix = "/estado";

    public const string Conexion = "rancho/esp32/conexion";

    public const string ReleCmdWildcard = "rancho/reles/+/+/cmd";
    public const string ReleEstadoWildcard = "rancho/reles/+/+/estado";

    public const string SensorTopicPrefix = "rancho/sensores/";
    public const string LecturaSuffix = "/lectura";

    public const string SensorLecturaWildcard = "rancho/sensores/+/+/lectura";

    public static string ReleCmd(string tipo, int id) => $"{ReleTopicPrefix}{tipo}/{id}{CmdSuffix}";
    public static string ReleEstado(string tipo, int id) => $"{ReleTopicPrefix}{tipo}/{id}{EstadoSuffix}";
    public static string SensorLectura(string tipo, int id) => $"{SensorTopicPrefix}{tipo}/{id}{LecturaSuffix}";

    public static bool EsTopicDeEstadoRele(string topic) =>
        topic.StartsWith(ReleTopicPrefix) && topic.EndsWith(EstadoSuffix);
    public static bool EsTopicDeLecturaSensor(string topic) =>
        topic.StartsWith(SensorTopicPrefix) && topic.EndsWith(LecturaSuffix);
}
