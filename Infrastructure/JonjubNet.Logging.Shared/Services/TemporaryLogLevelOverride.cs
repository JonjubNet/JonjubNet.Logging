namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Representa un override temporal del nivel de log con expiración automática
    /// </summary>
    internal class TemporaryLogLevelOverride
    {
        public string? Category { get; set; }
        public string Level { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? OriginalLevel { get; set; } // Para restaurar después de expirar

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}

