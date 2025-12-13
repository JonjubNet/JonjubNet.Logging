# Configuración de Seguridad Avanzada - JonjubNet.Logging

## Encriptación en Tránsito

Configura TLS/SSL para todos los sinks HTTP (HTTP, Elasticsearch, Kafka REST Proxy).

```json
{
  "StructuredLogging": {
    "Security": {
      "EncryptionInTransit": {
        "Enabled": true,
        "RequireTls": true,
        "MinimumTlsVersion": "1.2",
        "ClientCertificatePath": null,
        "ClientCertificatePassword": null,
        "ValidateServerCertificate": true,
        "CustomRootCertificatesPath": null
      }
    }
  }
}
```

### Opciones de Encriptación en Tránsito

- **Enabled**: Habilitar/deshabilitar encriptación en tránsito
- **RequireTls**: Forzar uso de HTTPS (rechaza conexiones HTTP)
- **MinimumTlsVersion**: Versión mínima de TLS (1.0, 1.1, 1.2, 1.3)
- **ClientCertificatePath**: Ruta al certificado cliente (para autenticación mutua)
- **ClientCertificatePassword**: Contraseña del certificado cliente
- **ValidateServerCertificate**: Validar certificado del servidor
- **CustomRootCertificatesPath**: Ruta a certificados raíz personalizados

## Encriptación en Reposo

Encripta archivos de log para el File sink.

```json
{
  "StructuredLogging": {
    "Security": {
      "EncryptionAtRest": {
        "Enabled": true,
        "EncryptionAlgorithm": "AES256",
        "EncryptionKeyPath": "keys/encryption.key",
        "EncryptionKeyPassword": null,
        "EnableKeyRotation": true,
        "KeyRotationIntervalDays": 90,
        "PreviousKeysPath": "keys/previous"
      }
    }
  }
}
```

### Opciones de Encriptación en Reposo

- **Enabled**: Habilitar/deshabilitar encriptación en reposo
- **EncryptionAlgorithm**: Algoritmo (AES256, AES128)
- **EncryptionKeyPath**: Ruta al archivo de clave de encriptación
- **EncryptionKeyPassword**: Contraseña para desbloquear la clave
- **EnableKeyRotation**: Habilitar rotación automática de claves
- **KeyRotationIntervalDays**: Intervalo de rotación (días)
- **PreviousKeysPath**: Ruta donde se almacenan claves anteriores (para descifrar logs antiguos)

## Audit Logging

Registra cambios de configuración y accesos a logs sensibles.

```json
{
  "StructuredLogging": {
    "Security": {
      "Audit": {
        "Enabled": true,
        "LogConfigurationChanges": true,
        "LogSensitiveAccess": true,
        "SensitiveCategories": ["Security", "Audit"],
        "SensitiveLevels": ["Error", "Critical"],
        "EnableComplianceTracking": false,
        "ComplianceStandards": ["GDPR", "HIPAA"],
        "AuditLogPath": "logs/audit.log"
      }
    }
  }
}
```

### Opciones de Audit Logging

- **Enabled**: Habilitar/deshabilitar audit logging
- **LogConfigurationChanges**: Registrar cambios de configuración
- **LogSensitiveAccess**: Registrar accesos a logs sensibles
- **SensitiveCategories**: Categorías consideradas sensibles
- **SensitiveLevels**: Niveles considerados sensibles
- **EnableComplianceTracking**: Habilitar rastreo de cumplimiento
- **ComplianceStandards**: Estándares a rastrear (GDPR, HIPAA, PCI-DSS, etc.)
- **AuditLogPath**: Ruta para logs de auditoría (opcional, usa sink principal si es null)

## Ejemplo Completo

```json
{
  "StructuredLogging": {
    "Security": {
      "EncryptionInTransit": {
        "Enabled": true,
        "RequireTls": true,
        "MinimumTlsVersion": "1.2",
        "ValidateServerCertificate": true
      },
      "EncryptionAtRest": {
        "Enabled": true,
        "EncryptionAlgorithm": "AES256",
        "EncryptionKeyPath": "keys/encryption.key",
        "EnableKeyRotation": true,
        "KeyRotationIntervalDays": 90
      },
      "Audit": {
        "Enabled": true,
        "LogConfigurationChanges": true,
        "LogSensitiveAccess": true,
        "EnableComplianceTracking": true,
        "ComplianceStandards": ["GDPR", "HIPAA"]
      }
    }
  }
}
```

---

**Anterior:** [Batching](batching.md)  
**Siguiente:** [Resiliencia](resilience.md)

