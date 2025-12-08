using MediatR;
using Microsoft.Extensions.Logging;
using JonjubNet.Logging.Application.Interfaces;
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

        public LoggingBehaviour(
            IStructuredLoggingService loggingService,
            ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        {
            _loggingService = loggingService;
            _logger = logger;
            _stopwatch = new Stopwatch();
            
            // ✅ LOG DE DIAGNÓSTICO: Verificar que LoggingBehaviour se está instanciando
            _logger.LogInformation("✅✅✅ LoggingBehaviour INSTANCIADO para {RequestType} ✅✅✅", typeof(TRequest).Name);
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // ✅ LOGGING DE DIAGNÓSTICO: Verificar que LoggingBehaviour se está ejecutando
            _logger.LogInformation("🔵 LoggingBehaviour ejecutándose para: {RequestType}", typeof(TRequest).Name);
            
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;

            _stopwatch.Restart();

            // Log de inicio de petición
            var requestProperties = new Dictionary<string, object>
            {
                { "RequestId", requestId },
                { "RequestType", requestName },
                { "RequestName", requestName }
            };

            // Agregar propiedades del request si es posible serializarlo
            try
            {
                var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    MaxDepth = 3
                });
                requestProperties["RequestData"] = requestJson;
            }
            catch
            {
                // Si no se puede serializar, no agregar
            }

            // ✅ LOGGING DE DIAGNÓSTICO: Verificar que se llama a LogInformation
            _logger.LogInformation("🔵 LoggingBehaviour: Llamando a _loggingService.LogInformation para {RequestType}", requestName);
            
            try
            {
                _loggingService.LogInformation(
                    $"Iniciando procesamiento de petición: {requestName}",
                    "MediatR",
                    "Request",
                    properties: requestProperties);
                _logger.LogInformation("✅ LoggingBehaviour: LogInformation completado para {RequestType}", requestName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ LoggingBehaviour: Error al llamar LogInformation para {RequestType}", requestName);
                throw;
            }

            TResponse? response = default;
            Exception? exception = null;

            try
            {
                // Ejecutar el handler
                response = await next();

                _stopwatch.Stop();
                var executionTime = _stopwatch.ElapsedMilliseconds;

                // Log de éxito
                var responseProperties = new Dictionary<string, object>
                {
                    { "RequestId", requestId },
                    { "RequestType", requestName },
                    { "ExecutionTimeMs", executionTime },
                    { "Status", "Success" }
                };

                // Agregar propiedades de la respuesta si es posible serializarla
                try
                {
                    if (response != null)
                    {
                        var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            MaxDepth = 3
                        });
                        responseProperties["ResponseData"] = responseJson;
                    }
                }
                catch
                {
                    // Si no se puede serializar, no agregar
                }

                _loggingService.LogInformation(
                    $"Petición completada exitosamente: {requestName} (Tiempo: {executionTime}ms)",
                    "MediatR",
                    "Request",
                    properties: responseProperties,
                    context: new Dictionary<string, object>
                    {
                        { "ExecutionTimeMs", executionTime },
                        { "StartTime", startTime },
                        { "EndTime", DateTime.UtcNow }
                    });

                return response;
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                var executionTime = _stopwatch.ElapsedMilliseconds;
                exception = ex;

                // Log de error
                var errorProperties = new Dictionary<string, object>
                {
                    { "RequestId", requestId },
                    { "RequestType", requestName },
                    { "ExecutionTimeMs", executionTime },
                    { "Status", "Error" }
                };

                _loggingService.LogError(
                    $"Error al procesar petición: {requestName} - {ex.Message}",
                    "MediatR",
                    "Request",
                    properties: errorProperties,
                    context: new Dictionary<string, object>
                    {
                        { "ExecutionTimeMs", executionTime },
                        { "StartTime", startTime },
                        { "EndTime", DateTime.UtcNow },
                        { "ExceptionType", ex.GetType().Name }
                    },
                    exception: ex);

                throw;
            }
        }
    }
}

