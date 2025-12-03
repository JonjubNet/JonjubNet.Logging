namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para health checks del componente de logging
    /// </summary>
    public interface ILoggingHealthCheck
    {
        /// <summary>
        /// Verifica el estado de salud del componente de logging
        /// </summary>
        /// <returns>True si el componente está saludable</returns>
        bool IsHealthy();

        /// <summary>
        /// Obtiene información del estado de la cola de logs
        /// </summary>
        QueueHealthStatus GetQueueStatus();
    }

    /// <summary>
    /// Estado de salud de la cola de logs
    /// </summary>
    public class QueueHealthStatus
    {
        public int CurrentCount { get; set; }
        public int Capacity { get; set; }
        public double UtilizationPercent => Capacity > 0 ? (CurrentCount / (double)Capacity) * 100 : 0;
        public bool IsHealthy => UtilizationPercent < 80.0; // Saludable si < 80% de capacidad
    }
}

