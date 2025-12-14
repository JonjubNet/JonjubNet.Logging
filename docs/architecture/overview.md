# Arquitectura General - JonjubNet.Logging

## Clean Architecture

El componente está implementado siguiendo **Clean Architecture** con separación clara de responsabilidades:

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  presentation/JonjubNet.Logging                      │  │
│  │  - ServiceExtensions (Punto de entrada)              │  │
│  │  - Registro de servicios                             │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE LAYER                       │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  infrastructure/JonjubNet.Logging.Shared            │  │
│  │  - Implementaciones concretas de servicios           │  │
│  │  - Sinks (Console, File, HTTP, Elasticsearch, Kafka)  │  │
│  │  - Circuit Breakers, Retry Policies, DLQ             │  │
│  │  - Dependencias externas (Serilog, Kafka, HTTP)      │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  APPLICATION LAYER                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  core/JonjubNet.Logging.Application                  │  │
│  │  - Interfaces (IStructuredLoggingService, ILogSink)  │  │
│  │  - Use Cases (Create, Enrich, Send)                 │  │
│  │  - Behaviours (LoggingBehaviour para MediatR)       │  │
│  │  - Configuration (LoggingConfiguration)              │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    DOMAIN LAYER                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  core/JonjubNet.Logging.Domain                        │  │
│  │  - Entities (StructuredLogEntry)                      │  │
│  │  - Value Objects (LogLevel, LogCategory, EventType)   │  │
│  │  - Helpers (DictionaryPool, GCOptimizationHelpers)     │  │
│  │  - ZERO dependencias externas                         │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Dependency Rule

Las dependencias apuntan **hacia adentro**:

- ✅ **Domain**: 0 dependencias externas (solo Microsoft.Extensions.ObjectPool)
- ✅ **Application**: Solo Domain + abstracciones de .NET
- ✅ **infrastructure**: Application + Domain + dependencias externas
- ✅ **presentation**: Application + Infrastructure

## Principios Aplicados

### Dependency Inversion
Las capas superiores dependen de abstracciones, no de implementaciones concretas.

### Separation of Concerns
Cada capa tiene una responsabilidad clara y bien definida.

### Single Responsibility
Cada clase tiene una única responsabilidad y razón para cambiar.

### Open/Closed
Abierto para extensión (puedes agregar nuevos sinks), cerrado para modificación.

### Interface Segregation
Interfaces específicas y cohesivas, no interfaces genéricas y grandes.

## Flujo de Dependencias

```
presentation/JonjubNet.Logging
    ↓
infrastructure/JonjubNet.Logging.Shared
    ↓
core/JonjubNet.Logging.Application
    ↓
core/JonjubNet.Logging.Domain
```

**Todas las dependencias apuntan hacia adentro (Clean Architecture)**

## Beneficios de esta Arquitectura

1. **Testabilidad**: Fácil de testear cada capa de forma independiente
2. **Mantenibilidad**: Cambios en una capa no afectan otras
3. **Extensibilidad**: Fácil agregar nuevos sinks o funcionalidades
4. **Independencia**: Domain no depende de nada externo
5. **Claridad**: Cada capa tiene un propósito claro

## Próximos Pasos

- [Estructura de Componentes](components.md) - Detalles de cada capa
- [Flujo de Datos](data-flow.md) - Cómo fluyen los logs

---

**Siguiente:** [Estructura de Componentes](components.md)

