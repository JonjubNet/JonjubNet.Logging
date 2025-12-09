using JonjubNet.Logging.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Logging.Application
{
    /// <summary>
    /// Extensiones para registrar los servicios de la capa de Application
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Registra todos los servicios de la capa de Application
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <returns>Colección de servicios para encadenamiento</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 🔍 LOGGING TEMPORAL DE DIAGNÓSTICO
            System.Console.WriteLine("[DIAGNÓSTICO] AddApplicationServices() llamado - Iniciando registro de UseCases...");

            var assembly = Assembly.GetExecutingAssembly();

            // Registrar casos de uso como Singleton
            // NOTA: Deben ser Singleton porque StructuredLoggingService (Singleton) los necesita
            // y AddSharedInfrastructure intenta resolverlos desde el root provider
            
            // CreateLogEntryUseCase - No tiene dependencias, se puede registrar directamente
            services.AddSingleton<UseCases.CreateLogEntryUseCase>();
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ CreateLogEntryUseCase registrado como Singleton");

            // EnrichLogEntryUseCase: ICurrentUserService ahora es Singleton (usa IHttpContextAccessor que es thread-safe)
            services.AddSingleton<UseCases.EnrichLogEntryUseCase>(sp =>
            {
                System.Console.WriteLine("[DIAGNÓSTICO] 🔵 Resolviendo dependencias para EnrichLogEntryUseCase...");
                try
                {
                    var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                    var httpContextProvider = sp.GetService<IHttpContextProvider>();
                    var currentUserService = sp.GetService<ICurrentUserService>();
                    var scopeManager = sp.GetService<ILogScopeManager>();
                    System.Console.WriteLine("[DIAGNÓSTICO] ✅ Dependencias resueltas para EnrichLogEntryUseCase");
                    return new UseCases.EnrichLogEntryUseCase(
                        configManager, 
                        httpContextProvider, 
                        currentUserService,
                        scopeManager);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[DIAGNÓSTICO] ❌ ERROR al resolver dependencias para EnrichLogEntryUseCase: {ex.Message}");
                    throw;
                }
            });
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ EnrichLogEntryUseCase registrado como Singleton (factory)");

            // SendLogUseCase - Tiene múltiples dependencias opcionales
            services.AddSingleton<UseCases.SendLogUseCase>(sp =>
            {
                System.Console.WriteLine("[DIAGNÓSTICO] 🔵 Resolviendo dependencias para SendLogUseCase...");
                try
                {
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UseCases.SendLogUseCase>>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ ILogger resuelto");
                    
                    var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ ILoggingConfigurationManager resuelto");
                    
                    var sinks = sp.GetServices<ILogSink>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ ILogSink resueltos: {sinks.Count()} sinks");
                    
                    var kafkaProducer = sp.GetService<IKafkaProducer>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ IKafkaProducer: {(kafkaProducer != null ? "disponible" : "null")}");
                    
                    var logFilter = sp.GetService<ILogFilter>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ ILogFilter: {(logFilter != null ? "disponible" : "null")}");
                    
                    var samplingService = sp.GetService<ILogSamplingService>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ ILogSamplingService: {(samplingService != null ? "disponible" : "null")}");
                    
                    var sanitizationService = sp.GetService<IDataSanitizationService>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ IDataSanitizationService: {(sanitizationService != null ? "disponible" : "null")}");
                    
                    var circuitBreakerManager = sp.GetService<ICircuitBreakerManager>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ ICircuitBreakerManager: {(circuitBreakerManager != null ? "disponible" : "null")}");
                    
                    var retryPolicyManager = sp.GetService<IRetryPolicyManager>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ IRetryPolicyManager: {(retryPolicyManager != null ? "disponible" : "null")}");
                    
                    var deadLetterQueue = sp.GetService<IDeadLetterQueue>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ IDeadLetterQueue: {(deadLetterQueue != null ? "disponible" : "null")}");

                    var sendLogUseCase = new UseCases.SendLogUseCase(
                        logger, configManager, sinks, kafkaProducer, logFilter,
                        samplingService, sanitizationService, circuitBreakerManager,
                        retryPolicyManager, deadLetterQueue);
                    
                    System.Console.WriteLine("[DIAGNÓSTICO] ✅ SendLogUseCase instanciado exitosamente");
                    return sendLogUseCase;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[DIAGNÓSTICO] ❌ ERROR al resolver dependencias para SendLogUseCase: {ex.Message}");
                    System.Console.WriteLine($"[DIAGNÓSTICO] StackTrace: {ex.StackTrace}");
                    throw;
                }
            });
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ SendLogUseCase registrado como Singleton (factory)");
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ AddApplicationServices() completado - Todos los UseCases registrados");

            // Registrar servicios de Application usando reflexión
            // Buscar todas las clases que implementan interfaces de Application
            var applicationInterfaces = assembly.GetTypes()
                .Where(t => t.IsInterface && t.Namespace?.StartsWith("JonjubNet.Logging.Application.Interfaces") == true)
                .ToList();

            // Nota: Las implementaciones concretas deben estar en Infrastructure
            // y registrarse en AddSharedInfrastructure
            
            return services;
        }
    }
}

