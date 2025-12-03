using Microsoft.Extensions.Configuration;

namespace JonjubNet.Logging
{
    /// <summary>
    /// Extensiones para facilitar la carga de configuración modular
    /// Útil cuando hay múltiples componentes con configuración extensa
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Carga automáticamente archivos de configuración desde la carpeta 'config'
        /// Útil cuando hay múltiples componentes con configuración extensa
        /// </summary>
        /// <param name="builder">Configuration builder</param>
        /// <param name="contentRootPath">Ruta raíz del contenido (usualmente Environment.ContentRootPath)</param>
        /// <param name="componentNames">Nombres opcionales de componentes a cargar. Si está vacío, carga todos los .json en config/</param>
        /// <returns>Configuration builder para chaining</returns>
        /// <example>
        /// <code>
        /// builder.Configuration
        ///     .AddJsonFile("appsettings.json")
        ///     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        ///     .AddComponentConfigurations(builder.Environment.ContentRootPath);
        /// </code>
        /// </example>
        public static IConfigurationBuilder AddComponentConfigurations(
            this IConfigurationBuilder builder,
            string contentRootPath,
            params string[] componentNames)
        {
            var configPath = contentRootPath;
            var configDir = Path.Combine(configPath, "config");

            if (!Directory.Exists(configDir))
                return builder;

            if (componentNames.Length == 0)
            {
                // Cargar todos los .json en config/
                var configFiles = Directory.GetFiles(configDir, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var file in configFiles)
                {
                    var relativePath = Path.GetRelativePath(configPath, file);
                    builder.AddJsonFile(relativePath, optional: true, reloadOnChange: true);
                }
            }
            else
            {
                // Cargar solo los componentes especificados
                foreach (var componentName in componentNames)
                {
                    var configFile = Path.Combine(configDir, $"{componentName}.json");
                    if (File.Exists(configFile))
                    {
                        var relativePath = Path.GetRelativePath(configPath, configFile);
                        builder.AddJsonFile(relativePath, optional: true, reloadOnChange: true);
                    }
                }
            }

            return builder;
        }

        /// <summary>
        /// Carga configuración de un componente específico desde archivo separado
        /// </summary>
        /// <param name="builder">Configuration builder</param>
        /// <param name="configFilePath">Ruta relativa al archivo de configuración (ej: "config/structured-logging.json")</param>
        /// <param name="optional">Si es true, no lanza excepción si el archivo no existe</param>
        /// <returns>Configuration builder para chaining</returns>
        /// <example>
        /// <code>
        /// builder.Configuration
        ///     .AddJsonFile("appsettings.json")
        ///     .AddComponentConfiguration("config/structured-logging.json");
        /// </code>
        /// </example>
        public static IConfigurationBuilder AddComponentConfiguration(
            this IConfigurationBuilder builder,
            string configFilePath,
            bool optional = true)
        {
            builder.AddJsonFile(configFilePath, optional: optional, reloadOnChange: true);
            return builder;
        }
    }
}

