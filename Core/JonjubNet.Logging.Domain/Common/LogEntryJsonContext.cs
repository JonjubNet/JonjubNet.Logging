using System.Text.Json.Serialization;
using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Domain.Common
{
    /// <summary>
    /// Contexto de serialización JSON generado en tiempo de compilación para mejor rendimiento y AOT compatibility.
    /// </summary>
    [JsonSerializable(typeof(StructuredLogEntry))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
    internal partial class LogEntryJsonContext : JsonSerializerContext
    {
    }
}

