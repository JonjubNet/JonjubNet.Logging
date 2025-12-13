namespace JonjubNet.Logging.Application.Interfaces
{
    /// <summary>
    /// Interfaz para servicios de encriptación
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encripta datos en tránsito (para HTTP sinks)
        /// </summary>
        /// <param name="data">Datos a encriptar</param>
        /// <returns>Datos encriptados</returns>
        Task<byte[]> EncryptInTransitAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Desencripta datos en tránsito
        /// </summary>
        /// <param name="encryptedData">Datos encriptados</param>
        /// <returns>Datos desencriptados</returns>
        Task<byte[]> DecryptInTransitAsync(byte[] encryptedData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Encripta datos en reposo (para file sink)
        /// </summary>
        /// <param name="data">Datos a encriptar</param>
        /// <param name="keyId">ID de la clave a usar (para rotación)</param>
        /// <returns>Datos encriptados</returns>
        Task<byte[]> EncryptAtRestAsync(byte[] data, string? keyId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Desencripta datos en reposo
        /// </summary>
        /// <param name="encryptedData">Datos encriptados</param>
        /// <param name="keyId">ID de la clave usada (opcional, se detecta automáticamente si es null)</param>
        /// <returns>Datos desencriptados</returns>
        Task<byte[]> DecryptAtRestAsync(byte[] encryptedData, string? keyId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el ID de la clave activa actual
        /// </summary>
        string GetCurrentKeyId();

        /// <summary>
        /// Rota la clave de encriptación (genera nueva clave y mantiene la anterior para descifrado)
        /// </summary>
        Task RotateKeyAsync(CancellationToken cancellationToken = default);
    }
}

