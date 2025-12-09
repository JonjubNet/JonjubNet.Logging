using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Common;
using System.Diagnostics;
using System.Text.Json;

namespace JonjubNet.Logging.Application.Behaviours
{
    /// <summary>
    /// Pipeline Behavior para registrar automáticamente todas las peticiones y respuestas de MediatR
    /// </summary>
    /// <typeparam name="TRequest">Tipo de la petición</typeparam>
    /// <typeparam name="TResponse">Tipo de la respuesta</typeparam>
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IStructuredLoggingService _loggingService;
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
        private readonly Stopwatch _stopwatch;
        
        // OPTIMIZACIÓN: Pool local de diccionarios para reducir allocations en hot path
        private static readonly ObjectPool<Dictionary<string, object>> _dictionaryPool =
            new DefaultObjectPool<Dictionary<string, object>>(
                new DefaultPooledObjectPolicy<Dictionary<string, object>>());
        
        // OPTIMIZACIÓN: Cache de JsonSerializerOptions para evitar allocations repetidas
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            MaxDepth = 3
        };

        public LoggingBehaviour(
            IStructuredLoggingService loggingService,
            ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stopwatch = new Stopwatch();
            
            // ✅ LOG DE DIAGNÓSTICO: Verificar que LoggingBehaviour se está instanciando
            _logger.LogWarning("✅✅✅✅✅ LoggingBehaviour INSTANCIADO para {RequestType} ✅✅✅✅✅", typeof(TRequest).Name);
            _logger.LogWarning("🔍 DIAGNÓSTICO: IStructuredLoggingService={ServiceType}, Logger={LoggerType}", 
                _loggingService?.GetType().FullName ?? "NULL", 
                _logger?.GetType().FullName ?? "NULL");
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // ✅ LOGGING DE DIAGNÓSTICO: Verificar que LoggingBehaviour se está ejecutando
            _logger.LogWarning("🔵🔵🔵🔵🔵 LoggingBehaviour Handle EJECUTÁNDOSE para: {RequestType} 🔵🔵🔵🔵🔵", typeof(TRequest).Name);
            _logger.LogWarning("🔍 DIAGNÓSTICO: Request={RequestType}, RequestData={RequestData}", 
                typeof(TRequest).FullName, 
                request?.ToString() ?? "NULL");
            
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            _stopwatch.Restart();

            // OPTIMIZACIÓN: Usar pool de diccionarios para reducir allocations
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

                // Crear nuevo diccionario para pasar al servicio (no devolver el del pool)
                var requestPropsCopy = new Dictionary<string, object>(requestProperties);
                
                // ✅ LOGGING DE DIAGNÓSTICO: Verificar que se llama a LogInformation
                _logger.LogInformation("🔵 LoggingBehaviour: Llamando a _loggingService.LogInformation para {RequestType}", requestName);
                
                try
                {
                    _loggingService.LogInformation(
                        $"Iniciando procesamiento de petición: {requestName}",
                        "MediatR",
                        "Request",
                        properties: requestPropsCopy);
                    _logger.LogInformation("✅ LoggingBehaviour: LogInformation completado para {RequestType}", requestName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ LoggingBehaviour: Error al llamar LogInformation para {RequestType}", requestName);
                    throw;
                }
            }
            finally
            {
                _dictionaryPool.Return(requestProperties);
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

                // OPTIMIZACIÓN: Usar pool de diccionarios para respuesta
                var responseProperties = _dictionaryPool.Get();
                var responseContext = _dictionaryPool.Get();
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

                    responseContext["ExecutionTimeMs"] = executionTime;
                    responseContext["StartTime"] = startTime;
                    responseContext["EndTime"] = endTime;

                    // Crear copias para pasar al servicio
                    var responsePropsCopy = new Dictionary<string, object>(responseProperties);
                    var responseContextCopy = new Dictionary<string, object>(responseContext);

                    _loggingService.LogInformation(
                        $"Petición completada exitosamente: {requestName} (Tiempo: {executionTime}ms)",
                        "MediatR",
                        "Request",
                        properties: responsePropsCopy,
                        context: responseContextCopy);
                }
                finally
                {
                    _dictionaryPool.Return(responseProperties);
                    _dictionaryPool.Return(responseContext);
                }

                return response;
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                var executionTime = _stopwatch.ElapsedMilliseconds;
                var endTime = DateTime.UtcNow;
                exception = ex;

                // OPTIMIZACIÓN: Usar pool de diccionarios para error
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
                    errorContext["EndTime"] = endTime;
                    errorContext["ExceptionType"] = ex.GetType().Name;

                    // Crear copias para pasar al servicio
                    var errorPropsCopy = new Dictionary<string, object>(errorProperties);
                    var errorContextCopy = new Dictionary<string, object>(errorContext);

                    _loggingService.LogError(
                        $"Error al procesar petición: {requestName} - {ex.Message}",
                        "MediatR",
                        "Request",
                        properties: errorPropsCopy,
                        context: errorContextCopy,
                        exception: ex);
                }
                finally
                {
                    _dictionaryPool.Return(errorProperties);
                    _dictionaryPool.Return(errorContext);
                }

                throw;
            }
        }
    }
}

