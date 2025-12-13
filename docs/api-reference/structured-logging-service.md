# IStructuredLoggingService - API Reference

## Métodos de Logging Básicos

### LogInformation

Registra un log de nivel Information.

```csharp
void LogInformation(
    string message,
    string operation = "",
    string category = "",
    Dictionary<string, object>? properties = default,
    Dictionary<string, object>? context = default
);
```

### LogWarning

Registra un log de nivel Warning.

```csharp
void LogWarning(
    string message,
    string operation = "",
    string category = "",
    Dictionary<string, object>? properties = default,
    Dictionary<string, object>? context = default,
    Exception? exception = null
);
```

### LogError

Registra un log de nivel Error.

```csharp
void LogError(
    string message,
    string operation = "",
    string category = "",
    Dictionary<string, object>? properties = default,
    Dictionary<string, object>? context = default,
    Exception? exception = null
);
```

### LogCritical

Registra un log de nivel Critical.

```csharp
void LogCritical(
    string message,
    string operation = "",
    string category = "",
    Dictionary<string, object>? properties = default,
    Dictionary<string, object>? context = default,
    Exception? exception = null
);
```

### LogDebug

Registra un log de nivel Debug.

```csharp
void LogDebug(
    string message,
    string operation = "",
    string category = "",
    Dictionary<string, object>? properties = default,
    Dictionary<string, object>? context = default
);
```

### LogTrace

Registra un log de nivel Trace.

```csharp
void LogTrace(
    string message,
    string operation = "",
    string category = "",
    Dictionary<string, object>? properties = default,
    Dictionary<string, object>? context = default
);
```

## Métodos Especializados

### LogOperationStart

Registra el inicio de una operación.

```csharp
void LogOperationStart(
    string operation,
    string category = "",
    Dictionary<string, object>? properties = default
);
```

### LogOperationEnd

Registra el fin de una operación.

```csharp
void LogOperationEnd(
    string operation,
    string category = "",
    long executionTimeMs = 0,
    Dictionary<string, object>? properties = default,
    bool success = true,
    Exception? exception = null
);
```

### LogUserAction

Registra una acción de usuario.

```csharp
void LogUserAction(
    string action,
    string entityType = "",
    string entityId = "",
    Dictionary<string, object>? properties = default
);
```

### LogSecurityEvent

Registra un evento de seguridad.

```csharp
void LogSecurityEvent(
    string eventType,
    string description,
    Dictionary<string, object>? properties = default,
    Exception? exception = null
);
```

### LogAuditEvent

Registra un evento de auditoría.

```csharp
void LogAuditEvent(
    string eventType,
    string description,
    string entityType = "",
    string entityId = "",
    Dictionary<string, object>? properties = default
);
```

## Scopes

### BeginScope

Crea un scope que agrega propiedades a todos los logs dentro del scope.

```csharp
// Con múltiples propiedades
ILogScope BeginScope(Dictionary<string, object> properties);

// Con una propiedad
ILogScope BeginScope(string key, object value);
```

**Ejemplo:**

```csharp
using var scope = _loggingService.BeginScope(new Dictionary<string, object>
{
    ["RequestId"] = requestId,
    ["UserId"] = userId
});

// Todos los logs dentro de este scope incluirán RequestId y UserId
_loggingService.LogInformation("Procesando request");
```

---

**Siguiente:** [ILoggingConfigurationManager](configuration-manager.md)

