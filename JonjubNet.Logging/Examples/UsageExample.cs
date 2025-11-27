using JonjubNet.Logging.Models;

namespace JonjubNet.Logging.Examples
{
    /// <summary>
    /// Ejemplo de uso del servicio de logging estructurado
    /// </summary>
    public class UsageExample
    {
        private readonly IStructuredLoggingService _loggingService;

        public UsageExample(IStructuredLoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Ejemplo de logging básico
        /// </summary>
        public void BasicLoggingExample()
        {
            // Log de información simple
            _loggingService.LogInformation("Aplicación iniciada correctamente");

            // Log con operación y categoría
            _loggingService.LogInformation("Usuario autenticado", "Authentication", "Security");

            // Log con propiedades adicionales
            _loggingService.LogInformation("Producto creado", "CreateProduct", "Business",
                properties: new Dictionary<string, object>
                {
                    { "ProductId", "PROD-12345" },
                    { "ProductName", "Laptop Gaming" },
                    { "Price", 1299.99 }
                });

            // Log de error con excepción
            try
            {
                throw new InvalidOperationException("Error de ejemplo");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error al procesar solicitud", "ProcessRequest", "General",
                    properties: new Dictionary<string, object> { { "RequestId", "REQ-12345" } },
                    exception: ex);
            }
        }

        /// <summary>
        /// Ejemplo de logging de operaciones
        /// </summary>
        public async Task OperationLoggingExample()
        {
            var operationName = "ProcessOrder";
            var category = "Business";

            // Iniciar operación
            _loggingService.LogOperationStart(operationName, category,
                properties: new Dictionary<string, object> { { "OrderId", "ORD-12345" } });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Simular procesamiento
                await Task.Delay(1000);

                // Operación exitosa
                stopwatch.Stop();
                _loggingService.LogOperationEnd(operationName, category, 
                    executionTimeMs: stopwatch.ElapsedMilliseconds,
                    properties: new Dictionary<string, object> 
                    { 
                        { "OrderId", "ORD-12345" },
                        { "Status", "Completed" }
                    });
            }
            catch (Exception ex)
            {
                // Operación fallida
                stopwatch.Stop();
                _loggingService.LogOperationEnd(operationName, category,
                    executionTimeMs: stopwatch.ElapsedMilliseconds,
                    success: false,
                    exception: ex);
                throw;
            }
        }

        /// <summary>
        /// Ejemplo de logging de eventos específicos
        /// </summary>
        public void EventLoggingExample()
        {
            // Evento de usuario
            _loggingService.LogUserAction("UpdateProfile", "User", "USER-12345",
                properties: new Dictionary<string, object>
                {
                    { "FieldUpdated", "Email" },
                    { "OldValue", "old@email.com" },
                    { "NewValue", "new@email.com" }
                });

            // Evento de seguridad
            _loggingService.LogSecurityEvent("FailedLogin", "Intento de login fallido",
                properties: new Dictionary<string, object>
                {
                    { "IP", "192.168.1.100" },
                    { "UserAgent", "Mozilla/5.0..." },
                    { "Attempts", 3 }
                });

            // Evento de auditoría
            _loggingService.LogAuditEvent("DataAccess", "Consulta de datos sensibles", 
                "User", "USER-12345",
                properties: new Dictionary<string, object>
                {
                    { "DataAccessed", "PersonalInformation" },
                    { "AccessReason", "ProfileUpdate" }
                });
        }

        /// <summary>
        /// Ejemplo de logging personalizado
        /// </summary>
        public void CustomLoggingExample()
        {
            // Crear entrada de log personalizada
            var customLogEntry = new StructuredLogEntry
            {
                ServiceName = "MiServicio",
                Operation = "CustomOperation",
                LogLevel = LogLevel.Information,
                Message = "Operación personalizada ejecutada",
                Category = "Custom",
                UserId = "USER-12345",
                UserName = "john.doe",
                Environment = "Development",
                Version = "1.0.0",
                Properties = new Dictionary<string, object>
                {
                    { "CustomProperty1", "Valor1" },
                    { "CustomProperty2", 42 },
                    { "CustomProperty3", true }
                },
                Context = new Dictionary<string, object>
                {
                    { "CustomContext", "Información adicional" },
                    { "Timestamp", DateTime.UtcNow }
                }
            };

            // Enviar log personalizado
            _loggingService.LogCustom(customLogEntry);
        }

        /// <summary>
        /// Ejemplo de logging con diferentes niveles
        /// </summary>
        public void LogLevelsExample()
        {
            // Trace - Información muy detallada
            _loggingService.LogTrace("Iniciando validación de datos", "ValidateData", "Debug");

            // Debug - Información de depuración
            _loggingService.LogDebug("Datos validados correctamente", "ValidateData", "Debug",
                properties: new Dictionary<string, object> { { "ValidationTime", 50 } });

            // Information - Información general
            _loggingService.LogInformation("Proceso completado exitosamente", "ProcessData", "General");

            // Warning - Advertencias
            _loggingService.LogWarning("Límite de memoria alcanzado", "MemoryCheck", "System",
                properties: new Dictionary<string, object> { { "MemoryUsage", "85%" } });

            // Error - Errores
            _loggingService.LogError("Error de conexión a base de datos", "DatabaseConnection", "Database",
                properties: new Dictionary<string, object> { { "ConnectionString", "***" } });

            // Critical - Errores críticos
            _loggingService.LogCritical("Sistema no disponible", "SystemCheck", "System",
                properties: new Dictionary<string, object> { { "SystemStatus", "Down" } });
        }
    }
}
