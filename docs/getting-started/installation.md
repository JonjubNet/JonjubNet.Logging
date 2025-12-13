# Instalación - JonjubNet.Logging

## Requisitos Previos

- .NET 10.0 o superior
- Visual Studio 2022 o VS Code
- NuGet Package Manager

## Instalación del Paquete

### ⚠️ IMPORTANTE: Instalar Solo el Paquete Principal

**NO intentes instalar los paquetes internos** (`JonjubNet.Logging.Shared`, `JonjubNet.Logging.Domain`, `JonjubNet.Logging.Application`). Estos son proyectos internos que se incluyen automáticamente en el paquete principal.

### Opción 1: CLI de .NET

```bash
dotnet add package JonjubNet.Logging
```

### Opción 2: Package Manager Console

```powershell
Install-Package JonjubNet.Logging
```

### Opción 3: NuGet Package Manager UI

Busca e instala `JonjubNet.Logging` desde el NuGet Package Manager en Visual Studio.

### Opción 4: Archivo .csproj

Agrega manualmente en tu archivo `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="JonjubNet.Logging" Version="3.1.1" />
</ItemGroup>
```

## Verificar Instalación

Después de instalar, verifica que en tu archivo `.csproj` aparezca:

```xml
<ItemGroup>
  <PackageReference Include="JonjubNet.Logging" Version="3.1.1" />
</ItemGroup>
```

**No debe haber referencias a:**
- ❌ `JonjubNet.Logging.Shared`
- ❌ `JonjubNet.Logging.Domain`
- ❌ `JonjubNet.Logging.Application`

Estos se incluyen automáticamente en el paquete principal.

## Verificar que Funciona

Después de instalar, intenta compilar tu proyecto:

```bash
dotnet build
```

Si la compilación es exitosa, la instalación fue correcta.

## Próximos Pasos

1. [Inicio Rápido](quick-start.md) - Primeros pasos en 5 minutos
2. [Configuración](../configuration/main.md) - Configura tu logging

---

**Anterior:** [Introducción](introduction.md)  
**Siguiente:** [Inicio Rápido](quick-start.md)

