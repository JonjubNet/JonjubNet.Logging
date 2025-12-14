using JonjubNet.Logging.Application;
using JonjubNet.Logging.Application.Configuration;
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Application.UseCases;
using JonjubNet.Logging.Shared.Services;
using JonjubNet.Logging.Shared.Services.Sinks;
using FluentValidation;
using MediatR;
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
            System.Console.WriteLine("[DIAGNÓSTICO] AddSharedInfrastructure() iniciado");
            
            // ✅ PASO 1: Registrar configuración PRIMERO (necesaria para ILoggingConfigurationManager)
            // Registrar configuración con IOptionsMonitor para Hot-Reload
            services.Configure<LoggingConfiguration>(
                configuration.GetSection(LoggingConfiguration.SectionName));
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ Configuración de LoggingConfiguration registrada");
            
            // ✅ PASO 2: Registrar ILoggingConfigurationManager (necesario para UseCases)
            // Registrar ILoggingConfigurationManager para gestión dinámica
            services.AddSingleton<ILoggingConfigurationManager, LoggingConfigurationManager>();
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ ILoggingConfigurationManager registrado");
            
            // ✅ PASO 3: Registrar servicios de Application (UseCases)
            // Ahora que ILoggingConfigurationManager está registrado, los UseCases pueden resolverlo
            System.Console.WriteLine("[DIAGNÓSTICO] Llamando AddApplicationServices()...");
            services.AddApplicationServices();
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ AddApplicationServices() completado");

            // Registrar validadores de FluentValidation
            // Nota: Los validadores están en el namespace Shared.Configuration
            // services.AddValidatorsFromAssemblyContaining<LoggingConfigurationValidator>();

            // Registrar IHttpContextAccessor e IHttpContextProvider (condicional - solo si ASP.NET Core está disponible)
            // Usar conditional compilation en lugar de reflection para AOT-friendly
            // NOTA: IHttpContextProvider debe ser Singleton porque EnrichLogEntryUseCase (Singleton) lo necesita
            RegisterHttpContextServices(services);

            // Registrar ICurrentUserService como Singleton
            // NOTA: Debe ser Singleton porque EnrichLogEntryUseCase (Singleton) lo necesita
            // IHttpContextAccessor es thread-safe y Singleton, por lo que ICurrentUserService puede ser Singleton también
            services.AddSingleton<ICurrentUserService, TUserService>();

            // Registrar IErrorCategorizationService como Singleton
            // NOTA: Debe ser Singleton porque puede ser usado desde servicios Singleton
            services.AddSingleton<IErrorCategorizationService, ErrorCategorizationService>();

            // Registrar LogScopeManager (singleton para mantener estado entre requests)
            services.AddSingleton<ILogScopeManager, LogScopeManager>();

            // Registrar ILogFilter como Singleton
            // NOTA: Debe ser Singleton porque SendLogUseCase (Singleton) lo necesita
            services.AddSingleton<ILogFilter, LogFilterService>();

            // Registrar ILogSamplingService (singleton con TimeProvider)
            services.AddSingleton<ILogSamplingService>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
                return new LogSamplingService(configManager, timeProvider);
            });

            // Registrar IDataSanitizationService como Singleton
            // NOTA: Debe ser Singleton porque SendLogUseCase (Singleton) lo necesita
            services.AddSingleton<IDataSanitizationService, DataSanitizationService>();

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
                // SendLogUseCase es Scoped, por lo que usamos IServiceScopeFactory
                services.AddHostedService<IntelligentLogProcessor>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<IntelligentLogProcessor>>();
                    var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                    var enrichUseCase = sp.GetRequiredService<EnrichLogEntryUseCase>();
                    var batchingService = sp.GetRequiredService<IIntelligentBatchingService>();
                    var compressionService = sp.GetRequiredService<IBatchCompressionService>();
                    var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                    var priorityQueue = sp.GetService<IPriorityLogQueue>();
                    var standardQueue = sp.GetService<LogQueue>();
                    return new IntelligentLogProcessor(
                        logger, serviceScopeFactory, enrichUseCase, batchingService, compressionService,
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
            // Los UseCases ahora son Singleton, se pueden resolver directamente desde root provider
            services.AddSingleton<IStructuredLoggingService>(sp =>
            {
                System.Console.WriteLine("[DIAGNÓSTICO] 🔵 Registrando IStructuredLoggingService - Resolviendo dependencias...");
                try
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ ILoggerFactory resuelto");
                    
                    var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ ILoggingConfigurationManager resuelto");
                    
                    System.Console.WriteLine("[DIAGNÓSTICO]   🔵 Intentando resolver CreateLogEntryUseCase...");
                    var createUseCase = sp.GetService<CreateLogEntryUseCase>();
                    if (createUseCase == null)
                    {
                        throw new InvalidOperationException(
                            "CreateLogEntryUseCase no está registrado. Asegúrate de que AddApplicationServices() se haya llamado antes de AddSharedInfrastructure.");
                    }
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ CreateLogEntryUseCase resuelto");
                    
                    System.Console.WriteLine("[DIAGNÓSTICO]   🔵 Intentando resolver EnrichLogEntryUseCase...");
                    var enrichUseCase = sp.GetService<EnrichLogEntryUseCase>();
                    if (enrichUseCase == null)
                    {
                        throw new InvalidOperationException(
                            "EnrichLogEntryUseCase no está registrado. Asegúrate de que AddApplicationServices() se haya llamado antes de AddSharedInfrastructure.");
                    }
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ EnrichLogEntryUseCase resuelto");
                    
                    // SendLogUseCase es Scoped, no se puede resolver desde root provider
                    // Usar IServiceScopeFactory en su lugar
                    System.Console.WriteLine("[DIAGNÓSTICO]   🔵 Obteniendo IServiceScopeFactory (SendLogUseCase es Scoped)...");
                    var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ IServiceScopeFactory resuelto");
                    
                    var sinks = sp.GetServices<ILogSink>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ ILogSink resueltos: {sinks.Count()} sinks");
                    
                    var scopeManager = sp.GetRequiredService<ILogScopeManager>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ ILogScopeManager resuelto");
                    
                    var kafkaProducer = sp.GetService<IKafkaProducer>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ IKafkaProducer: {(kafkaProducer != null ? "disponible" : "null")}");
                    
                    var logQueue = sp.GetService<ILogQueue>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ ILogQueue: {(logQueue != null ? "disponible" : "null")}");
                    
                    var priorityQueue = sp.GetService<IPriorityLogQueue>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ IPriorityLogQueue: {(priorityQueue != null ? "disponible" : "null")}");

                    var structuredLoggingService = new StructuredLoggingService(
                        loggerFactory, configManager, createUseCase, enrichUseCase, serviceScopeFactory,
                        sinks, scopeManager, kafkaProducer, logQueue, priorityQueue);
                    
                    System.Console.WriteLine("[DIAGNÓSTICO] ✅ IStructuredLoggingService instanciado exitosamente");
                    return structuredLoggingService;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[DIAGNÓSTICO] ❌ ERROR al registrar IStructuredLoggingService: {ex.Message}");
                    System.Console.WriteLine($"[DIAGNÓSTICO] StackTrace: {ex.StackTrace}");
                    throw;
                }
            });

            // Registrar IAuditLoggingService después de IStructuredLoggingService (lo necesita)
            services.AddSingleton<IAuditLoggingService>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var loggingService = sp.GetRequiredService<IStructuredLoggingService>();
                var logger = sp.GetService<ILogger<AuditLoggingService>>();
                return new AuditLoggingService(configManager, loggingService, logger);
            });

            // Actualizar LoggingConfigurationManager para usar IAuditLoggingService
            // Necesitamos re-registrarlo con la dependencia de IAuditLoggingService
            var existingConfigManager = services.FirstOrDefault(s => s.ServiceType == typeof(ILoggingConfigurationManager));
            if (existingConfigManager != null)
            {
                services.Remove(existingConfigManager);
            }
            services.AddSingleton<ILoggingConfigurationManager>(sp =>
            {
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<LoggingConfiguration>>();
                var logger = sp.GetRequiredService<ILogger<LoggingConfigurationManager>>();
                var auditService = sp.GetService<IAuditLoggingService>();
                return new LoggingConfigurationManager(optionsMonitor, logger, auditService);
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
            System.Console.WriteLine("[DIAGNÓSTICO] AddSharedInfrastructureWithoutHost() iniciado");
            
            // ✅ PASO 1: Registrar configuración PRIMERO (necesaria para ILoggingConfigurationManager)
            // Registrar configuración con IOptionsMonitor para Hot-Reload
            services.Configure<LoggingConfiguration>(
                configuration.GetSection(LoggingConfiguration.SectionName));
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ Configuración de LoggingConfiguration registrada");
            
            // ✅ PASO 2: Registrar ILoggingConfigurationManager (necesario para UseCases)
            // Registrar ILoggingConfigurationManager para gestión dinámica
            services.AddSingleton<ILoggingConfigurationManager, LoggingConfigurationManager>();
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ ILoggingConfigurationManager registrado");
            
            // ✅ PASO 3: Registrar servicios de Application (UseCases)
            // Ahora que ILoggingConfigurationManager está registrado, los UseCases pueden resolverlo
            System.Console.WriteLine("[DIAGNÓSTICO] Llamando AddApplicationServices()...");
            services.AddApplicationServices();
            System.Console.WriteLine("[DIAGNÓSTICO] ✅ AddApplicationServices() completado");

            // Registrar IHttpContextAccessor e IHttpContextProvider (condicional - solo si ASP.NET Core está disponible)
            // Usar conditional compilation en lugar de reflection para AOT-friendly
            // NOTA: IHttpContextProvider debe ser Singleton porque EnrichLogEntryUseCase (Singleton) lo necesita
            RegisterHttpContextServices(services);

            // Registrar ICurrentUserService como Singleton
            // NOTA: Debe ser Singleton porque EnrichLogEntryUseCase (Singleton) lo necesita
            // IHttpContextAccessor es thread-safe y Singleton, por lo que ICurrentUserService puede ser Singleton también
            services.AddSingleton<ICurrentUserService, TUserService>();

            // Registrar IErrorCategorizationService como Singleton
            // NOTA: Debe ser Singleton porque puede ser usado desde servicios Singleton
            services.AddSingleton<IErrorCategorizationService, ErrorCategorizationService>();

            // Registrar LogScopeManager (singleton para mantener estado entre requests)
            services.AddSingleton<ILogScopeManager, LogScopeManager>();

            // Registrar ILogFilter como Singleton
            // NOTA: Debe ser Singleton porque SendLogUseCase (Singleton) lo necesita
            services.AddSingleton<ILogFilter, LogFilterService>();

            // Registrar ILogSamplingService (singleton con TimeProvider)
            services.AddSingleton<ILogSamplingService>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
                return new LogSamplingService(configManager, timeProvider);
            });

            // Registrar IDataSanitizationService como Singleton
            // NOTA: Debe ser Singleton porque SendLogUseCase (Singleton) lo necesita
            services.AddSingleton<IDataSanitizationService, DataSanitizationService>();

            // Registrar servicios de seguridad avanzada
            services.AddSingleton<IEncryptionService, EncryptionService>();
            // IAuditLoggingService se registrará después de IStructuredLoggingService (lo necesita)

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
            // SendLogUseCase es Scoped, por lo que usamos IServiceScopeFactory
            services.AddSingleton<SynchronousLogProcessor>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SynchronousLogProcessor>>();
                var logQueue = sp.GetRequiredService<LogQueue>();
                var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var enrichLogUseCase = sp.GetRequiredService<EnrichLogEntryUseCase>();
                return new SynchronousLogProcessor(logger, logQueue, serviceScopeFactory, enrichLogUseCase);
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
                services.AddSingleton<ILogSink>(sp =>
                {
                    var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                    var logger = sp.GetRequiredService<ILogger<SerilogSink>>();
                    var encryptionService = sp.GetService<IEncryptionService>();
                    return new SerilogSink(configManager, logger, encryptionService);
                });
            }

            // Registrar IStructuredLoggingService (inyectar LogQueue)
            // Usar ILoggerFactory en lugar de ILogger<T> para Singletons
            // Los UseCases ahora son Singleton, se pueden resolver directamente desde root provider
            services.AddSingleton<IStructuredLoggingService>(sp =>
            {
                System.Console.WriteLine("[DIAGNÓSTICO] 🔵 Registrando IStructuredLoggingService (WithoutHost) - Resolviendo dependencias...");
                try
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ ILoggerFactory resuelto");
                    
                    var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ ILoggingConfigurationManager resuelto");
                    
                    System.Console.WriteLine("[DIAGNÓSTICO]   🔵 Intentando resolver CreateLogEntryUseCase...");
                    var createUseCase = sp.GetService<CreateLogEntryUseCase>();
                    if (createUseCase == null)
                    {
                        throw new InvalidOperationException(
                            "CreateLogEntryUseCase no está registrado. Asegúrate de que AddApplicationServices() se haya llamado antes de AddSharedInfrastructureWithoutHost.");
                    }
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ CreateLogEntryUseCase resuelto");
                    
                    System.Console.WriteLine("[DIAGNÓSTICO]   🔵 Intentando resolver EnrichLogEntryUseCase...");
                    var enrichUseCase = sp.GetService<EnrichLogEntryUseCase>();
                    if (enrichUseCase == null)
                    {
                        throw new InvalidOperationException(
                            "EnrichLogEntryUseCase no está registrado. Asegúrate de que AddApplicationServices() se haya llamado antes de AddSharedInfrastructureWithoutHost.");
                    }
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ EnrichLogEntryUseCase resuelto");
                    
                    // SendLogUseCase es Scoped, no se puede resolver desde root provider
                    // Usar IServiceScopeFactory en su lugar
                    System.Console.WriteLine("[DIAGNÓSTICO]   🔵 Obteniendo IServiceScopeFactory (SendLogUseCase es Scoped)...");
                    var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ IServiceScopeFactory resuelto");
                    
                    var sinks = sp.GetServices<ILogSink>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ ILogSink resueltos: {sinks.Count()} sinks");
                    
                    var scopeManager = sp.GetRequiredService<ILogScopeManager>();
                    System.Console.WriteLine("[DIAGNÓSTICO]   ✅ ILogScopeManager resuelto");
                    
                    var kafkaProducer = sp.GetService<IKafkaProducer>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ IKafkaProducer: {(kafkaProducer != null ? "disponible" : "null")}");
                    
                    var logQueue = sp.GetService<ILogQueue>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ ILogQueue: {(logQueue != null ? "disponible" : "null")}");
                    
                    var priorityQueue = sp.GetService<IPriorityLogQueue>();
                    System.Console.WriteLine($"[DIAGNÓSTICO]   ✅ IPriorityLogQueue: {(priorityQueue != null ? "disponible" : "null")}");

                    var structuredLoggingService = new StructuredLoggingService(
                        loggerFactory, configManager, createUseCase, enrichUseCase, serviceScopeFactory,
                        sinks, scopeManager, kafkaProducer, logQueue, priorityQueue);
                    
                    System.Console.WriteLine("[DIAGNÓSTICO] ✅ IStructuredLoggingService (WithoutHost) instanciado exitosamente");
                    return structuredLoggingService;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[DIAGNÓSTICO] ❌ ERROR al registrar IStructuredLoggingService (WithoutHost): {ex.Message}");
                    System.Console.WriteLine($"[DIAGNÓSTICO] StackTrace: {ex.StackTrace}");
                    throw;
                }
            });

            // Registrar IAuditLoggingService después de IStructuredLoggingService (lo necesita)
            services.AddSingleton<IAuditLoggingService>(sp =>
            {
                var configManager = sp.GetRequiredService<ILoggingConfigurationManager>();
                var loggingService = sp.GetRequiredService<IStructuredLoggingService>();
                var logger = sp.GetService<ILogger<AuditLoggingService>>();
                return new AuditLoggingService(configManager, loggingService, logger);
            });

            // Actualizar LoggingConfigurationManager para usar IAuditLoggingService
            var existingConfigManager2 = services.FirstOrDefault(s => s.ServiceType == typeof(ILoggingConfigurationManager));
            if (existingConfigManager2 != null)
            {
                services.Remove(existingConfigManager2);
            }
            services.AddSingleton<ILoggingConfigurationManager>(sp =>
            {
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<LoggingConfiguration>>();
                var logger = sp.GetRequiredService<ILogger<LoggingConfigurationManager>>();
                var auditService = sp.GetService<IAuditLoggingService>();
                return new LoggingConfigurationManager(optionsMonitor, logger, auditService);
            });

            // ✅ Registrar Pipeline Behaviors de logging automático para MediatR
            // Esto registra automáticamente todas las peticiones y respuestas de MediatR
            // CRÍTICO: El ensamblado JonjubNet.Logging.Application debe estar cargado ANTES de esto
            // En Program.cs se fuerza la carga con: using JonjubNet.Logging.Application.Behaviours;
            // El orden importa: los Pipeline Behaviors se ejecutan en orden INVERSO al registro
            // LoggingBehaviour se registra PRIMERO aquí, luego ValidationBehaviour en AddApplicationServices
            // Por lo tanto, el orden de ejecución será: ValidationBehaviour -> LoggingBehaviour
            
            // 🔍 LOG DE DIAGNÓSTICO: Verificar que el tipo se puede resolver
            try
            {
                var loggingBehaviourType = typeof(JonjubNet.Logging.Application.Behaviours.LoggingBehaviour<,>);
                var assembly = loggingBehaviourType.Assembly;
                var assemblyName = assembly.GetName().Name;
                var typeName = loggingBehaviourType.FullName;
                
                // Usar Console.WriteLine como fallback para diagnóstico (no requiere ServiceProvider)
                System.Console.WriteLine($"[DIAGNÓSTICO] Intentando registrar LoggingBehaviour");
                System.Console.WriteLine($"[DIAGNÓSTICO] Assembly={assemblyName}, Type={typeName}");
                System.Console.WriteLine($"[DIAGNÓSTICO] Assembly Location={assembly.Location ?? "N/A"}");
                
                services.AddTransient(typeof(IPipelineBehavior<,>), loggingBehaviourType);
                
                System.Console.WriteLine($"[DIAGNÓSTICO] ✅✅✅ LoggingBehaviour REGISTRADO correctamente como IPipelineBehavior ✅✅✅");
            }
            catch (Exception ex)
            {
                // Si falla, loguear el error y registrar de todas formas
                System.Console.WriteLine($"[DIAGNÓSTICO] ❌❌❌ ERROR al registrar LoggingBehaviour: {ex.Message}");
                System.Console.WriteLine($"[DIAGNÓSTICO] StackTrace: {ex.StackTrace}");
                
                // Intentar registrar de todas formas
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(JonjubNet.Logging.Application.Behaviours.LoggingBehaviour<,>));
            }

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
            // NOTA: IHttpContextProvider debe ser Singleton porque EnrichLogEntryUseCase (Singleton) lo necesita
            // IHttpContextAccessor es thread-safe y puede ser Singleton
            services.AddHttpContextAccessor();
            services.AddSingleton<IHttpContextProvider, AspNetCoreHttpContextProvider>();
#else
            // ASP.NET Core no está disponible - usar implementación null
            // NOTA: IHttpContextProvider debe ser Singleton porque EnrichLogEntryUseCase (Singleton) lo necesita
            services.AddSingleton<IHttpContextProvider, NullHttpContextProvider>();
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

