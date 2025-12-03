using System.Text.Json;

namespace JonjubNet.Logging.Domain.Common
{
    /// <summary>
    /// Cache estático de JsonSerializerOptions para evitar allocations en cada serialización
    /// Mejora significativamente el performance al reutilizar las opciones
    /// </summary>
    public static class JsonSerializerOptionsCache
    {
        private static readonly JsonSerializerOptions _defaultOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        /// <summary>
        /// Obtiene las opciones de serialización por defecto (cacheadas)
        /// </summary>
        public static JsonSerializerOptions Default => _defaultOptions;
    }
}

