# Tests de Performance/Benchmarking

Este proyecto contiene benchmarks de performance para el componente `JonjubNet.Logging` usando [BenchmarkDotNet](https://benchmarkdotnet.org/).

## 📋 Benchmarks Implementados

### 1. **JsonSerializationBenchmark**
Compara diferentes métodos de serialización JSON:
- `ToJson()` - Método estándar (baseline)
- `JsonSerializationHelper.SerializeToJson()` - Optimizado con Span/Memory
- `JsonSerializationHelper.SerializeToUtf8Bytes()` - Serialización a bytes UTF-8

**Métricas medidas:**
- Tiempo de ejecución
- Allocations de memoria
- Throughput

### 2. **DataSanitizationBenchmark**
Mide el rendimiento de la sanitización de datos:
- `DataSanitizationService.Sanitize()` - Sanitización general
- `LogDataSanitizationService.Sanitize()` - Sanitización específica de logs

**Métricas medidas:**
- Tiempo de sanitización
- Allocations durante el proceso
- Impacto de patrones regex compilados

### 3. **LogEntryCloningBenchmark**
Compara diferentes métodos de clonado de log entries:
- `CloneViaJsonSerialization()` - Método antiguo (baseline)
- `CloneViaManualCloning()` - Método optimizado actual
- `CloneViaSanitizationService()` - Clonado a través del servicio

**Métricas medidas:**
- Tiempo de clonado
- Reducción de allocations
- Mejora de rendimiento vs método antiguo

### 4. **LogEntryCreationBenchmark**
Mide el rendimiento de creación de log entries:
- `CreateBasicLogEntry()` - Log entry básico (baseline)
- `CreateLogEntryWithProperties()` - Con propiedades
- `CreateFullLogEntry()` - Log entry completo

**Métricas medidas:**
- Tiempo de creación
- Allocations por tipo de log entry

## 🚀 Ejecución

### Ejecutar todos los benchmarks:
```bash
cd tests/JonjubNet.Logging.Performance.Tests
dotnet run -c Release
```

### Ejecutar un benchmark específico:
```bash
dotnet run -c Release -- --filter "*JsonSerializationBenchmark*"
dotnet run -c Release -- --filter "*DataSanitizationBenchmark*"
dotnet run -c Release -- --filter "*LogEntryCloningBenchmark*"
dotnet run -c Release -- --filter "*LogEntryCreationBenchmark*"
```

### Ejecutar con opciones personalizadas:
```bash
# Solo medir tiempo (sin allocations)
dotnet run -c Release -- --filter "*" --job Dry

# Exportar resultados a Markdown
dotnet run -c Release -- --filter "*" --exporters markdown
```

## 📊 Resultados

Los resultados se generan en la carpeta `BenchmarkDotNet.Artifacts/results/` con:
- Reportes en Markdown
- Reportes en HTML
- Reportes en CSV
- Gráficos de comparación

## 📈 Métricas Esperadas

Basado en las optimizaciones implementadas:

### Serialización JSON:
- **JsonSerializationHelper**: ~5-10% menos allocations que `ToJson()`
- **Throughput**: Mejora del 5-10% en serialización

### Clonado de Log Entries:
- **Clonado manual**: ~80-90% más rápido que serialización JSON
- **Allocations**: ~70-80% menos que método antiguo

### Sanitización:
- **Tiempo**: ~0.35-1.5ms por log entry
- **Allocations**: Optimizado con DictionaryPool

## 🔧 Configuración

Los benchmarks están configurados con:
- **Runtime**: .NET 10.0
- **Diagnóstico de memoria**: Habilitado (`[MemoryDiagnoser]`)
- **Exportador Markdown**: Habilitado para reportes

## 📝 Notas

- Los benchmarks deben ejecutarse en modo **Release** para obtener resultados precisos
- Se recomienda ejecutar en un entorno limpio sin otras aplicaciones pesadas
- Los resultados pueden variar según el hardware y la carga del sistema

