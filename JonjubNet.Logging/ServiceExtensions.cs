using JonjubNet.Logging.Configuration;
using JonjubNet.Logging.Interfaces;
using JonjubNet.Logging.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace JonjubNet.Logging
{
    public static class ServiceExtensions
    {
        public static void AddStructuredLoggingInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Registrar el servicio de usuario por defecto
            services.AddScoped<ICurrentUserService, DefaultCurrentUserService>();

            // Registrar el servicio de logging estructurado
            services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();

            // Configurar Serilog
            var loggingConfig = configuration.GetSection(LoggingConfiguration.SectionName).Get<LoggingConfiguration>() ?? new LoggingConfiguration();
            ConfigureSerilog(loggingConfig);

            // Agregar Serilog al pipeline de logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });
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
            // Registrar el servicio de usuario personalizado
            services.AddScoped<ICurrentUserService, TUserService>();

            // Registrar el servicio de logging estructurado
            services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();

            // Configurar Serilog
            var loggingConfig = configuration.GetSection(LoggingConfiguration.SectionName).Get<LoggingConfiguration>() ?? new LoggingConfiguration();
            ConfigureSerilog(loggingConfig);

            // Agregar Serilog al pipeline de logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });
        }

        private static void ConfigureSerilog(LoggingConfiguration config)
        {
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

            // Crear el logger
            Log.Logger = loggerConfig.CreateLogger();
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