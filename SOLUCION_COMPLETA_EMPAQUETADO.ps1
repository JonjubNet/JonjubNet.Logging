# Script para Recompilar y Reempaquetar JonjubNet.Logging Correctamente
# Soluciona el problema de DLLs faltantes en el paquete NuGet

param(
    [string]$Version = "3.0.14",
    [string]$Configuration = "Release"
)

Write-Host "`n🔨 RECOMPILACIÓN Y EMPAQUETADO DE JONJUBNET.LOGGING" -ForegroundColor Cyan
Write-Host "==================================================`n" -ForegroundColor Cyan

# Paso 1: Detener IIS Express si está ejecutándose
Write-Host "📋 Paso 1: Deteniendo IIS Express..." -ForegroundColor Yellow
$iisProcesses = Get-Process -Name "iisexpress" -ErrorAction SilentlyContinue
if ($iisProcesses) {
    Write-Host "   Deteniendo procesos IIS Express..." -ForegroundColor White
    Stop-Process -Name "iisexpress" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "   ✅ IIS Express detenido" -ForegroundColor Green
} else {
    Write-Host "   ℹ️  IIS Express no está ejecutándose" -ForegroundColor Gray
}

# Paso 2: Limpiar completamente
Write-Host "`n📋 Paso 2: Limpiando solución completa..." -ForegroundColor Yellow
$rootPath = $PSScriptRoot
if (-not $rootPath) { $rootPath = Get-Location }

# Limpiar con dotnet clean
Write-Host "   Ejecutando dotnet clean..." -ForegroundColor White
dotnet clean --configuration $Configuration 2>&1 | Out-Null

# Eliminar bin y obj manualmente
Write-Host "   Eliminando directorios bin y obj..." -ForegroundColor White
Get-ChildItem -Path $rootPath -Recurse -Directory -Filter "bin" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path $rootPath -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "   ✅ Limpieza completada" -ForegroundColor Green

# Paso 3: Restaurar paquetes
Write-Host "`n📋 Paso 3: Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Error al restaurar paquetes" -ForegroundColor Red
    exit 1
}
Write-Host "   ✅ Paquetes restaurados" -ForegroundColor Green

# Paso 4: Compilar en Release
Write-Host "`n📋 Paso 4: Compilando en $Configuration..." -ForegroundColor Yellow
Write-Host "   Esto puede tardar unos minutos..." -ForegroundColor White
dotnet build --configuration $Configuration --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Error al compilar" -ForegroundColor Red
    exit 1
}
Write-Host "   ✅ Compilación exitosa" -ForegroundColor Green

# Paso 5: Verificar DLLs
Write-Host "`n📋 Paso 5: Verificando DLLs compiladas..." -ForegroundColor Yellow
$appDll = Join-Path $rootPath "Core\JonjubNet.Logging.Application\bin\$Configuration\net10.0\JonjubNet.Logging.Application.dll"
$domainDll = Join-Path $rootPath "Core\JonjubNet.Logging.Domain\bin\$Configuration\net10.0\JonjubNet.Logging.Domain.dll"
$sharedDll = Join-Path $rootPath "Infrastructure\JonjubNet.Logging.Shared\bin\$Configuration\net10.0\JonjubNet.Logging.Shared.dll"

$allFound = $true
if (Test-Path $appDll) {
    Write-Host "   ✅ Application.dll encontrado" -ForegroundColor Green
} else {
    Write-Host "   ❌ Application.dll NO encontrado en: $appDll" -ForegroundColor Red
    $allFound = $false
}

if (Test-Path $domainDll) {
    Write-Host "   ✅ Domain.dll encontrado" -ForegroundColor Green
} else {
    Write-Host "   ❌ Domain.dll NO encontrado en: $domainDll" -ForegroundColor Red
    $allFound = $false
}

if (Test-Path $sharedDll) {
    Write-Host "   ✅ Shared.dll encontrado" -ForegroundColor Green
} else {
    Write-Host "   ❌ Shared.dll NO encontrado en: $sharedDll" -ForegroundColor Red
    $allFound = $false
}

if (-not $allFound) {
    Write-Host "`n   ⚠️  ALGUNAS DLLs NO SE ENCONTRARON" -ForegroundColor Red
    Write-Host "   Verifica que la compilación haya sido exitosa" -ForegroundColor Yellow
    exit 1
}

