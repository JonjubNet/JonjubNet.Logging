using JonjubNet.Logging.Configuration;
using JonjubNet.Logging.Interfaces;
using JonjubNet.Logging.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Linq;
using System.Reflection;

namespace JonjubNet.Logging
{
    public static class ServiceExtensions
    {
        private static bool _serilogConfigured = false;
        private static bool _serilogProviderAdded = false;
        private static readonly object _lockObject = new object();

        public static void AddStructuredLoggingInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configurar y registrar LoggingConfiguration como IOptions
            services.Configure<LoggingConfiguration>(configuration.GetSection(LoggingConfiguration.SectionName));
            
            // Registrar IHttpContextAccessor para obtener información HTTP (RequestPath, RequestMethod, etc.)
            // Esto es necesario para llenar campos HTTP en los logs
            // Solo registrar si no está ya registrado
            if (!services.Any(s => s.ServiceType == typeof(IHttpContextAccessor)))
            {
                services.AddHttpContextAccessor();
            }
            
            // Registrar el servicio de usuario por defecto solo si no está ya registrado
            // Esto permite que las aplicaciones proporcionen su propia implementación
            if (!services.Any(s => s.ServiceType == typeof(ICurrentUserService)))
            {
                services.AddScoped<ICurrentUserService, DefaultCurrentUserService>();
            }

            // Registrar el servicio de categorización de errores genérico como Singleton
            // Es thread-safe (usa ConcurrentDictionary) y puede ser compartido entre requests
            // Solo registrar si no está ya registrado
            if (!services.Any(s => s.ServiceType == typeof(IErrorCategorizationService)))
            {
                services.AddSingleton<IErrorCategorizationService, ErrorCategorizationService>();
            }

            // Registrar el servicio de logging estructurado
            // Solo registrar si no está ya registrado
            if (!services.Any(s => s.ServiceType == typeof(IStructuredLoggingService)))
            {
                services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
            }

            // Registrar el LoggingBehaviour automático para MediatR (opcional, solo si MediatR está disponible)
            // Este behavior captura automáticamente todas las operaciones sin código manual
            // Nota: Requiere que MediatR esté instalado en la aplicación que usa esta biblioteca
            // Comentado temporalmente hasta que MediatR esté completamente configurado
            // RegisterLoggingBehaviour(services);

            // Configurar Serilog solo una vez
            lock (_lockObject)
            {
                if (!_serilogConfigured)
                {
                    var loggingConfig = configuration.GetSection(LoggingConfiguration.SectionName).Get<LoggingConfiguration>() ?? new LoggingConfiguration();
                    ConfigureSerilog(loggingConfig);
                    _serilogConfigured = true;
                }
            }

