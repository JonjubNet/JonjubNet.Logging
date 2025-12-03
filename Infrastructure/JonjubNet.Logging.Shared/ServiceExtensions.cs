using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Shared.Services;
using JonjubNet.Logging.Shared.Services.Sinks;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            if (IsAspNetCoreAvailable())
            {
                try
                {
                    // Intentar registrar IHttpContextAccessor usando el método de extensión
                    // Esto requiere que Microsoft.AspNetCore.Http.Abstractions esté disponible
                    var httpContextAccessorExtensions = Type.GetType("Microsoft.Extensions.DependencyInjection.HttpServiceCollectionExtensions, Microsoft.AspNetCore.Http.Abstractions");
                    if (httpContextAccessorExtensions != null)
                    {
                        var addMethod = httpContextAccessorExtensions.GetMethod("AddHttpContextAccessor", 
                            BindingFlags.Public | BindingFlags.Static, 
                            null, 
                            new[] { typeof(IServiceCollection) }, 
                            null);
                        addMethod?.Invoke(null, new object[] { services });
                    }
                }
                catch
                {
                    // Si falla, continuar sin registrar IHttpContextAccessor
                }

                // Registrar IHttpContextProvider con implementación ASP.NET Core
                services.AddScoped<IHttpContextProvider, AspNetCoreHttpContextProvider>();
            }
            else
            {
                // Registrar IHttpContextProvider con implementación null (sin HTTP)
                services.AddScoped<IHttpContextProvider, NullHttpContextProvider>();
            }

            // Registrar ICurrentUserService
            services.AddScoped<ICurrentUserService, TUserService>();

            // Registrar IErrorCategorizationService
            services.AddScoped<IErrorCategorizationService, ErrorCategorizationService>();

            // Registrar LogScopeManager (singleton para mantener estado entre requests)
            services.AddSingleton<ILogScopeManager, LogScopeManager>();

            // Registrar ILogFilter
            services.AddScoped<ILogFilter, LogFilterService>();

            // Registrar ILogSamplingService
            services.AddSingleton<ILogSamplingService, LogSamplingService>();

            // Registrar IDataSanitizationService
            services.AddScoped<IDataSanitizationService, DataSanitizationService>();

            // Registrar servicios de resiliencia
            services.AddSingleton<ICircuitBreakerManager, CircuitBreakerManager>();
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
            // Registrar ConsoleLogSink (siempre disponible)
            services.AddScoped<ILogSink, ConsoleLogSink>();
            
            // Registrar SerilogSink (condicional - solo si Serilog está disponible)
            if (IsSerilogAvailable())
            {
                services.AddScoped<ILogSink, SerilogSink>();
            }

            // Registrar IStructuredLoggingService (inyectar LogQueue)
            services.AddSingleton<IStructuredLoggingService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<StructuredLoggingService>>();
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
                    logger, configManager, createUseCase, enrichUseCase, sendUseCase,
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
            if (IsAspNetCoreAvailable())
            {
                try
                {
                    // Intentar registrar IHttpContextAccessor usando el método de extensión
                    var httpContextAccessorExtensions = Type.GetType("Microsoft.Extensions.DependencyInjection.HttpServiceCollectionExtensions, Microsoft.AspNetCore.Http.Abstractions");
                    if (httpContextAccessorExtensions != null)
                    {
                        var addMethod = httpContextAccessorExtensions.GetMethod("AddHttpContextAccessor", 
                            BindingFlags.Public | BindingFlags.Static, 
                            null, 
                            new[] { typeof(IServiceCollection) }, 
                            null);
                        addMethod?.Invoke(null, new object[] { services });
                    }
                }
                catch
                {
                    // Si falla, continuar sin registrar IHttpContextAccessor
                }

                // Registrar IHttpContextProvider con implementación ASP.NET Core
                services.AddScoped<IHttpContextProvider, AspNetCoreHttpContextProvider>();
            }
            else
            {
                // Registrar IHttpContextProvider con implementación null (sin HTTP)
                services.AddScoped<IHttpContextProvider, NullHttpContextProvider>();
            }

            // Registrar ICurrentUserService
            services.AddScoped<ICurrentUserService, TUserService>();

            // Registrar IErrorCategorizationService
            services.AddScoped<IErrorCategorizationService, ErrorCategorizationService>();

            // Registrar LogScopeManager (singleton para mantener estado entre requests)
            services.AddSingleton<ILogScopeManager, LogScopeManager>();

            // Registrar ILogFilter
            services.AddScoped<ILogFilter, LogFilterService>();

            // Registrar ILogSamplingService
            services.AddSingleton<ILogSamplingService, LogSamplingService>();

            // Registrar IDataSanitizationService
            services.AddScoped<IDataSanitizationService, DataSanitizationService>();

            // Registrar servicios de resiliencia
            services.AddSingleton<ICircuitBreakerManager, CircuitBreakerManager>();
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
            // Registrar ConsoleLogSink (siempre disponible)
            services.AddScoped<ILogSink, ConsoleLogSink>();
            
            // Registrar SerilogSink (condicional - solo si Serilog está disponible)
            if (IsSerilogAvailable())
            {
                services.AddScoped<ILogSink, SerilogSink>();
            }

            // Registrar IStructuredLoggingService (inyectar LogQueue)
            services.AddSingleton<IStructuredLoggingService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<StructuredLoggingService>>();
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
                    logger, configManager, createUseCase, enrichUseCase, sendUseCase,
                    sinks, scopeManager, kafkaProducer, logQueue, priorityQueue);
            });

            return services;
        }

        /// <summary>
        /// Verifica si ASP.NET Core está disponible en el entorno actual
        /// </summary>
        private static bool IsAspNetCoreAvailable()
        {
            try
            {
                // Intentar cargar el tipo IHttpContextAccessor
                var httpContextAccessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions");
                if (httpContextAccessorType == null)
                {
                    // Intentar con el assembly completo
                    var assembly = Assembly.Load("Microsoft.AspNetCore.Http.Abstractions");
                    httpContextAccessorType = assembly.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor");
                }
                return httpContextAccessorType != null;
            }
            catch
            {
                return false;
            }
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

