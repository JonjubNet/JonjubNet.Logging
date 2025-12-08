using MediatR;
using Microsoft.Extensions.Logging;
using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics;
using System.Text.Json;

namespace JonjubNet.Logging.Application.Behaviours
{
    /// <summary>
    /// Pipeline Behavior para registrar automáticamente todas las peticiones y respuestas de MediatR
    /// Optimizado para performance: usa DictionaryPool y JsonSerializerOptions cacheado
    /// </summary>
    /// <typeparam name="TRequest">Tipo de la petición</typeparam>
    /// <typeparam name="TResponse">Tipo de la respuesta</typeparam>
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IStructuredLoggingService _loggingService;
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
        private readonly Stopwatch _stopwatch;
        
        // Cachear JsonSerializerOptions para evitar allocations repetidas
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            MaxDepth = 3,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        // Pool de diccionarios local para este comportamiento (Application no tiene acceso a Shared)
        private static readonly ObjectPool<Dictionary<string, object>> _dictionaryPool = 
            new DefaultObjectPool<Dictionary<string, object>>(
                new DefaultPooledObjectPolicy<Dictionary<string, object>>());

        public LoggingBehaviour(
            IStructuredLoggingService loggingService,
            ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        {
            _loggingService = loggingService;
            _logger = logger;
            _stopwatch = new Stopwatch();
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Optimización: cachear nombre del tipo para evitar reflection repetida
            var requestName = typeof(TRequest).Name;
            // Optimización: usar Guid.NewGuid().ToString("N") es más rápido que ToString() sin formato
            var requestId = Guid.NewGuid().ToString("N");
            var startTime = DateTime.UtcNow;

            _stopwatch.Restart();

            // Log de inicio de petición - usar pool local para reducir allocations
            var requestProperties = _dictionaryPool.Get();
            try
            {
                // Pre-allocar capacidad estimada (3 propiedades base + posible RequestData)
                if (requestProperties.Capacity < 4)
                {
                    requestProperties.EnsureCapacity(4);
                }

                requestProperties["RequestId"] = requestId;
                requestProperties["RequestType"] = requestName;
                requestProperties["RequestName"] = requestName;

                // Agregar propiedades del request si es posible serializarlo
                try
                {
                    var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                    requestProperties["RequestData"] = requestJson;
                }
                catch (Exception serializationEx)
                {
                    // Si no se puede serializar, no agregar (no es crítico para el logging)
                    // Log opcional solo si el logger está disponible y en modo debug
                    _logger.LogDebug(serializationEx, "No se pudo serializar el request {RequestType} para logging", requestName);
                }

                // Crear nuevo diccionario antes de pasar al servicio (no devolver el del pool)
                var propertiesCopy = new Dictionary<string, object>(requestProperties);
                try
                {
                    _loggingService.LogInformation(
                        $"Iniciando procesamiento de petición: {requestName}",
                        "MediatR",
                        "Request",
                        properties: propertiesCopy);
                }
                catch (Exception logEx)
                {
                    // Si el logging falla, no debe interrumpir el flujo de la aplicación
                    // Log en el logger estándar como fallback
                    _logger.LogWarning(logEx, "Error al registrar inicio de petición {RequestType}", requestName);
                }
            }
            finally
            {
                // Garantizar que el diccionario siempre se devuelva al pool, incluso si hay errores
                try
                {
                    requestProperties.Clear();
                    _dictionaryPool.Return(requestProperties);
                }
                catch (Exception poolEx)
                {
                    // Si falla el retorno al pool, loguear pero no lanzar excepción
                    // (evita ocultar excepciones originales)
                    _logger.LogError(poolEx, "Error crítico al devolver diccionario al pool en LoggingBehaviour");
                }
            }

            TResponse? response = default;
            Exception? exception = null;

            try
            {
                // Ejecutar el handler
                response = await next().ConfigureAwait(false);

                _stopwatch.Stop();
                var executionTime = _stopwatch.ElapsedMilliseconds;
                var endTime = DateTime.UtcNow;

                // Log de éxito - usar pool local para reducir allocations
                var responseProperties = _dictionaryPool.Get();
                var contextDict = _dictionaryPool.Get();
                try
                {
                    // Pre-allocar capacidad estimada
                    if (responseProperties.Capacity < 5)
                    {
                        responseProperties.EnsureCapacity(5);
                    }
                    if (contextDict.Capacity < 3)
                    {
                        contextDict.EnsureCapacity(3);
                    }

                    responseProperties["RequestId"] = requestId;
                    responseProperties["RequestType"] = requestName;
                    responseProperties["ExecutionTimeMs"] = executionTime;
                    responseProperties["Status"] = "Success";

                    // Agregar propiedades de la respuesta si es posible serializarla
                    try
                    {
                        if (response != null)
                        {
                            var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                            responseProperties["ResponseData"] = responseJson;
                        }
                    }
                    catch (Exception serializationEx)
                    {
                        // Si no se puede serializar, no agregar (no es crítico para el logging)
                        _logger.LogDebug(serializationEx, "No se pudo serializar la respuesta {RequestType} para logging", requestName);
                    }

                    contextDict["ExecutionTimeMs"] = executionTime;
                    contextDict["StartTime"] = startTime;
                    contextDict["EndTime"] = endTime;

                    // Crear nuevos diccionarios antes de pasar al servicio
                    var propertiesCopy = new Dictionary<string, object>(responseProperties);
                    var contextCopy = new Dictionary<string, object>(contextDict);

                    try
                    {
                        _loggingService.LogInformation(
                            $"Petición completada exitosamente: {requestName} (Tiempo: {executionTime}ms)",
                            "MediatR",
                            "Request",
                            properties: propertiesCopy,
                            context: contextCopy);
                    }
                    catch (Exception logEx)
                    {
                        // Si el logging falla, no debe interrumpir el flujo de la aplicación
                        _logger.LogWarning(logEx, "Error al registrar éxito de petición {RequestType}", requestName);
                    }
                }
                finally
                {
                    // Garantizar que los diccionarios siempre se devuelvan al pool
                    try
                    {
                        responseProperties.Clear();
                        _dictionaryPool.Return(responseProperties);
                    }
                    catch (Exception poolEx)
                    {
                        _logger.LogError(poolEx, "Error crítico al devolver responseProperties al pool");
                    }

                    try
                    {
                        contextDict.Clear();
                        _dictionaryPool.Return(contextDict);
                    }
                    catch (Exception poolEx)
                    {
                        _logger.LogError(poolEx, "Error crítico al devolver contextDict al pool");
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                var executionTime = _stopwatch.ElapsedMilliseconds;
                var endTime = DateTime.UtcNow;
                exception = ex;

                // Log de error - usar pool local para reducir allocations
                var errorProperties = _dictionaryPool.Get();
                var errorContext = _dictionaryPool.Get();
                try
                {
                    // Pre-allocar capacidad estimada
                    if (errorProperties.Capacity < 4)
                    {
                        errorProperties.EnsureCapacity(4);
                    }
                    if (errorContext.Capacity < 4)
                    {
                        errorContext.EnsureCapacity(4);
                    }

                    errorProperties["RequestId"] = requestId;
                    errorProperties["RequestType"] = requestName;
                    errorProperties["ExecutionTimeMs"] = executionTime;
                    errorProperties["Status"] = "Error";

                    errorContext["ExecutionTimeMs"] = executionTime;
                    errorContext["StartTime"] = startTime;
                    errorContext["EndTime"] = endTime;
                    errorContext["ExceptionType"] = ex.GetType().Name;

                    // Crear nuevos diccionarios antes de pasar al servicio
                    var propertiesCopy = new Dictionary<string, object>(errorProperties);
                    var contextCopy = new Dictionary<string, object>(errorContext);

                    try
                    {
                        _loggingService.LogError(
                            $"Error al procesar petición: {requestName} - {ex.Message}",
                            "MediatR",
                            "Request",
                            properties: propertiesCopy,
                            context: contextCopy,
                            exception: ex);
                    }
                    catch (Exception logEx)
                    {
                        // Si el logging estructurado falla, usar logger estándar como fallback
                        // Esto es crítico porque necesitamos registrar el error original
                        _logger.LogError(logEx, "Error al registrar error de petición {RequestType}. Error original: {OriginalError}", 
                            requestName, ex.Message);
                        // También loguear el error original en el logger estándar
                        _logger.LogError(ex, "Error original al procesar petición {RequestType}", requestName);
                    }
                }
                finally
                {
                    // Garantizar que los diccionarios siempre se devuelvan al pool
                    // Incluso si hay errores en el logging, debemos limpiar recursos
                    try
                    {
                        errorProperties.Clear();
                        _dictionaryPool.Return(errorProperties);
                    }
                    catch (Exception poolEx)
                    {
                        _logger.LogError(poolEx, "Error crítico al devolver errorProperties al pool");
                    }

                    try
                    {
                        errorContext.Clear();
                        _dictionaryPool.Return(errorContext);
                    }
                    catch (Exception poolEx)
                    {
                        _logger.LogError(poolEx, "Error crítico al devolver errorContext al pool");
                    }
                }

                // Siempre re-lanzar la excepción original para no ocultar errores
                throw;
            }
        }
    }
}

