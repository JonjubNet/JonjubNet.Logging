# Configuración de Sinks - JonjubNet.Logging

Los sinks son los destinos donde se envían los logs. Puedes configurar múltiples sinks simultáneamente.

## Console Sink

El sink más simple, envía logs a la consola.

```json
{
  "StructuredLogging": {
    "Sinks": {
      "EnableConsole": true
    }
  }
}
```

## File Sink

Envía logs a archivos con rolling automático.

```json
{
  "StructuredLogging": {
    "Sinks": {
      "EnableFile": true,
      "File": {
        "Path": "logs/log-.txt",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 30,
        "FileSizeLimitBytes": 104857600,
        "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
      }
    }
  }
}
```

### Opciones de File Sink

- **Path**: Ruta del archivo (usa `-` para rolling, ej: `logs/log-.txt`)
- **RollingInterval**: `Day`, `Hour`, `Minute`
- **RetainedFileCountLimit**: Número máximo de archivos a retener
- **FileSizeLimitBytes**: Tamaño máximo por archivo (en bytes)
- **OutputTemplate**: Plantilla de formato (opcional)

## HTTP Sink

Envía logs a un endpoint HTTP.

```json
{
  "StructuredLogging": {
    "Sinks": {
      "EnableHttp": true,
      "Http": {
        "Url": "https://mi-servidor-logs.com/api/logs",
        "ContentType": "application/json",
        "BatchPostingLimit": 100,
        "PeriodSeconds": 2,
        "Headers": {
          "Authorization": "Bearer token123"
        }
      }
    }
  }
}
```

### Opciones de HTTP Sink

- **Url**: URL del endpoint
- **ContentType**: Tipo de contenido (default: `application/json`)
- **BatchPostingLimit**: Logs por batch
- **PeriodSeconds**: Intervalo entre batches
- **Headers**: Headers HTTP adicionales

## Elasticsearch Sink

Envía logs a Elasticsearch.

```json
{
  "StructuredLogging": {
    "Sinks": {
      "EnableElasticsearch": true,
      "Elasticsearch": {
        "Url": "http://localhost:9200",
        "IndexFormat": "logs-{0:yyyy.MM.dd}",
        "EnableAuthentication": true,
        "Username": "elastic",
        "Password": "password"
      }
    }
  }
}
```

### Opciones de Elasticsearch Sink

- **Url**: URL de Elasticsearch
- **IndexFormat**: Formato del índice (usa `{0:yyyy.MM.dd}` para fecha)
- **EnableAuthentication**: Habilitar autenticación
- **Username**: Usuario (si EnableAuthentication = true)
- **Password**: Contraseña (si EnableAuthentication = true)

## Kafka Producer

Envía logs a Kafka (nativo, REST Proxy o Webhook).

```json
{
  "StructuredLogging": {
    "KafkaProducer": {
      "Enabled": true,
      "BootstrapServers": "localhost:9092",
      "Topic": "structured-logs",
      "TimeoutSeconds": 5,
      "BatchSize": 100,
      "LingerMs": 5,
      "RetryCount": 3,
      "EnableCompression": true,
      "CompressionType": "gzip"
    }
  }
}
```

### Opciones de Conexión Kafka

1. **Conexión Nativa**: Usa `BootstrapServers` (ej: "localhost:9092")
2. **REST Proxy**: Usa `ProducerUrl` con `UseWebhook: false`
3. **Webhook HTTP**: Usa `ProducerUrl` con `UseWebhook: true`

## Múltiples Sinks

Puedes habilitar múltiples sinks simultáneamente:

```json
{
  "StructuredLogging": {
    "Sinks": {
      "EnableConsole": true,
      "EnableFile": true,
      "EnableHttp": true,
      "EnableElasticsearch": true
    },
    "KafkaProducer": {
      "Enabled": true
    }
  }
}
```

---

**Anterior:** [Configuración Principal](main.md)  
**Siguiente:** [Filtros y Sampling](filters-sampling.md)

