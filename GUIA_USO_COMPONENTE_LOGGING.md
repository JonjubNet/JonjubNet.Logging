# Guía de Uso del Componente JonjubNet.Logging

## 📋 Análisis de Errores y Soluciones

### 🔍 Errores Identificados

1. **Archivos de metadatos faltantes** - Proyectos no compilados
2. **`JonjubNet.Logging.Interfaces` no existe** - Namespace incorrecto
3. **`JonjubNet.Logging.Extensions` no existe** - Namespace incorrecto
4. **`IErrorCategorizationService` no encontrado** - Using incorrecto
5. **`ICurrentUserService` no encontrado** - Using incorrecto

---

## ✅ Namespaces Correctos

### **Interfaces y Servicios**

**❌ INCORRECTO:**
```csharp
using JonjubNet.Logging.Interfaces;  // ❌ NO EXISTE
```

**✅ CORRECTO:**
```csharp
using JonjubNet.Logging.Application.Interfaces;  // ✅ CORRECTO
```

### **Extensiones de Servicios**

**❌ INCORRECTO:**
```csharp
using JonjubNet.Logging.Extensions;  // ❌ NO EXISTE
```

**✅ CORRECTO:**
```csharp
using JonjubNet.Logging.Shared;  // ✅ CORRECTO (para ServiceExtensions)
```

---

## 📦 Referencia al Componente

### Opción 1: Referencia como Paquete NuGet (Recomendado)

**En el archivo `.csproj` de tu API:**
```xml
<ItemGroup>
  <PackageReference Include="JonjubNet.Logging" Version="3.0.1" />
</ItemGroup>
```

### Opción 2: Referencia como Proyecto

**En el archivo `.csproj` de tu API:**
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Components\JonjubNet.Logging\Presentation\JonjubNet.Logging\JonjubNet.Logging.csproj" />
</ItemGroup>
```

---

## 🔧 Configuración en tu API

### 1. En `Program.cs` o `Startup.cs`

```csharp
using JonjubNet.Logging.Shared;  // ✅ Para ServiceExtensions
using JonjubNet.Logging.Application.Interfaces;  // ✅ Para interfaces

var builder = WebApplication.CreateBuilder(args);

// Registrar servicios de logging
builder.Services.AddSharedInfrastructure(builder.Configuration);

// O con servicio de usuario personalizado:
// builder.Services.AddSharedInfrastructure<MiCurrentUserService>(builder.Configuration);

var app = builder.Build();
```

### 2. Usar el Servicio de Logging

```csharp
using JonjubNet.Logging.Application.Interfaces;  // ✅ Namespace correcto

public class MiController : ControllerBase
{
    private readonly IStructuredLoggingService _loggingService;
    
    public MiController(IStructuredLoggingService loggingService)
    {
        _loggingService = loggingService;
    }
    
    [HttpGet]
    public IActionResult Get()
    {
        _loggingService.LogInformation("Operación ejecutada", "GetItems");
        return Ok();
    }
}
```

### 3. Usar Interfaces Específicas

```csharp
using JonjubNet.Logging.Application.Interfaces;  // ✅ Namespace correcto

public class MiServicio
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IErrorCategorizationService _errorCategorizationService;
    
    public MiServicio(
        ICurrentUserService currentUserService,
        IErrorCategorizationService errorCategorizationService)
    {
        _currentUserService = currentUserService;
        _errorCategorizationService = errorCategorizationService;
    }
}
```

---

## 📝 Using Statements Completos

### Para Controllers/Services que usan Logging

```csharp
using JonjubNet.Logging.Application.Interfaces;
using JonjubNet.Logging.Domain.Entities;
using JonjubNet.Logging.Domain.ValueObjects;
```

### Para Configuración/Startup

```csharp
using JonjubNet.Logging.Shared;  // Para ServiceExtensions
using JonjubNet.Logging.Application.Configuration;
```

---

## 🏗️ Orden de Compilación

Los errores de "archivos de metadatos faltantes" indican que los proyectos no se han compilado en el orden correcto.

### Script de Compilación (PowerShell)

Ejecuta en la raíz de tu solución CatalogMaster:

```powershell
# 1. Limpiar todo
Write-Host "🧹 Limpiando..." -ForegroundColor Yellow
Get-ChildItem -Path . -Recurse -Directory -Filter "bin" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path . -Recurse -Directory -Filter "obj" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# 2. Compilar componente JonjubNet.Logging PRIMERO
Write-Host "🔨 Compilando JonjubNet.Logging..." -ForegroundColor Yellow
cd "D:\Onuar\Proyecto\Net Core\JonjubNet\Component\JonjubNet.Logging"
dotnet restore
dotnet build --no-incremental

# 3. Volver a CatalogMaster y compilar
Write-Host "🔨 Compilando CatalogMaster..." -ForegroundColor Yellow
cd "D:\Onuar\Proyecto\Net Core\JonjubNet\Sevices\CatalogMaster"
dotnet restore
dotnet build --no-incremental