# Paso 6: Actualizar versión en .csproj
Write-Host "`n📋 Paso 6: Verificando versión en .csproj..." -ForegroundColor Yellow
$csprojPath = Join-Path $rootPath "Presentation\JonjubNet.Logging\JonjubNet.Logging.csproj"
if (Test-Path $csprojPath) {
    $csprojContent = Get-Content $csprojPath -Raw
    if ($csprojContent -match '<Version>(\d+\.\d+\.\d+)</Version>') {
        $currentVersion = $matches[1]
        Write-Host "   Versión actual: $currentVersion" -ForegroundColor White
        if ($currentVersion -ne $Version) {
            Write-Host "   ⚠️  La versión en .csproj ($currentVersion) no coincide con la solicitada ($Version)" -ForegroundColor Yellow
            Write-Host "   Actualiza manualmente el .csproj si es necesario" -ForegroundColor Yellow
        }
    }
}

# Paso 7: Empaquetar
Write-Host "`n📋 Paso 7: Empaquetando NuGet..." -ForegroundColor Yellow
$packagePath = Join-Path $rootPath "Presentation\JonjubNet.Logging"
Push-Location $packagePath

dotnet pack --configuration $Configuration --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Error al empaquetar" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location
Write-Host "   ✅ Empaquetado exitoso" -ForegroundColor Green

# Paso 8: Verificar contenido del paquete
Write-Host "`n📋 Paso 8: Verificando contenido del paquete..." -ForegroundColor Yellow
$packageFile = Get-ChildItem -Path (Join-Path $rootPath "Presentation\JonjubNet.Logging\bin\$Configuration") -Filter "*.nupkg" | Where-Object { $_.Name -like "*$Version*" } | Select-Object -First 1

if ($packageFile) {
    Write-Host "   Paquete encontrado: $($packageFile.Name)" -ForegroundColor White
    Write-Host "   Tamaño: $([math]::Round($packageFile.Length / 1KB, 2)) KB" -ForegroundColor White
    
    # Verificar contenido (usar 7-Zip si está disponible, o simplemente reportar)
    Write-Host "`n   ⚠️  IMPORTANTE: Verifica manualmente que el .nupkg contenga:" -ForegroundColor Yellow
    Write-Host "      • lib/net10.0/JonjubNet.Logging.dll" -ForegroundColor White
    Write-Host "      • lib/net10.0/JonjubNet.Logging.Application.dll" -ForegroundColor White
    Write-Host "      • lib/net10.0/JonjubNet.Logging.Domain.dll" -ForegroundColor White
    Write-Host "      • lib/net10.0/JonjubNet.Logging.Shared.dll" -ForegroundColor White
    Write-Host "`n   Puedes abrir el .nupkg con 7-Zip o NuGet Package Explorer" -ForegroundColor Gray
} else {
    Write-Host "   ⚠️  No se encontró el paquete con versión $Version" -ForegroundColor Yellow
    Write-Host "   Buscando cualquier paquete .nupkg..." -ForegroundColor White
    $anyPackage = Get-ChildItem -Path (Join-Path $rootPath "Presentation\JonjubNet.Logging\bin\$Configuration") -Filter "*.nupkg" | Select-Object -First 1
    if ($anyPackage) {
        Write-Host "   Paquete encontrado: $($anyPackage.Name)" -ForegroundColor White
    } else {
        Write-Host "   ❌ No se encontró ningún paquete .nupkg" -ForegroundColor Red
    }
}

# Resumen final
Write-Host "`n✅ PROCESO COMPLETADO" -ForegroundColor Green
Write-Host "====================`n" -ForegroundColor Green
Write-Host "📦 Próximos pasos:" -ForegroundColor Cyan
Write-Host "   1. Verifica el contenido del .nupkg manualmente" -ForegroundColor White
Write-Host "   2. Limpia el cache de NuGet en el API:" -ForegroundColor White
Write-Host "      dotnet nuget locals all --clear" -ForegroundColor Gray
Write-Host "   3. Instala el paquete en el API:" -ForegroundColor White
Write-Host "      dotnet add package JonjubNet.Logging --version $Version --source `"$($packageFile.DirectoryName)`"" -ForegroundColor Gray
Write-Host "`n📄 Ubicación del paquete:" -ForegroundColor Cyan
if ($packageFile) {
    Write-Host "   $($packageFile.FullName)" -ForegroundColor White
}

