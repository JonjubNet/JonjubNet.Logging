namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para administrar scopes de logging
    /// Permite que la capa Application sea independiente de la implementación en Infrastructure
    /// </summary>
    public interface ILogScopeManager
    {
        /// <summary>
        /// Obtiene todas las propiedades de los scopes activos (más reciente primero)
        /// </summary>
        Dictionary<string, object> GetCurrentScopeProperties();
    }
}

