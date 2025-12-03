namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para Circuit Breaker que protege contra llamadas repetidas a servicios fallidos
    /// </summary>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Estado actual del circuit breaker
        /// </summary>
        CircuitBreakerState State { get; }

        /// <summary>
        /// Nombre del sink asociado
        /// </summary>
        string SinkName { get; }

        /// <summary>
        /// Ejecuta una operación protegida por el circuit breaker
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);

        /// <summary>
        /// Registra un éxito (cierra el circuit breaker si estaba abierto)
        /// </summary>
        void RecordSuccess();

        /// <summary>
        /// Registra un fallo (puede abrir el circuit breaker si hay muchos fallos)
        /// </summary>
        void RecordFailure();

        /// <summary>
        /// Resetea el circuit breaker manualmente
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Estados del circuit breaker
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Cerrado - Funcionando normalmente
        /// </summary>
        Closed,

        /// <summary>
        /// Abierto - Bloqueado por fallos
        /// </summary>
        Open,

        /// <summary>
        /// Semi-Abierto - Probando recuperación
        /// </summary>
        HalfOpen
    }

    /// <summary>
    /// Excepción lanzada cuando el circuit breaker está abierto
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string sinkName) 
            : base($"Circuit breaker está abierto para sink: {sinkName}")
        {
            SinkName = sinkName;
        }

        public string SinkName { get; }
    }
}

