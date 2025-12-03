namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para servicio de compresión de batches
    /// </summary>
    public interface IBatchCompressionService
    {
        /// <summary>
        /// Comprime un batch de logs
        /// </summary>
        Task<CompressedBatch> CompressAsync(LogBatch batch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Descomprime un batch de logs
        /// </summary>
        Task<LogBatch> DecompressAsync(CompressedBatch compressedBatch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si la compresión está habilitada
        /// </summary>
        bool IsCompressionEnabled { get; }
    }

    /// <summary>
    /// Batch comprimido
    /// </summary>
    public class CompressedBatch
    {
        public byte[] CompressedData { get; set; } = Array.Empty<byte>();
        public string SinkName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int OriginalSize { get; set; }
        public int CompressedSize => CompressedData.Length;
        public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 0;
    }
}