Write-Host "✅ Compilación completada" -ForegroundColor Green
```

---

## 🔍 Verificación de Referencias

### Verificar que el Paquete/Proyecto está Referenciado

**En Visual Studio:**
1. Click derecho en el proyecto de tu API
2. **Manage NuGet Packages** o **Add → Project Reference**
3. Verifica que `JonjubNet.Logging` esté listado

**Desde línea de comandos:**
```powershell
dotnet list package  # Ver paquetes NuGet
dotnet list reference  # Ver referencias de proyecto
```

---

## 📋 Checklist de Implementación

### ✅ Paso 1: Referencia al Componente
- [ ] Agregada referencia a `JonjubNet.Logging` (NuGet o ProjectReference)
- [ ] Versión correcta (3.0.1 o superior)

### ✅ Paso 2: Using Statements Correctos
- [ ] `using JonjubNet.Logging.Application.Interfaces;` (para interfaces)
- [ ] `using JonjubNet.Logging.Shared;` (para extensiones)
- [ ] Eliminados `using JonjubNet.Logging.Interfaces;` (incorrecto)
- [ ] Eliminados `using JonjubNet.Logging.Extensions;` (incorrecto)

### ✅ Paso 3: Registro de Servicios
- [ ] `builder.Services.AddSharedInfrastructure(builder.Configuration);` en `Program.cs`
- [ ] O `services.AddSharedInfrastructure<TUserService>(configuration);` si usas servicio personalizado

### ✅ Paso 4: Compilación
- [ ] Componente `JonjubNet.Logging` compilado primero
- [ ] Proyectos de CatalogMaster compilados después
- [ ] Sin errores de compilación

### ✅ Paso 5: Inyección de Dependencias
- [ ] `IStructuredLoggingService` inyectado en constructores
- [ ] `ICurrentUserService` inyectado si se usa
- [ ] `IErrorCategorizationService` inyectado si se usa

---

## 🎯 Ejemplo Completo de Implementación

### `Program.cs`

```csharp
using JonjubNet.Logging.Shared;
using JonjubNet.Logging.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Registrar logging estructurado
builder.Services.AddSharedInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### `MiController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using JonjubNet.Logging.Application.Interfaces;  // ✅ Namespace correcto

namespace MiAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MiController : ControllerBase
    {
        private readonly IStructuredLoggingService _loggingService;
        
        public MiController(IStructuredLoggingService loggingService)
        {
            _loggingService = loggingService;
        }
        
        [HttpGet]
        public IActionResult Get()
        {
            _loggingService.LogInformation("Obteniendo items", "GetItems");
            return Ok(new { message = "Success" });
        }
    }
}
```

### `MiServicio.cs`

```csharp
using JonjubNet.Logging.Application.Interfaces;  // ✅ Namespace correcto

namespace MiAPI.Services
{
    public class MiServicio
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IErrorCategorizationService _errorCategorizationService;
        
        public MiServicio(
            ICurrentUserService currentUserService,
            IErrorCategorizationService errorCategorizationService)
        {
            _currentUserService = currentUserService;
            _errorCategorizationService = errorCategorizationService;
        }
        
        public void MiMetodo()
        {
            var userId = _currentUserService.GetCurrentUserId();
            // Usar servicios...
        }
    }
}
```

---

## ⚠️ Errores Comunes y Soluciones

### Error: "El tipo o el nombre del espacio de nombres 'Interfaces' no existe"

**Causa:** Using statement incorrecto

**Solución:**
```csharp
// ❌ INCORRECTO
using JonjubNet.Logging.Interfaces;

// ✅ CORRECTO
using JonjubNet.Logging.Application.Interfaces;
```

### Error: "El tipo o el nombre del espacio de nombres 'Extensions' no existe"

**Causa:** Using statement incorrecto

**Solución:**
```csharp
// ❌ INCORRECTO
using JonjubNet.Logging.Extensions;

// ✅ CORRECTO
using JonjubNet.Logging.Shared;  // Para ServiceExtensions
```

### Error: "No se encontró el archivo de metadatos"

**Causa:** Proyectos no compilados o compilados en orden incorrecto

**Solución:**
1. Compilar `JonjubNet.Logging` primero
2. Luego compilar los proyectos que lo referencian
3. Usar `dotnet build --no-incremental` para forzar recompilación completa

---

## 📚 Referencias de Namespaces Disponibles

### Namespaces Principales

| Namespace | Descripción | Uso |
|-----------|-------------|-----|
| `JonjubNet.Logging.Application.Interfaces` | Todas las interfaces | ✅ Usar para inyección de dependencias |
| `JonjubNet.Logging.Shared` | Extensiones de servicios | ✅ Usar para `AddSharedInfrastructure()` |
| `JonjubNet.Logging.Domain.Entities` | Entidades del dominio | ✅ Usar para `StructuredLogEntry` |
| `JonjubNet.Logging.Domain.ValueObjects` | Value objects | ✅ Usar para `LogLevelValue`, `EventTypeValue`, etc. |
| `JonjubNet.Logging.Application.Configuration` | Configuración | ✅ Usar para `LoggingConfiguration` |

### Interfaces Disponibles

- ✅ `IStructuredLoggingService` - Servicio principal de logging
- ✅ `ICurrentUserService` - Servicio de usuario actual
- ✅ `IErrorCategorizationService` - Categorización de errores
- ✅ `ILoggingConfigurationManager` - Gestor de configuración
- ✅ `ILogScopeManager` - Gestor de scopes
- ✅ `ILogSink` - Sinks de logging
- ✅ `IKafkaProducer` - Productor de Kafka
- ✅ Y más...

---

## 🚀 Próximos Pasos

1. **Corregir using statements** en todos los archivos de CatalogMaster
2. **Compilar el componente** JonjubNet.Logging primero
3. **Compilar CatalogMaster** después
4. **Verificar** que no haya errores de compilación

---

**Última actualización:** Diciembre 2024 (v3.0.1)

