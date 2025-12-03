namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Manager para obtener políticas de retry por sink
    /// </summary>
    public interface IRetryPolicyManager
    {
        /// <summary>
        /// Obtiene la política de retry para un sink específico
        /// </summary>
        IRetryPolicy GetPolicy(string sinkName);
    }
}

