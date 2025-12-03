using JonjubNet.Logging.Application.Interfaces;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Health check para el componente de logging
    /// Proporciona información sobre el estado del sistema de logging
    /// </summary>
    public class LoggingHealthCheck : ILoggingHealthCheck
    {
        private readonly ILogQueue? _logQueue;

        public LoggingHealthCheck(ILogQueue? logQueue = null)
        {
            _logQueue = logQueue;
        }

        public bool IsHealthy()
        {
            if (_logQueue == null)
                return true; // Sin cola, siempre saludable

            var status = GetQueueStatus();
            return status.IsHealthy;
        }

        public QueueHealthStatus GetQueueStatus()
        {
            if (_logQueue == null)
            {
                return new QueueHealthStatus
                {
                    CurrentCount = 0,
                    Capacity = 0
                };
            }

            return new QueueHealthStatus
            {
                CurrentCount = _logQueue.Count,
                Capacity = _logQueue.Capacity
            };
        }
    }
}

