namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para el productor de Kafka
    /// Permite desacoplar la lógica de aplicación de la implementación de Kafka
    /// </summary>
    public interface IKafkaProducer
    {
        /// <summary>
        /// Indica si el producer está habilitado
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Envía un mensaje a Kafka
        /// </summary>
        /// <param name="message">Mensaje a enviar (JSON string)</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task SendAsync(string message);
    }
}

