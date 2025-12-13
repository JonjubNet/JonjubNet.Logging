using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JonjubNet.Logging.Shared.Services
{
    /// <summary>
    /// Servicio de audit logging para registrar cambios de configuración y accesos a logs sensibles
    /// </summary>
    public class AuditLoggingService : IAuditLoggingService
    {
        private readonly ILoggingConfigurationManager _configurationManager;
        private readonly IStructuredLoggingService _loggingService;
        private readonly ILogger<AuditLoggingService>? _logger;
        private readonly string? _auditLogPath;

        public AuditLoggingService(
            ILoggingConfigurationManager configurationManager,
            IStructuredLoggingService loggingService,
            ILogger<AuditLoggingService>? logger = null)
        {
            _configurationManager = configurationManager;
            _loggingService = loggingService;
            _logger = logger;
            _auditLogPath = _configurationManager.Current.Security.Audit.AuditLogPath;

            // Subscribe to configuration changes if enabled
            if (_configurationManager.Current.Security.Audit.LogConfigurationChanges)
            {
                _configurationManager.ConfigurationChanged += OnConfigurationChanged;
            }
        }

        private void OnConfigurationChanged(LoggingConfiguration newConfiguration)
        {
            // This will be handled by explicit calls to LogConfigurationChangeAsync
        }

        public Task LogConfigurationChangeAsync(
            string changeType,
            string configurationSection,
            object? oldValue = null,
            object? newValue = null,
            string? changedBy = null,
            CancellationToken cancellationToken = default)
        {
            var config = _configurationManager.Current.Security.Audit;
            if (!config.Enabled || !config.LogConfigurationChanges)
            {
                return Task.CompletedTask;
            }

            try
            {
                var properties = new Dictionary<string, object>
                {
                    ["ChangeType"] = changeType,
                    ["ConfigurationSection"] = configurationSection,
                    ["Timestamp"] = DateTime.UtcNow
                };

                if (oldValue != null)
                {
                    properties["OldValue"] = JsonSerializer.Serialize(oldValue);
                }

                if (newValue != null)
                {
                    properties["NewValue"] = JsonSerializer.Serialize(newValue);
                }

                if (!string.IsNullOrEmpty(changedBy))
                {
                    properties["ChangedBy"] = changedBy;
                }

                _loggingService.LogAuditEvent(
                    "ConfigurationChange",
                    $"Configuración {changeType}: {configurationSection}",
                    entityType: "Configuration",
                    entityId: configurationSection,
                    properties: properties
                );

                // Also write to dedicated audit log file if configured
                if (!string.IsNullOrEmpty(_auditLogPath))
                {
                    WriteToAuditLogFile(properties);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al registrar cambio de configuración");
            }

            return Task.CompletedTask;
        }

        public Task LogSensitiveAccessAsync(
            StructuredLogEntry logEntry,
            string? accessedBy = null,
            string accessMethod = "View",
            CancellationToken cancellationToken = default)
        {
            var config = _configurationManager.Current.Security.Audit;
            if (!config.Enabled || !config.LogSensitiveAccess)
            {
                return Task.CompletedTask;
            }

            if (!IsSensitive(logEntry))
            {
                return Task.CompletedTask;
            }

            try
            {
                var properties = new Dictionary<string, object>
                {
                    ["AccessMethod"] = accessMethod,
                    ["AccessedLogCorrelationId"] = logEntry.CorrelationId ?? "",
                    ["AccessedLogRequestId"] = logEntry.RequestId ?? "",
                    ["AccessedLogLevel"] = logEntry.LogLevel,
                    ["AccessedLogCategory"] = logEntry.Category ?? "",
                    ["AccessedLogOperation"] = logEntry.Operation ?? "",
                    ["Timestamp"] = DateTime.UtcNow
                };

                if (!string.IsNullOrEmpty(accessedBy))
                {
                    properties["AccessedBy"] = accessedBy;
                }

                if (!string.IsNullOrEmpty(logEntry.UserId))
                {
                    properties["LogUserId"] = logEntry.UserId;
                }

                _loggingService.LogSecurityEvent(
                    "SensitiveLogAccess",
                    $"Acceso a log sensible: {logEntry.Message}",
                    properties: properties
                );

                // Also write to dedicated audit log file if configured
                if (!string.IsNullOrEmpty(_auditLogPath))
                {
                    WriteToAuditLogFile(properties);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al registrar acceso a log sensible");
            }

            return Task.CompletedTask;
        }

        public Task LogComplianceEventAsync(
            string standard,
            string eventType,
            string description,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            var config = _configurationManager.Current.Security.Audit;
            if (!config.Enabled || !config.EnableComplianceTracking)
            {
                return Task.CompletedTask;
            }

            if (!config.ComplianceStandards.Contains(standard))
            {
                return Task.CompletedTask;
            }

            try
            {
                var properties = new Dictionary<string, object>
                {
                    ["ComplianceStandard"] = standard,
                    ["EventType"] = eventType,
                    ["Description"] = description,
                    ["Timestamp"] = DateTime.UtcNow
                };

                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        properties[$"Metadata_{kvp.Key}"] = kvp.Value;
                    }
                }

                _loggingService.LogAuditEvent(
                    "ComplianceEvent",
                    $"{standard}: {eventType} - {description}",
                    entityType: "Compliance",
                    entityId: standard,
                    properties: properties
                );

                // Also write to dedicated audit log file if configured
                if (!string.IsNullOrEmpty(_auditLogPath))
                {
                    WriteToAuditLogFile(properties);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al registrar evento de cumplimiento");
            }

            return Task.CompletedTask;
        }

        public bool IsSensitive(StructuredLogEntry logEntry)
        {
            var config = _configurationManager.Current.Security.Audit;
            if (!config.Enabled)
            {
                return false;
            }

            // Check by category
            if (!string.IsNullOrEmpty(logEntry.Category) && 
                config.SensitiveCategories.Contains(logEntry.Category, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check by level
            if (config.SensitiveLevels.Contains(logEntry.LogLevel, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void WriteToAuditLogFile(Dictionary<string, object> auditData)
        {
            if (string.IsNullOrEmpty(_auditLogPath))
                return;

            try
            {
                var directory = Path.GetDirectoryName(_auditLogPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var logLine = JsonSerializer.Serialize(auditData) + Environment.NewLine;
                File.AppendAllText(_auditLogPath, logLine);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al escribir en archivo de auditoría: {Path}", _auditLogPath);
            }
        }
    }
}

