# Changelog

Todos los cambios notables de este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-01-15

### Actualizado
- **Framework**: Actualizado a .NET 10.0 (desde .NET 8.0)
- **Paquetes Microsoft.Extensions**: Actualizados a versión 9.0.9
  - Microsoft.Extensions.DependencyInjection.Abstractions
  - Microsoft.Extensions.Logging.Abstractions
  - Microsoft.Extensions.Logging
  - Microsoft.Extensions.Configuration.Abstractions
  - Microsoft.Extensions.Options
  - Microsoft.Extensions.Options.ConfigurationExtensions
- **Paquetes Serilog**: Actualizados a las versiones más recientes
  - Serilog actualizado a versión 4.3.0
  - Serilog.AspNetCore actualizado a versión 9.0.0
  - Serilog.Sinks.Console actualizado a versión 6.0.0
  - Serilog.Sinks.File actualizado a versión 7.0.0
  - Serilog.Sinks.Http actualizado a versión 9.2.0
  - Serilog.Enrichers.Environment actualizado a versión 3.0.1
  - Serilog.Enrichers.Process actualizado a versión 3.0.0
  - Serilog.Enrichers.Thread actualizado a versión 4.0.0
  - Serilog.Formatting.Compact actualizado a versión 3.0.0
- **Otros paquetes**:
  - Microsoft.AspNetCore.Http.Abstractions actualizado a versión 2.3.0
  - System.Text.Json eliminado (incluido en .NET 10.0)

### Agregado
- Implementación inicial del servicio de logging estructurado
- Soporte para múltiples sinks: Console, File, HTTP, Elasticsearch, Kafka
- Funcionalidades de correlación: CorrelationId, RequestId, SessionId
- Enriquecimiento automático con información de Environment, Process, Thread, Machine
- Sistema de filtros avanzado por categoría, operación, usuario y nivel de log
- Configuración flexible via appsettings.json
- Integración nativa con ASP.NET Core
- Interfaz genérica para servicio de usuario actual
- Implementación por defecto del servicio de usuario
- Soporte para logging de operaciones con medición de tiempo
- Logging de eventos específicos: UserAction, SecurityEvent, AuditEvent
- Logging personalizado con StructuredLogEntry
- Documentación completa con ejemplos de uso
- Scripts de build para Windows y Linux/Mac
- Configuración de CI/CD con GitHub Actions

### Características Técnicas
- Basado en Serilog para máximo rendimiento
- Soporte para .NET 8.0
- Configuración via IConfiguration
- Inyección de dependencias nativa
- Logs en formato JSON estructurado
- Envío asíncrono a Kafka (fire-and-forget)
- Fallback a logging local si Kafka falla
- Headers HTTP personalizables
- Compresión configurable para Kafka
- Retry automático para envío a Kafka
- Rolling files con límites configurables
- Templates de output personalizables

### Configuración
- Sección de configuración: "StructuredLogging"
- Niveles de log: Trace, Debug, Information, Warning, Error, Critical, Fatal
- Categorías predefinidas: General, Security, Audit, Performance, UserAction, System, Business, Integration, Database, External
- Headers de correlación configurables
- Propiedades estáticas personalizables
- Filtros por categoría, operación y usuario
- Límites de archivo y retención configurables

### Ejemplos Incluidos
- Uso básico del servicio de logging
- Logging de operaciones con medición de tiempo
- Logging de eventos específicos
- Logging personalizado
- Implementaciones de ICurrentUserService para diferentes escenarios
- Configuración completa de appsettings.json
- Scripts de build y publicación

### Documentación
- README.md completo con ejemplos
- Comentarios XML en todo el código
- Ejemplos de uso en la carpeta Examples/
- Configuración de ejemplo en appsettings.example.json
- Licencia MIT incluida
