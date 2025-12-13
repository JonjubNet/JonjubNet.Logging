# Ejemplos de Uso Básico - JonjubNet.Logging

## Uso Básico

```csharp
public class OrderService
{
    private readonly IStructuredLoggingService _loggingService;

    public OrderService(IStructuredLoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        _loggingService.LogInformation(
            "Iniciando creación de orden",
            operation: "CreateOrder",
            category: "Order"
        );

        try
        {
            var order = await ProcessOrderAsync(request);
            
            _loggingService.LogInformation(
                "Orden creada exitosamente",
                operation: "CreateOrder",
                category: "Order",
                properties: new Dictionary<string, object>
                {
                    ["OrderId"] = order.Id,
                    ["Total"] = order.Total
                }
            );

            return order;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(
                "Error al crear orden",
                operation: "CreateOrder",
                category: "Order",
                exception: ex,
                properties: new Dictionary<string, object>
                {
                    ["Request"] = request
                }
            );
            throw;
        }
    }
}
```

## Diferentes Niveles de Log

```csharp
// Trace - Información muy detallada
_loggingService.LogTrace("Detalle de ejecución", operation: "ProcessData");

// Debug - Información de depuración
_loggingService.LogDebug("Variable value", operation: "Debug", 
    properties: new Dictionary<string, object> { ["Value"] = value });

// Information - Eventos importantes
_loggingService.LogInformation("Operación completada", operation: "ProcessOrder");

// Warning - Situaciones anómalas
_loggingService.LogWarning("Valor fuera de rango", operation: "Validate",
    properties: new Dictionary<string, object> { ["Value"] = value });

// Error - Errores que requieren atención
_loggingService.LogError("Error al procesar", operation: "Process", exception: ex);

// Critical - Errores críticos
_loggingService.LogCritical("Sistema no disponible", operation: "SystemCheck", exception: ex);
```

## Logging con Propiedades

```csharp
_loggingService.LogInformation(
    "Usuario autenticado",
    operation: "Login",
    category: "Security",
    properties: new Dictionary<string, object>
    {
        ["UserId"] = user.Id,
        ["Username"] = user.Username,
        ["IpAddress"] = ipAddress,
        ["Timestamp"] = DateTime.UtcNow
    }
);
```

## Logging con Excepciones

```csharp
try
{
    // Tu código aquí
}
catch (Exception ex)
{
    _loggingService.LogError(
        "Error al procesar petición",
        operation: "ProcessRequest",
        category: "API",
        exception: ex,
        properties: new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["UserId"] = userId
        }
    );
    throw;
}
```

---

**Siguiente:** [Scopes y Contexto](scopes.md)

