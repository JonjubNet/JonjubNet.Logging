using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio de encriptación para logs en tránsito y en reposo
    /// </summary>
    public class EncryptionService : IEncryptionService, IDisposable
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly ILogger<EncryptionService>? _logger;
        private readonly Dictionary<string, Aes> _encryptionKeys = new();
        private readonly object _keysLock = new();
        private string? _currentKeyId;
        private bool _disposed = false;

        public EncryptionService(
            ILoggingConfigurationManager configurationManager,
            ILogger<EncryptionService>? logger = null)
        {
            _configurationManager = configurationManager;
            _logger = logger;
            InitializeKeys();
        }

        private void InitializeKeys()
        {
            var config = _configurationManager.Current.Security.EncryptionAtRest;
            if (!config.Enabled || string.IsNullOrEmpty(config.EncryptionKeyPath))
                return;

            try
            {
                LoadEncryptionKey(config.EncryptionKeyPath, config.EncryptionKeyPassword);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al inicializar claves de encriptación");
            }
        }

        private void LoadEncryptionKey(string keyPath, string? password)
        {
            if (!File.Exists(keyPath))
            {
                // Generar nueva clave si no existe
                GenerateAndSaveKey(keyPath, password);
                return;
            }

            var keyData = File.ReadAllBytes(keyPath);
            byte[] key;

            if (!string.IsNullOrEmpty(password))
            {
                // Desencriptar la clave usando la contraseña
                key = DecryptKeyWithPassword(keyData, password);
            }
            else
            {
                key = keyData;
            }

            var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            _currentKeyId = "default";
            _encryptionKeys[_currentKeyId] = aes;

            _logger?.LogInformation("Clave de encriptación cargada: {KeyId}", _currentKeyId);
        }

        private void GenerateAndSaveKey(string keyPath, string? password)
        {
            var aes = Aes.Create();
            aes.GenerateKey();

            byte[] keyToSave = aes.Key;
            if (!string.IsNullOrEmpty(password))
            {
                keyToSave = EncryptKeyWithPassword(aes.Key, password);
            }

            var directory = Path.GetDirectoryName(keyPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(keyPath, keyToSave);
            _currentKeyId = "default";
            _encryptionKeys[_currentKeyId] = aes;

            _logger?.LogInformation("Nueva clave de encriptación generada y guardada: {KeyId}", _currentKeyId);
        }

        private byte[] EncryptKeyWithPassword(byte[] key, string password)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("JonjubNet.Logging.Salt"), 10000, HashAlgorithmName.SHA256);
            using var aes = Aes.Create();
            aes.Key = deriveBytes.GetBytes(32);
            aes.IV = deriveBytes.GetBytes(16);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(key, 0, key.Length);
        }

        private byte[] DecryptKeyWithPassword(byte[] encryptedKey, string password)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("JonjubNet.Logging.Salt"), 10000, HashAlgorithmName.SHA256);
            using var aes = Aes.Create();
            aes.Key = deriveBytes.GetBytes(32);
            aes.IV = deriveBytes.GetBytes(16);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedKey, 0, encryptedKey.Length);
        }

        public Task<byte[]> EncryptInTransitAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            // Para encriptación en tránsito, normalmente se usa TLS/SSL a nivel de HTTP
            // Esta implementación es para casos especiales donde se necesita encriptación adicional
            var config = _configurationManager.Current.Security.EncryptionInTransit;
            if (!config.Enabled)
            {
                return Task.FromResult(data);
            }

            // En producción, esto podría usar TLS adicional o encriptación de payload
            // Por ahora, retornamos los datos sin modificar ya que TLS se maneja a nivel HTTP
            return Task.FromResult(data);
        }

        public Task<byte[]> DecryptInTransitAsync(byte[] encryptedData, CancellationToken cancellationToken = default)
        {
            var config = _configurationManager.Current.Security.EncryptionInTransit;
            if (!config.Enabled)
            {
                return Task.FromResult(encryptedData);
            }

            return Task.FromResult(encryptedData);
        }

        public Task<byte[]> EncryptAtRestAsync(byte[] data, string? keyId = null, CancellationToken cancellationToken = default)
        {
            var config = _configurationManager.Current.Security.EncryptionAtRest;
            if (!config.Enabled)
            {
                return Task.FromResult(data);
            }

            lock (_keysLock)
            {
                var effectiveKeyId = keyId ?? _currentKeyId ?? "default";
                if (!_encryptionKeys.TryGetValue(effectiveKeyId, out var aes))
                {
                    throw new InvalidOperationException($"Clave de encriptación no encontrada: {effectiveKeyId}");
                }

                aes.GenerateIV();
                using var encryptor = aes.CreateEncryptor();
                var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

                // Prepend IV and keyId to encrypted data
                var result = new List<byte>();
                result.AddRange(Encoding.UTF8.GetBytes($"{effectiveKeyId}:"));
                result.AddRange(BitConverter.GetBytes(aes.IV.Length));
                result.AddRange(aes.IV);
                result.AddRange(encrypted);

                return Task.FromResult(result.ToArray());
            }
        }

        public Task<byte[]> DecryptAtRestAsync(byte[] encryptedData, string? keyId = null, CancellationToken cancellationToken = default)
        {
            var config = _configurationManager.Current.Security.EncryptionAtRest;
            if (!config.Enabled)
            {
                return Task.FromResult(encryptedData);
            }

            lock (_keysLock)
            {
                // Extract keyId, IV, and encrypted data
                var keyIdEnd = Array.IndexOf(encryptedData, (byte)':');
                if (keyIdEnd < 0)
                {
                    throw new InvalidOperationException("Formato de datos encriptados inválido");
                }

                var extractedKeyId = Encoding.UTF8.GetString(encryptedData, 0, keyIdEnd);
                var effectiveKeyId = keyId ?? extractedKeyId;

                var offset = keyIdEnd + 1;
                var ivLength = BitConverter.ToInt32(encryptedData, offset);
                offset += sizeof(int);

                var iv = new byte[ivLength];
                Array.Copy(encryptedData, offset, iv, 0, ivLength);
                offset += ivLength;

                var encrypted = new byte[encryptedData.Length - offset];
                Array.Copy(encryptedData, offset, encrypted, 0, encrypted.Length);

                // Try current key first, then previous keys
                Aes? aes = null;
                if (_encryptionKeys.TryGetValue(effectiveKeyId, out aes))
                {
                    // Key found, use it
                }
                else if (!string.IsNullOrEmpty(_configurationManager.Current.Security.EncryptionAtRest.PreviousKeysPath))
                {
                    // Try to load previous key
                    LoadPreviousKey(effectiveKeyId);
                    _encryptionKeys.TryGetValue(effectiveKeyId, out aes);
                }

                if (aes == null)
                {
                    throw new InvalidOperationException($"Clave de encriptación no encontrada: {effectiveKeyId}");
                }

                aes.IV = iv;
                using var decryptor = aes.CreateDecryptor();
                var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                return Task.FromResult(decrypted);
            }
        }

        private void LoadPreviousKey(string keyId)
        {
            var previousKeysPath = _configurationManager.Current.Security.EncryptionAtRest.PreviousKeysPath;
            if (string.IsNullOrEmpty(previousKeysPath) || !Directory.Exists(previousKeysPath))
                return;

            var keyFile = Path.Combine(previousKeysPath, $"{keyId}.key");
            if (!File.Exists(keyFile))
                return;

            try
            {
                var keyData = File.ReadAllBytes(keyFile);
                var password = _configurationManager.Current.Security.EncryptionAtRest.EncryptionKeyPassword;
                byte[] key;

                if (!string.IsNullOrEmpty(password))
                {
                    key = DecryptKeyWithPassword(keyData, password);
                }
                else
                {
                    key = keyData;
                }

                var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                _encryptionKeys[keyId] = aes;
                _logger?.LogInformation("Clave anterior cargada: {KeyId}", keyId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al cargar clave anterior: {KeyId}", keyId);
            }
        }

        public string GetCurrentKeyId()
        {
            return _currentKeyId ?? "default";
        }

        public async Task RotateKeyAsync(CancellationToken cancellationToken = default)
        {
            var config = _configurationManager.Current.Security.EncryptionAtRest;
            if (!config.Enabled || !config.EnableKeyRotation)
            {
                return;
            }

            lock (_keysLock)
            {
                // Save current key as previous
                if (_currentKeyId != null && _encryptionKeys.TryGetValue(_currentKeyId, out var oldAes))
                {
                    var previousKeysPath = config.PreviousKeysPath ?? Path.Combine(Path.GetDirectoryName(config.EncryptionKeyPath) ?? ".", "previous-keys");
                    if (!Directory.Exists(previousKeysPath))
                    {
                        Directory.CreateDirectory(previousKeysPath);
                    }

                    var previousKeyFile = Path.Combine(previousKeysPath, $"{_currentKeyId}.key");
                    byte[] keyToSave = oldAes.Key;
                    if (!string.IsNullOrEmpty(config.EncryptionKeyPassword))
                    {
                        keyToSave = EncryptKeyWithPassword(oldAes.Key, config.EncryptionKeyPassword);
                    }
                    File.WriteAllBytes(previousKeyFile, keyToSave);
                }

                // Generate new key
                var newKeyId = $"key-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                var newAes = Aes.Create();
                newAes.GenerateKey();

                byte[] newKeyToSave = newAes.Key;
                if (!string.IsNullOrEmpty(config.EncryptionKeyPassword))
                {
                    newKeyToSave = EncryptKeyWithPassword(newAes.Key, config.EncryptionKeyPassword);
                }

                File.WriteAllBytes(config.EncryptionKeyPath!, newKeyToSave);
                _encryptionKeys[newKeyId] = newAes;
                _currentKeyId = newKeyId;

                _logger?.LogInformation("Clave de encriptación rotada: {OldKeyId} -> {NewKeyId}", _currentKeyId, newKeyId);
            }

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_keysLock)
            {
                foreach (var aes in _encryptionKeys.Values)
                {
                    aes.Dispose();
                }
                _encryptionKeys.Clear();
            }

            _disposed = true;
        }
    }
}

