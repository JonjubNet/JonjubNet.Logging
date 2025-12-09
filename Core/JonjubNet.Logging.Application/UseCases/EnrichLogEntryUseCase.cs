using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace JonjubNet.Logging.Application.UseCases
{
    /// <summary>
    /// Caso de uso para enriquecer una entrada de log con información adicional
    /// </summary>
    public class EnrichLogEntryUseCase
    {
        private readonly IHttpContextProvider? _httpContextProvider;
        private readonly ICurrentUserService? _currentUserService;
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly ILogScopeManager? _scopeManager;

        public EnrichLogEntryUseCase(
            ILoggingConfigurationManager configurationManager,
            IHttpContextProvider? httpContextProvider = null,
            ICurrentUserService? currentUserService = null,
            ILogScopeManager? scopeManager = null)
        {
            _configurationManager = configurationManager;
            _httpContextProvider = httpContextProvider;
            _currentUserService = currentUserService;
            _scopeManager = scopeManager;
        }

        /// <summary>
        /// Enriquece una entrada de log con información adicional (completo)
        /// </summary>
        public StructuredLogEntry Execute(StructuredLogEntry logEntry)
        {
            var configuration = _configurationManager.Current;
            
            // Enriquecer con información del servicio
            if (configuration.Enrichment.IncludeServiceInfo)
            {
                if (string.IsNullOrEmpty(logEntry.ServiceName))
                    logEntry.ServiceName = configuration.ServiceName;
                if (string.IsNullOrEmpty(logEntry.Environment))
                    logEntry.Environment = configuration.Environment;
                if (string.IsNullOrEmpty(logEntry.Version))
                    logEntry.Version = configuration.Version;
            }

            // Enriquecer con información del sistema
            if (configuration.Enrichment.IncludeMachineName)
                logEntry.MachineName = Environment.MachineName;

            if (configuration.Enrichment.IncludeProcess)
                logEntry.ProcessId = JonjubNet.Logging.Domain.Common.GCOptimizationHelpers.ProcessIdToString(Environment.ProcessId);

            if (configuration.Enrichment.IncludeThread)
                logEntry.ThreadId = JonjubNet.Logging.Domain.Common.GCOptimizationHelpers.ThreadIdToString(Thread.CurrentThread.ManagedThreadId);

            // Enriquecer con información del usuario
            if (_currentUserService != null)
            {
                logEntry.UserId = _currentUserService.GetCurrentUserId() ?? string.Empty;
                logEntry.UserName = _currentUserService.GetCurrentUserName() ?? string.Empty;
            }

            // Enriquecer con información HTTP
            if (_httpContextProvider != null && configuration.Enrichment.HttpCapture != null)
            {
                var httpCapture = configuration.Enrichment.HttpCapture;

                logEntry.RequestPath = _httpContextProvider.GetRequestPath();
                logEntry.RequestMethod = _httpContextProvider.GetRequestMethod();
                logEntry.StatusCode = _httpContextProvider.GetStatusCode();
                logEntry.ClientIp = _httpContextProvider.GetClientIp();
                logEntry.UserAgent = _httpContextProvider.GetUserAgent();

                if (httpCapture.IncludeQueryString)
                    logEntry.QueryString = _httpContextProvider.GetQueryString();

                if (httpCapture.IncludeRequestHeaders)
                    logEntry.RequestHeaders = _httpContextProvider.GetRequestHeaders(httpCapture.SensitiveHeaders);

                if (httpCapture.IncludeResponseHeaders)
                    logEntry.ResponseHeaders = _httpContextProvider.GetResponseHeaders();

                if (httpCapture.IncludeRequestBody)
                    logEntry.RequestBody = _httpContextProvider.GetRequestBody(httpCapture.MaxBodySizeBytes);

                if (httpCapture.IncludeResponseBody)
                    logEntry.ResponseBody = _httpContextProvider.GetResponseBody(httpCapture.MaxBodySizeBytes);
            }

            // Agregar propiedades estáticas
            // OPTIMIZACIÓN: Usar TryAdd en lugar de ContainsKey + asignación (una sola operación)
            foreach (var staticProperty in configuration.Enrichment.StaticProperties)
            {
                logEntry.Properties.TryAdd(staticProperty.Key, staticProperty.Value);
            }

            // Agregar propiedades de scopes activos
            if (_scopeManager != null)
            {
                var scopeProperties = _scopeManager.GetCurrentScopeProperties();
                // OPTIMIZACIÓN: Usar TryAdd en lugar de ContainsKey + asignación (una sola operación)
                foreach (var scopeProperty in scopeProperties)
                {
                    // Las propiedades del log entry tienen prioridad sobre las del scope
                    logEntry.Properties.TryAdd(scopeProperty.Key, scopeProperty.Value);
                }
            }

            return logEntry;
        }

        /// <summary>
        /// Enriquece una entrada de log con solo información esencial (rápido, para encolado)
        /// El resto del enriquecimiento se completa en background antes de enviar
        /// </summary>
        public StructuredLogEntry ExecuteMinimal(StructuredLogEntry logEntry)
        {
            var configuration = _configurationManager.Current;
            
            // Solo enriquecer lo esencial (rápido, sin acceso a HTTP context pesado)
            if (configuration.Enrichment.IncludeServiceInfo)
            {
                if (string.IsNullOrEmpty(logEntry.ServiceName))
                    logEntry.ServiceName = configuration.ServiceName;
                if (string.IsNullOrEmpty(logEntry.Environment))
                    logEntry.Environment = configuration.Environment;
                if (string.IsNullOrEmpty(logEntry.Version))
                    logEntry.Version = configuration.Version;
            }

            // Información del sistema (rápido)
            if (configuration.Enrichment.IncludeMachineName)
                logEntry.MachineName = Environment.MachineName;

            if (configuration.Enrichment.IncludeProcess)
                logEntry.ProcessId = JonjubNet.Logging.Domain.Common.GCOptimizationHelpers.ProcessIdToString(Environment.ProcessId);

            if (configuration.Enrichment.IncludeThread)
                logEntry.ThreadId = JonjubNet.Logging.Domain.Common.GCOptimizationHelpers.ThreadIdToString(Thread.CurrentThread.ManagedThreadId);

            // Usuario (rápido si está en cache, lento si accede a HTTP context)
            if (_currentUserService != null)
            {
                logEntry.UserId = _currentUserService.GetCurrentUserId() ?? string.Empty;
                logEntry.UserName = _currentUserService.GetCurrentUserName() ?? string.Empty;
            }

            // Agregar propiedades estáticas (rápido)
            // OPTIMIZACIÓN: Usar TryAdd en lugar de ContainsKey + asignación
            foreach (var staticProperty in configuration.Enrichment.StaticProperties)
            {
                logEntry.Properties.TryAdd(staticProperty.Key, staticProperty.Value);
            }

            // Agregar propiedades de scopes activos (rápido)
            // OPTIMIZACIÓN: Usar TryAdd en lugar de ContainsKey + asignación
            if (_scopeManager != null)
            {
                var scopeProperties = _scopeManager.GetCurrentScopeProperties();
                foreach (var scopeProperty in scopeProperties)
                {
                    logEntry.Properties.TryAdd(scopeProperty.Key, scopeProperty.Value);
                }
            }

            // NO enriquecer HTTP context aquí (lento) - se hace en background
            // Marcamos que necesita enriquecimiento completo
            logEntry.Properties["_NeedsFullEnrichment"] = true;

            return logEntry;
        }

        /// <summary>
        /// Completa el enriquecimiento de un log entry (HTTP context y otros datos pesados)
        /// Se ejecuta en background antes de enviar a sinks
        /// </summary>
        public StructuredLogEntry CompleteEnrichment(StructuredLogEntry logEntry)
        {
            var configuration = _configurationManager.Current;

            // Remover flag de enriquecimiento pendiente
            logEntry.Properties.Remove("_NeedsFullEnrichment");

            // Enriquecer con información HTTP (puede ser lento)
            if (_httpContextProvider != null && configuration.Enrichment.HttpCapture != null)
            {
                var httpCapture = configuration.Enrichment.HttpCapture;

                logEntry.RequestPath = _httpContextProvider.GetRequestPath();
                logEntry.RequestMethod = _httpContextProvider.GetRequestMethod();
                logEntry.StatusCode = _httpContextProvider.GetStatusCode();
                logEntry.ClientIp = _httpContextProvider.GetClientIp();
                logEntry.UserAgent = _httpContextProvider.GetUserAgent();

                if (httpCapture.IncludeQueryString)
                    logEntry.QueryString = _httpContextProvider.GetQueryString();

                if (httpCapture.IncludeRequestHeaders)
                    logEntry.RequestHeaders = _httpContextProvider.GetRequestHeaders(httpCapture.SensitiveHeaders);

                if (httpCapture.IncludeResponseHeaders)
                    logEntry.ResponseHeaders = _httpContextProvider.GetResponseHeaders();

                if (httpCapture.IncludeRequestBody)
                    logEntry.RequestBody = _httpContextProvider.GetRequestBody(httpCapture.MaxBodySizeBytes);

                if (httpCapture.IncludeResponseBody)
                    logEntry.ResponseBody = _httpContextProvider.GetResponseBody(httpCapture.MaxBodySizeBytes);
            }

            return logEntry;
        }
    }
}

