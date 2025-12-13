using MediatR;
using Microsoft.Extensions.Logging;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Common;
using System.Diagnostics;
using System.Text.Json;

namespace JonjubNet.Logging.Application.Behaviours
{
    /// <summary>
    /// Pipeline Behavior para registrar automáticamente todas las peticiones y respuestas de MediatR
    /// Optimizado para alto rendimiento con mínimo overhead
    /// </summary>
    /// <typeparam name="TRequest">Tipo de la petición</typeparam>
    /// <typeparam name="TResponse">Tipo de la respuesta</typeparam>
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IStructuredLoggingService _loggingService;
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
        
        // OPTIMIZACIÓN: Cache de JsonSerializerOptions para evitar allocations repetidas
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            MaxDepth = 3,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // OPTIMIZACIÓN: Pre-allocar capacidad estimada para diccionarios comunes
        private const int EstimatedPropertiesCapacity = 8;
        private const int EstimatedContextCapacity = 4;

        public LoggingBehaviour(
            IStructuredLoggingService loggingService,
            ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        {
            ArgumentNullException.ThrowIfNull(loggingService);
            ArgumentNullException.ThrowIfNull(logger);
            
            _loggingService = loggingService;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;
            
            // OPTIMIZACIÓN: Usar Stopwatch local en lugar de campo de instancia (mejor para threading)
            var stopwatch = Stopwatch.StartNew();

            // Log inicio de petición
            LogRequestStart(requestName, requestId, request);

            TResponse? response = default;
            Exception? exception = null;

            try
            {
                // Ejecutar el handler
                response = await next().ConfigureAwait(false);

                stopwatch.Stop();
                var executionTime = stopwatch.ElapsedMilliseconds;

                // Log éxito
                LogRequestSuccess(requestName, requestId, executionTime, startTime, response);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var executionTime = stopwatch.ElapsedMilliseconds;
                exception = ex;

                // Log error
                LogRequestError(requestName, requestId, executionTime, startTime, ex);

                throw;
            }
        }

        /// <summary>
        /// Registra el inicio de una petición
        /// </summary>
        private void LogRequestStart(string requestName, string requestId, TRequest request)
        {
            // OPTIMIZACIÓN: Usar DictionaryPool compartido en lugar de pool local
            var properties = DictionaryPool.Rent();
            try
            {
                // Pre-allocar capacidad para evitar redimensionamientos
                properties.EnsureCapacity(EstimatedPropertiesCapacity);
                
                properties["RequestId"] = requestId;
                properties["RequestType"] = requestName;
                properties["RequestName"] = requestName;

                // OPTIMIZACIÓN: Serialización JSON condicional (solo si request no es null)
                if (request != null)
                {
                    TrySerializeToProperty(properties, "RequestData", request);
                }

                // OPTIMIZACIÓN: Crear copia eficiente (el servicio asigna directamente, necesitamos copia)
                // El constructor de Dictionary es más eficiente que iterar manualmente
                var propertiesCopy = new Dictionary<string, object>(properties);

                _loggingService.LogInformation(
                    $"Iniciando procesamiento de petición: {requestName}",
                    "MediatR",
                    "Request",
                    properties: propertiesCopy);
            }
            catch (Exception ex)
            {
                // No permitir que errores de logging afecten la ejecución
                _logger.LogError(ex, "Error al registrar inicio de petición {RequestType}", requestName);
            }
            finally
            {
                DictionaryPool.Return(properties);
            }
        }

        /// <summary>
        /// Registra el éxito de una petición
        /// </summary>
        private void LogRequestSuccess(string requestName, string requestId, long executionTime, DateTime startTime, TResponse? response)
        {
            var properties = DictionaryPool.Rent();
            var context = DictionaryPool.Rent();
            try
            {
                // Pre-allocar capacidad
                properties.EnsureCapacity(EstimatedPropertiesCapacity);
                context.EnsureCapacity(EstimatedContextCapacity);

                properties["RequestId"] = requestId;
                properties["RequestType"] = requestName;
                properties["ExecutionTimeMs"] = executionTime;
                properties["Status"] = "Success";

                // OPTIMIZACIÓN: Serialización JSON condicional
                if (response != null)
                {
                    TrySerializeToProperty(properties, "ResponseData", response);
                }

                context["ExecutionTimeMs"] = executionTime;
                context["StartTime"] = startTime;
                context["EndTime"] = DateTime.UtcNow;

                // OPTIMIZACIÓN: Crear copias eficientes (el constructor de Dictionary es optimizado)
                var propertiesCopy = new Dictionary<string, object>(properties);
                var contextCopy = new Dictionary<string, object>(context);

                _loggingService.LogInformation(
                    $"Petición completada exitosamente: {requestName} (Tiempo: {executionTime}ms)",
                    "MediatR",
                    "Request",
                    properties: propertiesCopy,
                    context: contextCopy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar éxito de petición {RequestType}", requestName);
            }
            finally
            {
                DictionaryPool.Return(properties);
                DictionaryPool.Return(context);
            }
        }

        /// <summary>
        /// Registra un error en una petición
        /// </summary>
        private void LogRequestError(string requestName, string requestId, long executionTime, DateTime startTime, Exception exception)
        {
            var properties = DictionaryPool.Rent();
            var context = DictionaryPool.Rent();
            try
            {
                // Pre-allocar capacidad
                properties.EnsureCapacity(EstimatedPropertiesCapacity);
                context.EnsureCapacity(EstimatedContextCapacity);

                properties["RequestId"] = requestId;
                properties["RequestType"] = requestName;
                properties["ExecutionTimeMs"] = executionTime;
                properties["Status"] = "Error";

                context["ExecutionTimeMs"] = executionTime;
                context["StartTime"] = startTime;
                context["EndTime"] = DateTime.UtcNow;
                context["ExceptionType"] = exception.GetType().Name;

                // OPTIMIZACIÓN: Crear copias eficientes (el constructor de Dictionary es optimizado)
                var propertiesCopy = new Dictionary<string, object>(properties);
                var contextCopy = new Dictionary<string, object>(context);

                _loggingService.LogError(
                    $"Error al procesar petición: {requestName} - {exception.Message}",
                    "MediatR",
                    "Request",
                    properties: propertiesCopy,
                    context: contextCopy,
                    exception: exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar error de petición {RequestType}", requestName);
            }
            finally
            {
                DictionaryPool.Return(properties);
                DictionaryPool.Return(context);
            }
        }

        /// <summary>
        /// Intenta serializar un objeto a JSON y agregarlo a las propiedades
        /// OPTIMIZACIÓN: Método helper para eliminar código duplicado
        /// </summary>
        private static void TrySerializeToProperty<T>(Dictionary<string, object> properties, string propertyName, T? value)
        {
            if (value == null)
                return;

            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                properties[propertyName] = json;
            }
            catch
            {
                // Si no se puede serializar, no agregar (fallo silencioso)
                // Esto evita que errores de serialización afecten el logging
            }
        }
    }
}
