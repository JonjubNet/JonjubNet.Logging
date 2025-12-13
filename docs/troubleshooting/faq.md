# FAQ - JonjubNet.Logging

## Preguntas Frecuentes

### ¿Puedo usar esto en aplicaciones que no son ASP.NET Core?

Sí, usa `AddStructuredLoggingInfrastructureWithoutHost()` para aplicaciones sin host (Console, Blazor WebAssembly, etc.).

### ¿Cómo cambio la configuración en runtime?

Usa `ILoggingConfigurationManager` para cambiar configuración en runtime. Los cambios se aplican inmediatamente.

```csharp
var configManager = serviceProvider.GetRequiredService<ILoggingConfigurationManager>();
configManager.SetMinimumLevel("Debug");
```

### ¿Los logs se envían de forma asíncrona?

Sí, los logs se envían de forma asíncrona usando `Channel<T>` y `BackgroundService`. No bloquean la aplicación.

### ¿Qué pasa si un sink falla?

El sistema usa circuit breakers y retry policies. Si un sink falla repetidamente, se abre el circuit breaker y los logs van a Dead Letter Queue.

### ¿Cómo personalizo el formato de los logs?

Los logs se estructuran automáticamente. Puedes personalizar el formato en los sinks (especialmente File sink con `OutputTemplate`).

### ¿Puedo agregar mis propios sinks?

Sí, implementa `ILogSink` y regístralo en `ServiceExtensions`.

```csharp
public class CustomSink : ILogSink
{
    public async Task SendAsync(StructuredLogEntry logEntry, CancellationToken cancellationToken = default)
    {
        // Tu lógica aquí
    }
}
```

### ¿Cómo funciona el hot-reload?

Cuando cambias `appsettings.json`, `IOptionsMonitor` detecta el cambio y actualiza la configuración automáticamente. Asegúrate de tener `reloadOnChange: true`.

### ¿Qué es el sampling?

El sampling reduce el volumen de logs registrando solo un porcentaje de ellos. Útil para logs de bajo nivel en producción.

### ¿Cómo se sanitizan los datos sensibles?

El sistema detecta automáticamente datos sensibles usando nombres de propiedades y patrones regex, y los enmascara antes de enviarlos a los sinks.

### ¿Puedo usar esto con Serilog existente?

Sí, el componente usa Serilog internamente para algunos sinks, pero puedes usar ambos sistemas simultáneamente.

### ¿Cómo configuro múltiples sinks?

Simplemente habilita múltiples sinks en la configuración:

```json
{
  "StructuredLogging": {
    "Sinks": {
      "EnableConsole": true,
      "EnableFile": true,
      "EnableHttp": true
    }
  }
}
```

### ¿Los logs se almacenan localmente antes de enviarse?

Sí, los logs se procesan de forma asíncrona usando colas. Si un sink falla, los logs van a Dead Letter Queue.

### ¿Cómo cambio el nivel de log para una categoría específica?

Usa `CategoryLogLevels` en la configuración:

```json
{
  "StructuredLogging": {
    "Filters": {
      "CategoryLogLevels": {
        "Security": "Warning",
        "Performance": "Information"
      }
    }
  }
}
```

---

**Anterior:** [Problemas Comunes](common-issues.md)

