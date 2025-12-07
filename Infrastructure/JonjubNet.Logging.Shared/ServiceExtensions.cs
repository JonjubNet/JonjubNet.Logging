using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Shared.Services;
using JonjubNet.Logging.Shared.Services.Sinks;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace JonjubNet.Logging.Shared
{
    /// <summary>
    /// Extensiones para registrar los servicios de la capa de Infrastructure (Shared)
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Registra todos los servicios de infraestructura compartida para logging estructurado
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para encadenamiento</returns>
        public static IServiceCollection AddSharedInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return AddSharedInfrastructure<DefaultCurrentUserService>(services, configuration);
        }

        /// <summary>
        /// Registra todos los servicios de infraestructura compartida con un servicio de usuario personalizado
        /// </summary>
        /// <typeparam name="TUserService">Tipo del servicio de usuario personalizado</typeparam>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para encadenamiento</returns>
        public static IServiceCollection AddSharedInfrastructure<TUserService>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TUserService : class, ICurrentUserService
        {
            // Registrar configuración con IOptionsMonitor para Hot-Reload
            services.Configure<LoggingConfiguration>(
                configuration.GetSection(LoggingConfiguration.SectionName));
            
            // Registrar ILoggingConfigurationManager para gestión dinámica
            services.AddSingleton<ILoggingConfigurationManager, LoggingConfigurationManager>();

            // Registrar validadores de FluentValidation
            // Nota: Los validadores están en el namespace Shared.Configuration
            // services.AddValidatorsFromAssemblyContaining<LoggingConfigurationValidator>();

            // Registrar IHttpContextAccessor e IHttpContextProvider (condicional - solo si ASP.NET Core está disponible)
            // Usar conditional compilation en lugar de reflection para AOT-friendly
            RegisterHttpContextServices(services);

            // Registrar ICurrentUserService
            services.AddScoped<ICurrentUserService, TUserService>();

            // Registrar IErrorCategorizationService
            services.AddScoped<IErrorCategorizationService, ErrorCategorizationService>();

            // Registrar LogScopeManager (singleton para mantener estado entre requests)
            services.AddSingleton<ILogScopeManager, LogScopeManager>();

            // Registrar ILogFilter
            services.AddScoped<ILogFilter, LogFilterService>();

            // Registrar ILogSamplingService (singleton con TimeProvider)
            services.AddSingleton<ILogSamplingService>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
                return new LogSamplingService(configManager, timeProvider);
            });

            // Registrar IDataSanitizationService
            services.AddScoped<IDataSanitizationService, DataSanitizationService>();

            // Registrar servicios de resiliencia
            services.AddSingleton<ICircuitBreakerManager>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var logger = sp.GetService<ILogger<CircuitBreakerService>>();
                return new CircuitBreakerManager(configManager, logger, sp);
            });
            services.AddSingleton<IRetryPolicyManager, RetryPolicyManager>();
            services.AddSingleton<IDeadLetterQueue, DeadLetterQueueService>();

            // Registrar servicios de batching inteligente
            services.AddSingleton<IIntelligentBatchingService, IntelligentBatchingService>();
            services.AddSingleton<IBatchCompressionService, BatchCompressionService>();

            // Registrar PriorityLogQueue si está habilitado
            services.AddSingleton<IPriorityLogQueue>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var logger = sp.GetService<ILogger<PriorityLogQueue>>();
                return new PriorityLogQueue(configManager, logger);
            });

            // Registrar LogQueue (singleton para compartir entre instancias)
            services.AddSingleton<LogQueue>();
            services.AddSingleton<ILogQueue>(sp => sp.GetRequiredService<LogQueue>());

            // Registrar procesador de logs (condicional - solo si hay un IHost disponible)
            if (IsHostedServiceAvailable())
            {
                // Registrar IntelligentLogProcessor con todas sus dependencias
                services.AddHostedService<IntelligentLogProcessor>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<IntelligentLogProcessor>>();
                    var sendUseCase = sp.GetRequiredService<SendLogUseCase>();
                    var enrichUseCase = sp.GetRequiredService<EnrichLogEntryUseCase>();
                    var batchingService = sp.GetRequiredService<IIntelligentBatchingService>();
                    var compressionService = sp.GetRequiredService<IBatchCompressionService>();
                    var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                    var priorityQueue = sp.GetService<IPriorityLogQueue>();
                    var standardQueue = sp.GetService<LogQueue>();
                    return new IntelligentLogProcessor(
                        logger, sendUseCase, enrichUseCase, batchingService, compressionService,
                        configManager, priorityQueue, standardQueue);
                });
            }

            // Registrar Health Check
            services.AddSingleton<ILoggingHealthCheck, LoggingHealthCheck>();

            // Registrar ILogSink implementations
            // IMPORTANTE: Cambiar a Singleton para evitar problemas de ciclo de vida
            // cuando se usa desde IStructuredLoggingService (Singleton)
            services.AddSingleton<ILogSink, ConsoleLogSink>();
            
            // Registrar SerilogSink (condicional - solo si Serilog está disponible)
            if (IsSerilogAvailable())
            {
                services.AddSingleton<ILogSink, SerilogSink>();
            }

            // Registrar IStructuredLoggingService (inyectar LogQueue)
            // Usar ILoggerFactory en lugar de ILogger<T> para Singletons
            services.AddSingleton<IStructuredLoggingService>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var createUseCase = sp.GetRequiredService<CreateLogEntryUseCase>();
                var enrichUseCase = sp.GetRequiredService<EnrichLogEntryUseCase>();
                var sendUseCase = sp.GetRequiredService<SendLogUseCase>();
                var sinks = sp.GetServices<ILogSink>();
                var scopeManager = sp.GetRequiredService<ILogScopeManager>();
                var kafkaProducer = sp.GetService<IKafkaProducer>();
                var logQueue = sp.GetService<ILogQueue>();
                var priorityQueue = sp.GetService<IPriorityLogQueue>();

                return new StructuredLoggingService(
                    loggerFactory, configManager, createUseCase, enrichUseCase, sendUseCase,
                    sinks, scopeManager, kafkaProducer, logQueue, priorityQueue);
            });

            return services;
        }

        /// <summary>
        /// Registra todos los servicios de infraestructura compartida SIN BackgroundService
        /// Usa procesamiento síncrono alternativo para aplicaciones sin host
        /// </summary>
        /// <typeparam name="TUserService">Tipo del servicio de usuario personalizado</typeparam>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para encadenamiento</returns>
        public static IServiceCollection AddSharedInfrastructureWithoutHost<TUserService>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TUserService : class, ICurrentUserService
        {
            // Registrar configuración con IOptionsMonitor para Hot-Reload
            services.Configure<LoggingConfiguration>(
                configuration.GetSection(LoggingConfiguration.SectionName));
            
            // Registrar ILoggingConfigurationManager para gestión dinámica
            services.AddSingleton<ILoggingConfigurationManager, LoggingConfigurationManager>();

            // Registrar IHttpContextAccessor e IHttpContextProvider (condicional - solo si ASP.NET Core está disponible)
            // Usar conditional compilation en lugar de reflection para AOT-friendly
            RegisterHttpContextServices(services);

            // Registrar ICurrentUserService
            services.AddScoped<ICurrentUserService, TUserService>();

            // Registrar IErrorCategorizationService
            services.AddScoped<IErrorCategorizationService, ErrorCategorizationService>();

            // Registrar LogScopeManager (singleton para mantener estado entre requests)
            services.AddSingleton<ILogScopeManager, LogScopeManager>();

            // Registrar ILogFilter
            services.AddScoped<ILogFilter, LogFilterService>();

            // Registrar ILogSamplingService (singleton con TimeProvider)
            services.AddSingleton<ILogSamplingService>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
                return new LogSamplingService(configManager, timeProvider);
            });

            // Registrar IDataSanitizationService
            services.AddScoped<IDataSanitizationService, DataSanitizationService>();

            // Registrar servicios de resiliencia
            services.AddSingleton<ICircuitBreakerManager>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var logger = sp.GetService<ILogger<CircuitBreakerService>>();
                return new CircuitBreakerManager(configManager, logger, sp);
            });
            services.AddSingleton<IRetryPolicyManager, RetryPolicyManager>();
            services.AddSingleton<IDeadLetterQueue, DeadLetterQueueService>();

            // Registrar LogQueue (singleton para compartir entre instancias)
            services.AddSingleton<LogQueue>();
            services.AddSingleton<ILogQueue>(sp => sp.GetRequiredService<LogQueue>());

            // Registrar SynchronousLogProcessor en lugar de BackgroundService
            services.AddSingleton<SynchronousLogProcessor>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SynchronousLogProcessor>>();
                var logQueue = sp.GetRequiredService<LogQueue>();
                var sendLogUseCase = sp.GetRequiredService<SendLogUseCase>();
                var enrichLogUseCase = sp.GetRequiredService<EnrichLogEntryUseCase>();
                return new SynchronousLogProcessor(logger, logQueue, sendLogUseCase, enrichLogUseCase);
            });

            // Registrar Health Check
            services.AddSingleton<ILoggingHealthCheck, LoggingHealthCheck>();

            // Registrar ILogSink implementations
            // IMPORTANTE: Cambiar a Singleton para evitar problemas de ciclo de vida
            // cuando se usa desde IStructuredLoggingService (Singleton)
            services.AddSingleton<ILogSink, ConsoleLogSink>();
            
            // Registrar SerilogSink (condicional - solo si Serilog está disponible)
            if (IsSerilogAvailable())
            {
                services.AddSingleton<ILogSink, SerilogSink>();
            }

            // Registrar IStructuredLoggingService (inyectar LogQueue)
            // Usar ILoggerFactory en lugar de ILogger<T> para Singletons
            services.AddSingleton<IStructuredLoggingService>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var createUseCase = sp.GetRequiredService<CreateLogEntryUseCase>();
                var enrichUseCase = sp.GetRequiredService<EnrichLogEntryUseCase>();
                var sendUseCase = sp.GetRequiredService<SendLogUseCase>();
                var sinks = sp.GetServices<ILogSink>();
                var scopeManager = sp.GetRequiredService<ILogScopeManager>();
                var kafkaProducer = sp.GetService<IKafkaProducer>();
                var logQueue = sp.GetService<ILogQueue>();
                var priorityQueue = sp.GetService<IPriorityLogQueue>();

                return new StructuredLoggingService(
                    loggerFactory, configManager, createUseCase, enrichUseCase, sendUseCase,
                    sinks, scopeManager, kafkaProducer, logQueue, priorityQueue);
            });

            return services;
        }

        /// <summary>
        /// Registra los servicios de HTTP context de forma condicional usando conditional compilation.
        /// Elimina la necesidad de reflection, haciéndolo AOT-friendly.
        /// </summary>
        private static void RegisterHttpContextServices(IServiceCollection services)
        {
#if ASPNETCORE
            // ASP.NET Core está disponible - usar implementación real
            services.AddHttpContextAccessor();
            services.AddScoped<IHttpContextProvider, AspNetCoreHttpContextProvider>();
#else
            // ASP.NET Core no está disponible - usar implementación null
            services.AddScoped<IHttpContextProvider, NullHttpContextProvider>();
#endif
        }


        /// <summary>
        /// Verifica si IHostedService está disponible (hay un host en la aplicación)
        /// </summary>
        private static bool IsHostedServiceAvailable()
        {
            try
            {
                // Verificar si IHostedService está disponible
                var hostedServiceType = Type.GetType("Microsoft.Extensions.Hosting.IHostedService, Microsoft.Extensions.Hosting.Abstractions");
                if (hostedServiceType == null)
                {
                    var assembly = Assembly.Load("Microsoft.Extensions.Hosting.Abstractions");
                    hostedServiceType = assembly?.GetType("Microsoft.Extensions.Hosting.IHostedService");
                }
                return hostedServiceType != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si Serilog está disponible en el entorno actual
        /// </summary>
        private static bool IsSerilogAvailable()
        {
            try
            {
                // Verificar si Serilog está disponible
                var serilogLogType = Type.GetType("Serilog.Log, Serilog");
                if (serilogLogType == null)
                {
                    var assembly = Assembly.Load("Serilog");
                    serilogLogType = assembly?.GetType("Serilog.Log");
                }
                return serilogLogType != null;
            }
            catch
            {
                return false;
            }
        }
    }
}

