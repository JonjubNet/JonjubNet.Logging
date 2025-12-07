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
            var assembly = Assembly.GetExecutingAssembly();

            // Registrar casos de uso como Singleton
            // NOTA: Deben ser Singleton porque StructuredLoggingService (Singleton) los necesita
            // y AddSharedInfrastructure intenta resolverlos desde el root provider
            services.AddSingleton<UseCases.CreateLogEntryUseCase>();
            // EnrichLogEntryUseCase: ICurrentUserService ahora es Singleton (usa IHttpContextAccessor que es thread-safe)
            services.AddSingleton<UseCases.EnrichLogEntryUseCase>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var httpContextProvider = sp.GetService<IHttpContextProvider>();
                var currentUserService = sp.GetService<ICurrentUserService>();
                var scopeManager = sp.GetService<ILogScopeManager>();
                return new UseCases.EnrichLogEntryUseCase(
                    configManager, 
                    httpContextProvider, 
                    currentUserService,
                    scopeManager);
            });
            services.AddSingleton<UseCases.SendLogUseCase>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UseCases.SendLogUseCase>>();
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var sinks = sp.GetServices<ILogSink>();
                var kafkaProducer = sp.GetService<IKafkaProducer>();
                var logFilter = sp.GetService<ILogFilter>();
                var samplingService = sp.GetService<ILogSamplingService>();
                var sanitizationService = sp.GetService<IDataSanitizationService>();
                var circuitBreakerManager = sp.GetService<ICircuitBreakerManager>();
                var retryPolicyManager = sp.GetService<IRetryPolicyManager>();
                var deadLetterQueue = sp.GetService<IDeadLetterQueue>();

                return new UseCases.SendLogUseCase(
                    logger, configManager, sinks, kafkaProducer, logFilter,
                    samplingService, sanitizationService, circuitBreakerManager,
                    retryPolicyManager, deadLetterQueue);
            });

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

