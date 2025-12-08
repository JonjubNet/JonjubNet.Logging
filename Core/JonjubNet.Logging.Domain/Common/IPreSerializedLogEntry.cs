namespace JonjubNet.Logging.Domain.Common
{
    /// <summary>
    /// Interfaz para indicar que un log entry tiene JSON pre-serializado
    /// Permite compartir serialización entre múltiples sinks para optimizar performance
    /// </summary>
    public interface IPreSerializedLogEntry
    {
        /// <summary>
        /// JSON pre-serializado del log entry (null si no está disponible)
        /// </summary>
        string? PreSerializedJson { get; set; }
    }
}

