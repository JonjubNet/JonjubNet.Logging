# Problemas Comunes - JonjubNet.Logging

## Los logs no aparecen

### Causas Posibles

1. **Logging deshabilitado**: Verifica `"Enabled": true` en configuración
2. **Nivel mínimo muy alto**: Verifica `MinimumLevel` en configuración
3. **Filtros activos**: Revisa configuración de `Filters`
4. **Sampling activo**: Revisa configuración de `Sampling`

### Solución

```csharp
// Verificar configuración actual
var configManager = serviceProvider.GetRequiredService<ILoggingConfigurationManager>();
var config = configManager.Current;
Console.WriteLine($"Enabled: {config.Enabled}");
Console.WriteLine($"MinimumLevel: {config.MinimumLevel}");
```

## Los logs no se envían a un sink específico

### Causas Posibles

1. **Sink deshabilitado**: Verifica `EnableConsole`, `EnableFile`, etc.
2. **Circuit breaker abierto**: Revisa estado del circuit breaker
3. **Error de conexión**: Revisa logs de errores

### Solución

```csharp
// Habilitar sink en runtime
var configManager = serviceProvider.GetRequiredService<ILoggingConfigurationManager>();
configManager.SetSinkEnabled("Http", true);

// Verificar estado del circuit breaker
var circuitBreakerManager = serviceProvider.GetService<ICircuitBreakerManager>();
var breaker = circuitBreakerManager?.GetBreaker("Http");
Console.WriteLine($"Circuit Breaker State: {breaker?.State}");
```

## Alto uso de memoria

### Causas Posibles

1. **Cola de logs llena**: Los logs se están acumulando
2. **Dead Letter Queue grande**: Muchos logs fallidos
3. **Cache sin límites**: Caches creciendo sin control

### Solución

- Configura límites en `Batching.QueueCapacityByPriority`
- Configura `DeadLetterQueue.MaxSize`
- Revisa limpieza de caches

```json
{
  "StructuredLogging": {
    "Batching": {
      "QueueCapacityByPriority": {
        "Information": 2000,
        "Debug": 1000
      }
    },
    "DeadLetterQueue": {
      "MaxSize": 10000
    }
  }
}
```

## Errores de conexión a sinks externos

### Causas Posibles

1. **URL incorrecta**: Verifica la URL del sink
2. **Autenticación fallida**: Verifica credenciales
3. **Timeout**: El sink tarda demasiado en responder

### Solución

- Verifica la configuración del sink
- Revisa los logs de errores
- Aumenta el timeout si es necesario
- Verifica que el servicio externo esté disponible

## Los logs no se correlacionan

### Causas Posibles

1. **Correlación deshabilitada**: Verifica `Correlation.EnableCorrelationId`
2. **Headers no configurados**: Verifica nombres de headers
3. **Headers no propagados**: Verifica que los headers se propaguen entre servicios

### Solución

```json
{
  "StructuredLogging": {
    "Correlation": {
      "EnableCorrelationId": true,
      "CorrelationIdHeader": "X-Correlation-ID"
    }
  }
}
```

---

**Siguiente:** [Diagnóstico](diagnostics.md)

