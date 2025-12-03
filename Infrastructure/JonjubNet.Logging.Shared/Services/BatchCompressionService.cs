using System.IO.Compression;
using System.Text;
using System.Text.Json;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio de compresión de batches de logs
    /// </summary>
    public class BatchCompressionService : IBatchCompressionService
    {
        private readonly LoggingBatchingConfiguration _config;
        private readonly ILogger<BatchCompressionService>? _logger;

        public bool IsCompressionEnabled => _config.EnableCompression;

        public BatchCompressionService(
            ILoggingConfigurationManager configurationManager,
            ILogger<BatchCompressionService>? logger = null)
        {
            _config = configurationManager.Current.Batching;
            _logger = logger;
        }

        public async Task<CompressedBatch> CompressAsync(LogBatch batch, CancellationToken cancellationToken = default)
        {
            // Serializar batch a JSON
            var json = JsonSerializer.Serialize(batch.LogEntries);
            
            if (!_config.EnableCompression)
            {
                // Si compresión está deshabilitada, retornar sin comprimir
                var bytes = Encoding.UTF8.GetBytes(json);
                return new CompressedBatch
                {
                    CompressedData = bytes,
                    SinkName = batch.SinkName,
                    OriginalSize = bytes.Length
                };
            }
            var originalBytes = Encoding.UTF8.GetBytes(json);

            // Comprimir usando GZip
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, GetCompressionLevel(), leaveOpen: true))
            {
                await gzipStream.WriteAsync(originalBytes, cancellationToken);
            }

            var compressedBytes = outputStream.ToArray();
            var compressionRatio = originalBytes.Length > 0 
                ? (double)compressedBytes.Length / originalBytes.Length 
                : 0;

            _logger?.LogDebug("Batch comprimido: {OriginalSize} -> {CompressedSize} bytes (ratio: {Ratio:P2})",
                originalBytes.Length, compressedBytes.Length, compressionRatio);

            return new CompressedBatch
            {
                CompressedData = compressedBytes,
                SinkName = batch.SinkName,
                OriginalSize = originalBytes.Length,
                CreatedAt = batch.CreatedAt
            };
        }

        public async Task<LogBatch> DecompressAsync(CompressedBatch compressedBatch, CancellationToken cancellationToken = default)
        {
            byte[] decompressedBytes;

            if (_config.EnableCompression)
            {
                // Descomprimir usando GZip
                using var inputStream = new MemoryStream(compressedBatch.CompressedData);
                using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                using var outputStream = new MemoryStream();

                await gzipStream.CopyToAsync(outputStream, cancellationToken);
                decompressedBytes = outputStream.ToArray();
            }
            else
            {
                decompressedBytes = compressedBatch.CompressedData;
            }

            // Deserializar JSON
            var jsonString = Encoding.UTF8.GetString(decompressedBytes);
            var logEntries = JsonSerializer.Deserialize<List<Domain.Entities.StructuredLogEntry>>(jsonString)
                ?? new List<Domain.Entities.StructuredLogEntry>();

            return new LogBatch
            {
                LogEntries = logEntries,
                SinkName = compressedBatch.SinkName,
                CreatedAt = compressedBatch.CreatedAt
            };
        }

        private CompressionLevel GetCompressionLevel()
        {
            return _config.CompressionLevel switch
            {
                "Fastest" => CompressionLevel.Fastest,
                "Optimal" => CompressionLevel.Optimal,
                "SmallestSize" => CompressionLevel.SmallestSize,
                "NoCompression" => CompressionLevel.NoCompression,
                _ => CompressionLevel.Optimal
            };
        }
    }
}

