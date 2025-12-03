using JonjubNet.Logging.Application.Configuration;

namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para gestionar configuración de logging de forma dinámica (Hot-Reload)
    /// Permite cambiar configuración en runtime sin reiniciar la aplicación
    /// </summary>
    public interface ILoggingConfigurationManager
    {
        /// <summary>
        /// Obtiene la configuración actual (siempre actualizada)
        /// </summary>
        LoggingConfiguration Current { get; }

        /// <summary>
        /// Cambia el nivel mínimo de log en runtime
        /// </summary>
        /// <param name="minimumLevel">Nuevo nivel mínimo (Trace, Debug, Information, Warning, Error, Critical, Fatal)</param>
        /// <returns>True si el cambio fue exitoso</returns>
        bool SetMinimumLevel(string minimumLevel);

        /// <summary>
        /// Habilita o deshabilita un sink específico en runtime
        /// </summary>
        /// <param name="sinkName">Nombre del sink (Console, File, Http, Elasticsearch)</param>
        /// <param name="enabled">True para habilitar, False para deshabilitar</param>
        /// <returns>True si el cambio fue exitoso</returns>
        bool SetSinkEnabled(string sinkName, bool enabled);

        /// <summary>
        /// Cambia la tasa de sampling para un nivel de log específico en runtime
        /// </summary>
        /// <param name="logLevel">Nivel de log (Trace, Debug, Information, Warning, Error, Critical, Fatal)</param>
        /// <param name="samplingRate">Tasa de sampling (0.0 a 1.0, donde 1.0 = 100%)</param>
        /// <returns>True si el cambio fue exitoso</returns>
        bool SetSamplingRate(string logLevel, double samplingRate);

        /// <summary>
        /// Habilita o deshabilita el sampling en runtime
        /// </summary>
        /// <param name="enabled">True para habilitar, False para deshabilitar</param>
        /// <returns>True si el cambio fue exitoso</returns>
        bool SetSamplingEnabled(bool enabled);

        /// <summary>
        /// Cambia el límite máximo de logs por minuto para un nivel específico en runtime
        /// </summary>
        /// <param name="logLevel">Nivel de log</param>
        /// <param name="maxLogsPerMinute">Máximo de logs por minuto (0 para deshabilitar límite)</param>
        /// <returns>True si el cambio fue exitoso</returns>
        bool SetMaxLogsPerMinute(string logLevel, int maxLogsPerMinute);

        /// <summary>
        /// Habilita o deshabilita el logging completo en runtime
        /// </summary>
        /// <param name="enabled">True para habilitar, False para deshabilitar</param>
        /// <returns>True si el cambio fue exitoso</returns>
        bool SetLoggingEnabled(bool enabled);

        /// <summary>
        /// Establece el nivel mínimo de log para una categoría específica en runtime
        /// </summary>
        /// <param name="category">Categoría de log (ej: "Security", "Performance")</param>
        /// <param name="level">Nivel mínimo (Trace, Debug, Information, Warning, Error, Critical, Fatal)</param>
        /// <returns>True si el cambio fue exitoso</returns>
        bool SetCategoryLogLevel(string category, string level);

        /// <summary>
        /// Establece el nivel mínimo de log para una operación específica en runtime
        /// </summary>
        /// <param name="operation">Operación (ej: "Payment", "HealthCheck")</param>
        /// <param name="level">Nivel mínimo (Trace, Debug, Information, Warning, Error, Critical, Fatal)</param>
        /// <returns>True si el cambio fue exitoso</returns>
        bool SetOperationLogLevel(string operation, string level);

        /// <summary>
        /// Establece un override temporal del nivel de log para debugging con expiración automática
        /// </summary>
        /// <param name="category">Categoría de log (opcional, null para aplicar globalmente)</param>
        /// <param name="level">Nivel de log temporal</param>
        /// <param name="expiration">Tiempo de expiración del override</param>
        /// <returns>True si el override fue establecido exitosamente</returns>
        bool SetTemporaryOverride(string? category, string level, TimeSpan expiration);

        /// <summary>
        /// Remueve un override temporal específico
        /// </summary>
        /// <param name="category">Categoría del override a remover (null para override global)</param>
        /// <returns>True si el override fue removido</returns>
        bool RemoveTemporaryOverride(string? category);

        /// <summary>
        /// Evento que se dispara cuando la configuración cambia
        /// </summary>
        event Action<LoggingConfiguration>? ConfigurationChanged;
    }
}

