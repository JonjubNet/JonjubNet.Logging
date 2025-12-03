namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para políticas de reintento
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Ejecuta una operación con reintentos según la política
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determina si se debe reintentar basado en la excepción y el número de intento
        /// </summary>
        bool ShouldRetry(Exception exception, int attemptNumber);

        /// <summary>
        /// Obtiene el delay antes del siguiente intento
        /// </summary>
        TimeSpan GetDelay(int attemptNumber);

        /// <summary>
        /// Número máximo de reintentos
        /// </summary>
        int MaxRetries { get; }
    }

    /// <summary>
    /// Estrategias de retry disponibles
    /// </summary>
    public enum RetryStrategy
    {
        /// <summary>
        /// No reintentar
        /// </summary>
        NoRetry,

        /// <summary>
        /// Delay fijo entre reintentos
        /// </summary>
        FixedDelay,

        /// <summary>
        /// Delay exponencial (1s, 2s, 4s, 8s...)
        /// </summary>
        ExponentialBackoff,

        /// <summary>
        /// Delay exponencial con jitter (aleatoriedad)
        /// </summary>
        JitteredExponentialBackoff
    }

    /// <summary>
    /// Excepción lanzada cuando se agotan los reintentos
    /// </summary>
    public class RetryExhaustedException : Exception
    {
        public RetryExhaustedException(string message, Exception? innerException) 
            : base(message, innerException)
        {
        }
    }
}

