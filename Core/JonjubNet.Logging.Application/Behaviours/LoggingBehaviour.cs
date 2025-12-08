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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            _stopwatch.Restart();

            // Log de inicio de petición - usar pool local para reducir allocations
            var requestProperties = _dictionaryPool.Get();
            try
            {
                requestProperties["RequestId"] = requestId;
                requestProperties["RequestType"] = requestName;
                requestProperties["RequestName"] = requestName;

                // Agregar propiedades del request si es posible serializarlo
                try
                {
                    var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
                    requestProperties["RequestData"] = requestJson;
                }
                catch
                {
                    // Si no se puede serializar, no agregar
                }

                // Crear nuevo diccionario antes de pasar al servicio (no devolver el del pool)
                var propertiesCopy = new Dictionary<string, object>(requestProperties);
                _loggingService.LogInformation(
                    $"Iniciando procesamiento de petición: {requestName}",
                    "MediatR",
                    "Request",
                    properties: propertiesCopy);
            }
            finally
            {
                requestProperties.Clear();
                _dictionaryPool.Return(requestProperties);
            }

            TResponse? response = default;
            Exception? exception = null;

            try
            {
                // Ejecutar el handler
                response = await next();

                _stopwatch.Stop();
                var executionTime = _stopwatch.ElapsedMilliseconds;

                // Log de éxito - usar pool local para reducir allocations
                var responseProperties = _dictionaryPool.Get();
                var contextDict = _dictionaryPool.Get();
                try
                {
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
                    catch
                    {
                        // Si no se puede serializar, no agregar
                    }

                    contextDict["ExecutionTimeMs"] = executionTime;
                    contextDict["StartTime"] = startTime;
                    contextDict["EndTime"] = DateTime.UtcNow;

                    // Crear nuevos diccionarios antes de pasar al servicio
                    var propertiesCopy = new Dictionary<string, object>(responseProperties);
                    var contextCopy = new Dictionary<string, object>(contextDict);

                    _loggingService.LogInformation(
                        $"Petición completada exitosamente: {requestName} (Tiempo: {executionTime}ms)",
                        "MediatR",
                        "Request",
                        properties: propertiesCopy,
                        context: contextCopy);
                }
                finally
                {
                    responseProperties.Clear();
                    contextDict.Clear();
                    _dictionaryPool.Return(responseProperties);
                    _dictionaryPool.Return(contextDict);
                }

                return response;
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                var executionTime = _stopwatch.ElapsedMilliseconds;
                exception = ex;

                // Log de error - usar pool local para reducir allocations
                var errorProperties = _dictionaryPool.Get();
                var errorContext = _dictionaryPool.Get();
                try
                {
                    errorProperties["RequestId"] = requestId;
                    errorProperties["RequestType"] = requestName;
                    errorProperties["ExecutionTimeMs"] = executionTime;
                    errorProperties["Status"] = "Error";

                    errorContext["ExecutionTimeMs"] = executionTime;
                    errorContext["StartTime"] = startTime;
                    errorContext["EndTime"] = DateTime.UtcNow;
                    errorContext["ExceptionType"] = ex.GetType().Name;

                    // Crear nuevos diccionarios antes de pasar al servicio
                    var propertiesCopy = new Dictionary<string, object>(errorProperties);
                    var contextCopy = new Dictionary<string, object>(errorContext);

                    _loggingService.LogError(
                        $"Error al procesar petición: {requestName} - {ex.Message}",
                        "MediatR",
                        "Request",
                        properties: propertiesCopy,
                        context: contextCopy,
                        exception: ex);
                }
                finally
                {
                    errorProperties.Clear();
                    errorContext.Clear();
                    _dictionaryPool.Return(errorProperties);
                    _dictionaryPool.Return(errorContext);
                }

                throw;
            }
        }
    }
}

