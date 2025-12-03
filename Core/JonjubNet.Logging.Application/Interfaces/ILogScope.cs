namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Scope de logging que agrega propiedades a todos los logs dentro del scope
    /// </summary>
    public interface ILogScope : IDisposable
    {
        /// <summary>
        /// Propiedades del scope que se agregarán a todos los logs
        /// </summary>
        Dictionary<string, object> Properties { get; }
    }
}
