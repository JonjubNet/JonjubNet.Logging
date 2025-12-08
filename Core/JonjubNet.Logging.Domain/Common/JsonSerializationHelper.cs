using System.Buffers;
using System.Text;
using System.Text.Json;
using JonjubNet.Logging.Domain.Entities;

namespace JonjubNet.Logging.Domain.Common
{
    /// <summary>
    /// Helper para serialización JSON optimizada usando ArrayBufferWriter y buffers reutilizables
    /// Reduce allocations significativamente en hot paths de serialización
    /// </summary>
    public static class JsonSerializationHelper
    {
        // Tamaño inicial del buffer (se ajusta automáticamente si es necesario)
        private const int InitialBufferSize = 4096;

        /// <summary>
        /// Serializa un StructuredLogEntry a JSON usando ArrayBufferWriter para reducir allocations
        /// Usa ArrayBufferWriter que implementa IBufferWriter y se ajusta automáticamente
        /// </summary>
        /// <param name="logEntry">Entrada de log a serializar</param>
        /// <returns>JSON string serializado</returns>
        public static string SerializeToJson(StructuredLogEntry logEntry)
        {
            // Usar ArrayBufferWriter que implementa IBufferWriter<byte> y se ajusta automáticamente
            var bufferWriter = new ArrayBufferWriter<byte>(InitialBufferSize);
            var writer = new Utf8JsonWriter(bufferWriter);
            
            try
            {
                // Serializar usando source generation
                JsonSerializer.Serialize(writer, logEntry, LogEntryJsonContext.Default.StructuredLogEntry);
                writer.Flush();

                // Convertir UTF-8 bytes a string
                var writtenBytes = bufferWriter.WrittenSpan;
                var jsonString = Encoding.UTF8.GetString(writtenBytes);
                return jsonString;
            }
            catch
            {
                // Si falla la serialización optimizada, usar método estándar como fallback
                return logEntry.ToJson();
            }
            finally
            {
                writer.Dispose();
            }
        }

        /// <summary>
        /// Serializa un StructuredLogEntry a bytes UTF-8 usando ArrayBufferWriter
        /// Útil para envío directo a Kafka, Elasticsearch, etc. sin conversión string
        /// </summary>
        /// <param name="logEntry">Entrada de log a serializar</param>
        /// <returns>Bytes UTF-8 serializados (copia del buffer, seguro para usar después)</returns>
        public static (byte[] Buffer, int Length) SerializeToUtf8Bytes(StructuredLogEntry logEntry)
        {
            var bufferWriter = new ArrayBufferWriter<byte>(InitialBufferSize);
            var writer = new Utf8JsonWriter(bufferWriter);
            
            try
            {
                JsonSerializer.Serialize(writer, logEntry, LogEntryJsonContext.Default.StructuredLogEntry);
                writer.Flush();

                var writtenBytes = bufferWriter.WrittenSpan;
                
                // Crear copia del buffer para retornar (seguro para usar después)
                var result = new byte[writtenBytes.Length];
                writtenBytes.CopyTo(result);
                
                return (result, writtenBytes.Length);
            }
            finally
            {
                writer.Dispose();
            }
        }
    }
}
