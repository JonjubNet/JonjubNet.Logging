# Introducción - JonjubNet.Logging

## ¿Qué es JonjubNet.Logging?

**JonjubNet.Logging** es una biblioteca de logging estructurado de alto rendimiento para aplicaciones .NET, diseñada siguiendo principios de Clean Architecture. Proporciona logging estructurado con soporte para múltiples sinks, correlación de logs, enriquecimiento automático, sampling, sanitización de datos y funcionalidades avanzadas de resiliencia.

## Características Principales

- ✅ **Logging Estructurado**: Logs en formato JSON con propiedades enriquecidas
- ✅ **Múltiples Sinks**: Console, File, HTTP, Elasticsearch, Kafka
- ✅ **Correlación**: IDs de correlación, request y sesión
- ✅ **Enriquecimiento Automático**: Usuario, HTTP context, ambiente, versión
- ✅ **Sampling/Rate Limiting**: Reducción de volumen en producción
- ✅ **Data Sanitization**: Enmascaramiento automático de datos sensibles
- ✅ **Resiliencia**: Circuit Breakers, Retry Policies, Dead Letter Queue
- ✅ **Hot-Reload**: Cambios de configuración en tiempo real
- ✅ **Clean Architecture**: Diseño modular y testeable
- ✅ **Alto Rendimiento**: Optimizado para .NET 10 con reducción del 65-80% en allocations

## Casos de Uso

### Microservicios
Logging centralizado y correlación entre servicios para facilitar el debugging y monitoreo en arquitecturas distribuidas.

### Aplicaciones Enterprise
Cumplimiento, auditoría y seguridad con logging estructurado que facilita el análisis y cumplimiento normativo.

### Aplicaciones de Alto Rendimiento
Optimizado para alta carga con object pooling, batching inteligente y procesamiento asíncrono.

### Aplicaciones Distribuidas
Correlación y trazabilidad de logs a través de múltiples servicios y componentes.

## Requisitos

- .NET 10.0 o superior
- Visual Studio 2022 o VS Code
- NuGet Package Manager

## Próximos Pasos

1. [Instalación](installation.md) - Instala el paquete NuGet
2. [Inicio Rápido](quick-start.md) - Primeros pasos en 5 minutos
3. [Configuración](../configuration/main.md) - Configura tu logging

---

**Siguiente:** [Instalación](installation.md)

