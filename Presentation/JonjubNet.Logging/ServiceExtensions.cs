using JonjubNet.Logging.Application;
using JonjubNet.Logging.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JonjubNet.Logging
{
    /// <summary>
    /// Extensiones para registrar todos los servicios de logging estructurado
    /// Este es el punto de entrada principal para la configuración
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Registra todos los servicios necesarios para logging estructurado
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para encadenamiento</returns>
        public static IServiceCollection AddStructuredLoggingInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return AddStructuredLoggingInfrastructure<Shared.Services.DefaultCurrentUserService>(services, configuration);
        }

        /// <summary>
        /// Registra todos los servicios necesarios para logging estructurado con un servicio de usuario personalizado
        /// </summary>
        /// <typeparam name="TUserService">Tipo del servicio de usuario personalizado</typeparam>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para encadenamiento</returns>
        public static IServiceCollection AddStructuredLoggingInfrastructure<TUserService>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TUserService : class, Application.Interfaces.ICurrentUserService
        {
            // Registrar servicios de Application
            services.AddApplicationServices();

            // Registrar servicios de Infrastructure (Shared)
            services.AddSharedInfrastructure<TUserService>(configuration);

            return services;
        }

        /// <summary>
        /// Registra todos los servicios necesarios para logging estructurado SIN BackgroundService
        /// Útil para aplicaciones sin host (Console Apps simples, Blazor WebAssembly, etc.)
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para encadenamiento</returns>
        public static IServiceCollection AddStructuredLoggingInfrastructureWithoutHost(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return AddStructuredLoggingInfrastructureWithoutHost<Shared.Services.DefaultCurrentUserService>(services, configuration);
        }

        /// <summary>
        /// Registra todos los servicios necesarios para logging estructurado SIN BackgroundService
        /// con un servicio de usuario personalizado
        /// </summary>
        /// <typeparam name="TUserService">Tipo del servicio de usuario personalizado</typeparam>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para encadenamiento</returns>
        public static IServiceCollection AddStructuredLoggingInfrastructureWithoutHost<TUserService>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TUserService : class, Application.Interfaces.ICurrentUserService
        {
            // Registrar servicios de Application
            services.AddApplicationServices();

            // Registrar servicios de Infrastructure (Shared) sin BackgroundService
            services.AddSharedInfrastructureWithoutHost<TUserService>(configuration);

            return services;
        }
    }
}

