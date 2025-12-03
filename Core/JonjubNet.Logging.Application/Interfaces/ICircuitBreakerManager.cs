namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Manager para obtener circuit breakers por sink
    /// </summary>
    public interface ICircuitBreakerManager
    {
        /// <summary>
        /// Obtiene el circuit breaker para un sink específico
        /// </summary>
        ICircuitBreaker GetBreaker(string sinkName);
    }
}

