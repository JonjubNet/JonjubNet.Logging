#if MEDIATR_AVAILABLE
using MediatR;
#endif
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using JonjubNet.Logging.Interfaces;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Behaviours
{
    /// <summary>
    /// Behavior que registra automáticamente el inicio y fin de todas las operaciones MediatR
    /// sin necesidad de código manual en cada handler
    /// Requiere que MediatR esté instalado en la aplicación que usa esta biblioteca
    /// </summary>
#if MEDIATR_AVAILABLE
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
        where TRequest : IRequest<TResponse>
#else
    // Clase deshabilitada - requiere MediatR para funcionar
    // Para habilitar, instala el paquete MediatR completo (no solo MediatR.Contracts) 
    // y define MEDIATR_AVAILABLE en las propiedades del proyecto
    internal class LoggingBehaviour<TRequest, TResponse>
        where TRequest : class
#endif
    {
        private readonly IStructuredLoggingService _loggingService;
        private readonly ICurrentUserService? _currentUserService;
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

        public LoggingBehaviour(
            IStructuredLoggingService loggingService,
            ICurrentUserService? currentUserService,
            ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        {
            _loggingService = loggingService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

#if MEDIATR_AVAILABLE
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
#else
        // Método deshabilitado - requiere MediatR
        public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
#endif
        {
            var stopwatch = Stopwatch.StartNew();
            var userId = _currentUserService?.GetCurrentUserId() ?? "Anonymous";
            var userName = _currentUserService?.GetCurrentUserName() ?? "Anonymous";
            var success = false;
            var errorMessage = string.Empty;
            Exception? operationException = null;

            // Extraer nombre de la operación del tipo de request
            var operationName = ExtractOperationName(typeof(TRequest).Name);
            var entityType = ExtractEntityType(typeof(TRequest).Name);

            try
            {
                // Ejecutar el handler
                var response = await next();
                success = true;
                return response;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                operationException = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                try
                {
                    // Extraer información del request y response
                    var inputData = ExtractInputData(request);
                    var outputData = ExtractOutputData(success, operationException);

                    // LOGGING: Un solo evento estructurado consolidado con toda la información
                    var logContext = new Dictionary<string, object>
                    {
                        { "BusinessOperation", operationName },
                        { "EntityType", entityType },
                        { "Action", ExtractAction(operationName) },
                        { "Input", inputData },
                        { "Output", outputData },
                        { "UserAction", new Dictionary<string, object>
                            {
                                { "UserId", userId },
                                { "UserName", userName },
                                { "ActionType", ExtractAction(operationName) }
                            }
                        }
                    };

                    _loggingService.LogOperationEnd(
                        operationName, 
                        "BusinessLogic", 
                        stopwatch.ElapsedMilliseconds, 
                        logContext, 
                        success, 
                        operationException);
                }
                catch (Exception logEx)
                {
                    // No fallar la operación principal si el logging falla
                    _logger.LogError(logEx, "Error al registrar log estructurado para {Operation}", operationName);
                }
            }
        }

        private string ExtractOperationName(string requestTypeName)
        {
            // Ejemplo: "CreateCatalogCommand" -> "CreateCatalog"
            if (requestTypeName.EndsWith("Command") || requestTypeName.EndsWith("Query"))
            {
                return requestTypeName.Substring(0, requestTypeName.Length - 7);
            }
            return requestTypeName;
        }

        private string ExtractEntityType(string requestTypeName)
        {
            // Ejemplo: "CreateCatalogCommand" -> "Catalog"
            // Ejemplo: "UpdateCatalogDetailCommand" -> "CatalogDetail"
            if (requestTypeName.Contains("CatalogDetail"))
                return "CatalogDetail";
            if (requestTypeName.Contains("Catalog"))
                return "Catalog";
            return "Unknown";
        }

        private string ExtractAction(string operationName)
        {
            if (operationName.StartsWith("Create"))
                return "CREATE";
            if (operationName.StartsWith("Update"))
                return "UPDATE";
            if (operationName.StartsWith("Delete"))
                return "DELETE";
            if (operationName.StartsWith("Get") || operationName.StartsWith("Query"))
                return "READ";
            return "UNKNOWN";
        }

        private Dictionary<string, object> ExtractInputData(TRequest request)
        {
            var inputData = new Dictionary<string, object>();
            
            try
            {
                // Usar reflexión para extraer propiedades públicas del request
                var properties = typeof(TRequest).GetProperties();
                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(request);
                        if (value != null)
                        {
                            // Serializar objetos complejos a JSON string para evitar problemas de serialización
                            if (prop.PropertyType.IsPrimitive || 
                                prop.PropertyType == typeof(string) || 
                                prop.PropertyType == typeof(DateTime) ||
                                prop.PropertyType == typeof(DateTime?) ||
                                prop.PropertyType == typeof(Guid) ||
                                prop.PropertyType == typeof(Guid?))
                            {
                                inputData[prop.Name] = value;
                            }
                            else
                            {
                                inputData[prop.Name] = JsonSerializer.Serialize(value);
                            }
                        }
                    }
                    catch
                    {
                        // Ignorar propiedades que no se pueden leer
                    }
                }
            }
            catch
            {
                // Si falla la extracción, al menos registrar el tipo
                inputData["RequestType"] = typeof(TRequest).Name;
            }

            return inputData;
        }

        private Dictionary<string, object> ExtractOutputData(bool success, Exception? exception)
        {
            var outputData = new Dictionary<string, object>
            {
                { "Success", success }
            };

            if (!success && exception != null)
            {
                outputData["Error"] = exception.Message;
                outputData["ErrorType"] = exception.GetType().Name;
            }

            return outputData;
        }
    }
}

