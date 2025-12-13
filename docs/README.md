# 📚 Documentación - JonjubNet.Logging

**Versión:** 3.1.1  
**Framework:** .NET 10 / C# 13  
**Licencia:** MIT  
**Última Actualización:** Diciembre 2024

---

## 📋 Índice de Documentación

### 🚀 Inicio Rápido
- [Introducción](getting-started/introduction.md) - ¿Qué es JonjubNet.Logging?
- [Instalación](getting-started/installation.md) - Guía de instalación paso a paso
- [Inicio Rápido](getting-started/quick-start.md) - Primeros pasos en 5 minutos

### 🏗️ Arquitectura
- [Arquitectura General](architecture/overview.md) - Clean Architecture y principios
- [Estructura de Componentes](architecture/components.md) - Capas y componentes
- [Flujo de Datos](architecture/data-flow.md) - Cómo fluyen los logs

### ⚙️ Configuración
- [Configuración Principal](configuration/main.md) - Configuración básica
- [Sinks](configuration/sinks.md) - Console, File, HTTP, Elasticsearch, Kafka
- [Filtros y Sampling](configuration/filters-sampling.md) - Filtrado y rate limiting
- [Enriquecimiento](configuration/enrichment.md) - Enriquecimiento automático
- [Resiliencia](configuration/resilience.md) - Circuit Breakers, Retry, DLQ
- [Batching](configuration/batching.md) - Batching y compresión
- [Seguridad Avanzada](configuration/security.md) - Encriptación en tránsito, en reposo, Audit Logging

### 💻 Implementación
- [ASP.NET Core](implementation/aspnet-core.md) - Implementación en ASP.NET Core
- [Aplicaciones Sin Host](implementation/without-host.md) - Console, Blazor WebAssembly
- [MediatR Integration](implementation/mediatr.md) - Logging automático con MediatR
- [Servicios Personalizados](implementation/custom-services.md) - Extensibilidad

### 📖 Referencia de API
- [IStructuredLoggingService](api-reference/structured-logging-service.md) - Servicio principal
- [ILoggingConfigurationManager](api-reference/configuration-manager.md) - Gestión de configuración
- [Interfaces Adicionales](api-reference/additional-interfaces.md) - Otras interfaces

### 💡 Ejemplos
- [Ejemplos Básicos](examples/basic-usage.md) - Uso básico
- [Scopes y Contexto](examples/scopes.md) - Uso de scopes
- [Operaciones](examples/operations.md) - Logging de operaciones
- [Seguridad y Auditoría](examples/security-audit.md) - Eventos de seguridad y auditoría
- [Configuración Dinámica](examples/dynamic-configuration.md) - Cambios en runtime

### 🔧 Troubleshooting
- [Problemas Comunes](troubleshooting/common-issues.md) - Soluciones a problemas frecuentes
- [Diagnóstico](troubleshooting/diagnostics.md) - Herramientas de diagnóstico
- [FAQ](troubleshooting/faq.md) - Preguntas frecuentes

### ⚡ Performance
- [Optimizaciones](performance/optimizations.md) - Optimizaciones implementadas
- [Mejores Prácticas](performance/best-practices.md) - Recomendaciones de performance
- [Configuraciones Recomendadas](performance/recommended-configs.md) - Configuraciones por escenario

---

## 🎯 Guía de Navegación

### Para Nuevos Usuarios
1. Lee [Introducción](getting-started/introduction.md)
2. Sigue [Instalación](getting-started/installation.md)
3. Completa [Inicio Rápido](getting-started/quick-start.md)
4. Revisa [Ejemplos Básicos](examples/basic-usage.md)

### Para Desarrolladores
1. Revisa [Arquitectura General](architecture/overview.md)
2. Consulta [Referencia de API](api-reference/structured-logging-service.md)
3. Explora [Ejemplos](examples/basic-usage.md)

### Para Configuración
1. Lee [Configuración Principal](configuration/main.md)
2. Configura [Sinks](configuration/sinks.md)
3. Ajusta [Filtros y Sampling](configuration/filters-sampling.md)
4. Revisa [Resiliencia](configuration/resilience.md)

### Para Troubleshooting
1. Consulta [Problemas Comunes](troubleshooting/common-issues.md)
2. Usa [Diagnóstico](troubleshooting/diagnostics.md)
3. Revisa [FAQ](troubleshooting/faq.md)

---

## 📚 Recursos Adicionales

- **README.md** (raíz): Documentación básica del proyecto
- **EVALUACION_PRODUCCION.md**: Evaluación completa para producción
- **appsettings.example.json**: Ejemplo completo de configuración en `presentation/JonjubNet.Logging/appsettings.example.json`

---

## 🔄 Changelog

### Versión 3.1.1 (Diciembre 2024)
- ✅ Corrección de dependencia circular (DictionaryPool movido a Domain)
- ✅ Mejora en limpieza de cache (sin allocations adicionales)
- ✅ ReaderWriterLockSlim en LoggingConfigurationManager
- ✅ Optimizaciones adicionales de performance

### Versión 3.0.12 (Diciembre 2024)
- ✅ Optimizaciones críticas de performance
- ✅ DictionaryPool implementado en hot paths
- ✅ CloneLogEntry optimizado
- ✅ Eliminación de ToList() innecesarios

---

## 📄 Licencia

MIT License - Ver archivo LICENSE para más detalles.

---

**Última Actualización:** Diciembre 2024  
**Versión del Documento:** 1.0

