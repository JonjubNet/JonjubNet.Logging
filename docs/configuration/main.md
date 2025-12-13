# Configuración Principal - JonjubNet.Logging

## Estructura de Configuración

La configuración se estructura en `appsettings.json` bajo la sección `StructuredLogging`:

```json
{
  "StructuredLogging": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "ServiceName": "MiServicio",
    "Environment": "Development",
    "Version": "1.0.0"
  }
}
```

## Propiedades Principales

### Enabled
Habilita o deshabilita el logging estructurado.

```json
"Enabled": true
```

### MinimumLevel
Nivel mínimo de log que se registrará. Valores posibles:
- `Trace` - Más detallado
- `Debug` - Información de depuración
- `Information` - Eventos importantes (recomendado para producción)
- `Warning` - Advertencias
- `Error` - Errores
- `Critical` - Errores críticos

```json
"MinimumLevel": "Information"
```

### ServiceName
Nombre del servicio o aplicación. Se incluye en todos los logs.

```json
"ServiceName": "MiServicio"
```

### Environment
Ambiente de ejecución. Valores comunes:
- `Development`
- `Staging`
- `Production`

```json
"Environment": "Development"
```

### Version
Versión de la aplicación. Se incluye en todos los logs.

```json
"Version": "1.0.0"
```

## Configuración Mínima

Para empezar, esta es la configuración mínima necesaria:

```json
{
  "StructuredLogging": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "ServiceName": "MiServicio",
    "Sinks": {
      "EnableConsole": true
    }
  }
}
```

## Configuración Completa

Para ver todas las opciones de configuración disponibles, consulta:

- [Sinks](sinks.md) - Configuración de destinos de logs
- [Filtros y Sampling](filters-sampling.md) - Filtrado y rate limiting
- [Enriquecimiento](enrichment.md) - Enriquecimiento automático
- [Resiliencia](resilience.md) - Circuit Breakers, Retry, DLQ
- [Batching](batching.md) - Batching y compresión

## Hot-Reload

La configuración se puede cambiar en tiempo real si habilitas `reloadOnChange`:

```csharp
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
```

Los cambios en `appsettings.json` se aplicarán automáticamente sin reiniciar la aplicación.

## Configuración por Ambiente

Puedes tener diferentes configuraciones por ambiente:

- `appsettings.json` - Configuración base
- `appsettings.Development.json` - Desarrollo
- `appsettings.Staging.json` - Staging
- `appsettings.Production.json` - Producción

---

**Siguiente:** [Sinks](sinks.md)

