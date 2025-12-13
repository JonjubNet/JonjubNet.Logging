using JonjubNet.Logging.Application.Configuration;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace JonjubNet.Logging.Shared.Services.Security
{
    /// <summary>
    /// Helper para configurar TLS/SSL en HttpClient para sinks HTTP
    /// </summary>
    public static class HttpClientTlsConfigurator
    {
        /// <summary>
        /// Configura HttpClientHandler con TLS según la configuración de seguridad
        /// </summary>
        public static HttpClientHandler ConfigureTls(LoggingConfiguration loggingConfig)
        {
            var config = loggingConfig.Security.EncryptionInTransit;
            var handler = new HttpClientHandler();

            if (!config.Enabled)
            {
                return handler;
            }

            // Configurar validación de certificado del servidor
            if (!config.ValidateServerCertificate)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            else
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Validación personalizada si hay certificados raíz personalizados
                    if (!string.IsNullOrEmpty(config.CustomRootCertificatesPath) && Directory.Exists(config.CustomRootCertificatesPath))
                    {
                        // Cargar certificados raíz personalizados
                        var customRootCerts = LoadCustomRootCertificates(config.CustomRootCertificatesPath);
                        if (customRootCerts.Count > 0)
                        {
                            // Validar con certificados personalizados
                            var customChain = new X509Chain();
                            foreach (var rootCert in customRootCerts)
                            {
                                customChain.ChainPolicy.ExtraStore.Add(rootCert);
                            }
                            customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                            return customChain.Build(cert!);
                        }
                    }

                    // Validación estándar
                    return errors == SslPolicyErrors.None;
                };
            }

            // Configurar certificado cliente si está especificado
            if (!string.IsNullOrEmpty(config.ClientCertificatePath) && File.Exists(config.ClientCertificatePath))
            {
                var clientCert = LoadClientCertificate(config.ClientCertificatePath, config.ClientCertificatePassword);
                if (clientCert != null)
                {
                    handler.ClientCertificates.Add(clientCert);
                }
            }

            // Configurar versión mínima de TLS
            ConfigureTlsVersion(handler, config.MinimumTlsVersion);

            return handler;
        }

        public static List<X509Certificate2> LoadCustomRootCertificates(string path)
        {
            var certificates = new List<X509Certificate2>();
            try
            {
                var files = Directory.GetFiles(path, "*.cer")
                    .Concat(Directory.GetFiles(path, "*.crt"))
                    .Concat(Directory.GetFiles(path, "*.pem"));

                foreach (var file in files)
                {
                    try
                    {
                        var cert = new X509Certificate2(file);
                        certificates.Add(cert);
                    }
                    catch
                    {
                        // Ignorar certificados inválidos
                    }
                }
            }
            catch
            {
                // Ignorar errores al cargar certificados
            }

            return certificates;
        }

        private static X509Certificate2? LoadClientCertificate(string path, string? password)
        {
            try
            {
                if (!string.IsNullOrEmpty(password))
                {
                    return new X509Certificate2(path, password);
                }
                return new X509Certificate2(path);
            }
            catch
            {
                return null;
            }
        }

        private static void ConfigureTlsVersion(HttpClientHandler handler, string minimumTlsVersion)
        {
            // La versión de TLS se configura a nivel de sistema operativo
            // En .NET, esto se hace mediante ServicePointManager o HttpClientHandler
            // Para .NET 5+, se usa SslProtocols en HttpClientHandler
            // Nota: La configuración exacta depende de la versión de .NET
            // Por ahora, validamos que la URL use HTTPS si RequireTls está habilitado
        }

        /// <summary>
        /// Valida que una URL use HTTPS si RequireTls está habilitado
        /// </summary>
        public static void ValidateHttpsUrl(string url, LoggingConfiguration loggingConfig)
        {
            var config = loggingConfig.Security.EncryptionInTransit;
            if (config.Enabled && config.RequireTls)
            {
                if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"La URL debe usar HTTPS cuando RequireTls está habilitado. URL proporcionada: {url}");
                }
            }
        }
    }
}