            // Agregar Serilog al pipeline de logging solo una vez
            lock (_lockObject)
            {
                if (!_serilogProviderAdded)
                {
                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddSerilog(dispose: true);
                    });
                    _serilogProviderAdded = true;
                }
            }
        }

        /// <summary>
        /// Agrega el servicio de logging estructurado con un servicio de usuario personalizado
        /// </summary>
        /// <typeparam name="TUserService">Tipo del servicio de usuario</typeparam>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración</param>
        public static void AddStructuredLoggingInfrastructure<TUserService>(this IServiceCollection services, IConfiguration configuration)
            where TUserService : class, ICurrentUserService
        {
            // Configurar y registrar LoggingConfiguration como IOptions
            services.Configure<LoggingConfiguration>(configuration.GetSection(LoggingConfiguration.SectionName));
            
            // Registrar IHttpContextAccessor para obtener información HTTP (RequestPath, RequestMethod, etc.)
            // Esto es necesario para llenar campos HTTP en los logs
            // Solo registrar si no está ya registrado
            if (!services.Any(s => s.ServiceType == typeof(IHttpContextAccessor)))
            {
                services.AddHttpContextAccessor();
            }
            
            // Registrar el servicio de usuario personalizado
            // Solo registrar si no está ya registrado
            if (!services.Any(s => s.ServiceType == typeof(ICurrentUserService)))
            {
                services.AddScoped<ICurrentUserService, TUserService>();
            }

            // Registrar el servicio de categorización de errores genérico como Singleton
            // Es thread-safe (usa ConcurrentDictionary) y puede ser compartido entre requests
            // Solo registrar si no está ya registrado
            if (!services.Any(s => s.ServiceType == typeof(IErrorCategorizationService)))
            {
                services.AddSingleton<IErrorCategorizationService, ErrorCategorizationService>();
            }

            // Registrar el servicio de logging estructurado
            // Solo registrar si no está ya registrado
            if (!services.Any(s => s.ServiceType == typeof(IStructuredLoggingService)))
            {
                services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
            }

            // Registrar el LoggingBehaviour automático para MediatR (opcional, solo si MediatR está disponible)
            // Este behavior captura automáticamente todas las operaciones sin código manual
            // Nota: Requiere que MediatR esté instalado en la aplicación que usa esta biblioteca
            // Comentado temporalmente hasta que MediatR esté completamente configurado
            // RegisterLoggingBehaviour(services);

            // Configurar Serilog solo una vez
            lock (_lockObject)
            {
                if (!_serilogConfigured)
                {
                    var loggingConfig = configuration.GetSection(LoggingConfiguration.SectionName).Get<LoggingConfiguration>() ?? new LoggingConfiguration();
                    ConfigureSerilog(loggingConfig);
                    _serilogConfigured = true;
                }
            }

            // Agregar Serilog al pipeline de logging solo una vez
            lock (_lockObject)
            {
                if (!_serilogProviderAdded)
                {
                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddSerilog(dispose: true);
                    });
                    _serilogProviderAdded = true;
                }
            }
        }

        /// <summary>
        /// Registra el LoggingBehaviour para MediatR si está disponible
        /// </summary>
        private static void RegisterLoggingBehaviour(IServiceCollection services)
        {
            try
            {
                // Verificar si MediatR está disponible usando reflexión
                var mediatRAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "MediatR");

                if (mediatRAssembly == null)
                {
                    // Intentar cargar MediatR
                    try
                    {
                        mediatRAssembly = Assembly.Load("MediatR");
                    }
                    catch
                    {
                        return; // MediatR no está disponible
                    }
                }

                // Obtener el tipo IPipelineBehavior usando reflexión
                var pipelineBehaviorType = mediatRAssembly.GetType("MediatR.IPipelineBehavior`2");
                if (pipelineBehaviorType == null)
                {
                    return; // No se encontró IPipelineBehavior
                }

                // Obtener el tipo LoggingBehaviour
                var loggingBehaviourType = Type.GetType("JonjubNet.Logging.Behaviours.LoggingBehaviour`2, JonjubNet.Logging");
                if (loggingBehaviourType == null)
                {
                    return; // No se encontró LoggingBehaviour
                }

                // Registrar el behavior usando el método AddTransient con tipos
                var addTransientMethod = typeof(ServiceCollectionServiceExtensions)
                    .GetMethods()
                    .FirstOrDefault(m => m.Name == "AddTransient" && 
                                        !m.IsGenericMethod &&
                                        m.GetParameters().Length == 2 &&
                                        m.GetParameters()[0].ParameterType == typeof(IServiceCollection) &&
                                        m.GetParameters()[1].ParameterType == typeof(Type));

                if (addTransientMethod != null)
                {
                    // Buscar el método correcto: AddTransient(IServiceCollection, Type, Type)
                    var correctMethod = typeof(ServiceCollectionServiceExtensions)
                        .GetMethods()
                        .FirstOrDefault(m => m.Name == "AddTransient" && 
                                            !m.IsGenericMethod &&
                                            m.GetParameters().Length == 3 &&
                                            m.GetParameters()[0].ParameterType == typeof(IServiceCollection) &&
                                            m.GetParameters()[1].ParameterType == typeof(Type) &&
                                            m.GetParameters()[2].ParameterType == typeof(Type));

                    if (correctMethod != null)
                    {
                        correctMethod.Invoke(null, new object[] { services, pipelineBehaviorType, loggingBehaviourType });
                    }
                }
            }
            catch
            {
                // Si MediatR no está disponible o no está registrado, ignorar silenciosamente
                // El behavior solo funcionará si MediatR está registrado en la aplicación
            }
        }

        private static void ConfigureSerilog(LoggingConfiguration config)
        {
            // Si el logger ya está configurado, no configurarlo de nuevo
            if (Log.Logger != null && Log.Logger != Serilog.Core.Logger.None)
            {
                return;
            }

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(GetLogLevel(config.MinimumLevel))
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", config.ServiceName)
                .Enrich.WithProperty("Environment", config.Environment)
                .Enrich.WithProperty("Version", config.Version);

            // Enriquecimiento automático
            if (config.Enrichment.IncludeEnvironment)
            {
                loggerConfig.Enrich.WithEnvironmentName();
            }

            if (config.Enrichment.IncludeProcess)
            {
                loggerConfig.Enrich.WithProcessId()
                                 .Enrich.WithProcessName();
            }

            if (config.Enrichment.IncludeThread)
            {
                loggerConfig.Enrich.WithThreadId()
                                 .Enrich.WithThreadName();
            }

            if (config.Enrichment.IncludeMachineName)
            {
                loggerConfig.Enrich.WithMachineName();
            }

            // Configurar sinks
            if (config.Sinks.EnableConsole)
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            if (config.Sinks.EnableFile)
            {
                var fileConfig = config.Sinks.File;
                var rollingInterval = GetRollingInterval(fileConfig.RollingInterval);
                
                loggerConfig.WriteTo.File(
                    path: fileConfig.Path,
                    rollingInterval: rollingInterval,
                    retainedFileCountLimit: fileConfig.RetainedFileCountLimit,
                    fileSizeLimitBytes: fileConfig.FileSizeLimitBytes,
                    outputTemplate: fileConfig.OutputTemplate,
                    formatProvider: null);
            }

            if (config.Sinks.EnableHttp && !string.IsNullOrEmpty(config.Sinks.Http.Url))
            {
                var httpConfig = config.Sinks.Http;
                loggerConfig.WriteTo.Http(
                    requestUri: httpConfig.Url,
                    queueLimitBytes: 1000000,
                    period: TimeSpan.FromSeconds(httpConfig.PeriodSeconds),
                    textFormatter: new Serilog.Formatting.Compact.CompactJsonFormatter());
            }

            // Aplicar propiedades estáticas
            foreach (var property in config.Enrichment.StaticProperties)
            {
                loggerConfig.Enrich.WithProperty(property.Key, property.Value);
            }

            // Crear el logger solo si no existe
            if (Log.Logger == null || Log.Logger == Serilog.Core.Logger.None)
            {
                Log.Logger = loggerConfig.CreateLogger();
            }
        }

        private static LogEventLevel GetLogLevel(string level)
        {
            return level.ToLowerInvariant() switch
            {
                "trace" => LogEventLevel.Verbose,
                "debug" => LogEventLevel.Debug,
                "information" => LogEventLevel.Information,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "critical" => LogEventLevel.Fatal,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }

        private static Serilog.RollingInterval GetRollingInterval(string interval)
        {
            return interval.ToLowerInvariant() switch
            {
                "infinite" => Serilog.RollingInterval.Infinite,
                "year" => Serilog.RollingInterval.Year,
                "month" => Serilog.RollingInterval.Month,
                "day" => Serilog.RollingInterval.Day,
                "hour" => Serilog.RollingInterval.Hour,
                "minute" => Serilog.RollingInterval.Minute,
                _ => Serilog.RollingInterval.Day
            };
        }
    }
}