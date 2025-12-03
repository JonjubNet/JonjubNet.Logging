# Script para construir y empaquetar JonjubNet.Logging
param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipTests = $false
)

Write-Host "Construyendo JonjubNet.Logging v$Version" -ForegroundColor Green

# Limpiar directorios anteriores
Write-Host "Limpiando directorios anteriores..." -ForegroundColor Yellow
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }

# Restaurar paquetes NuGet
Write-Host "Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al restaurar paquetes NuGet" -ForegroundColor Red
    exit 1
}

# Compilar el proyecto
Write-Host "Compilando proyecto..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al compilar el proyecto" -ForegroundColor Red
    exit 1
}

# Ejecutar pruebas (si no se omite)
if (-not $SkipTests) {
    Write-Host "Ejecutando pruebas..." -ForegroundColor Yellow
    # Aquí podrías agregar pruebas si las tienes
    # dotnet test --configuration $Configuration --no-build
}

# Empaquetar
Write-Host "Empaquetando NuGet..." -ForegroundColor Yellow
dotnet pack --configuration $Configuration --no-build --output ./packages

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al empaquetar" -ForegroundColor Red
    exit 1
}

Write-Host "Paquete NuGet creado exitosamente en ./packages/" -ForegroundColor Green

# Mostrar información del paquete
$packageFile = Get-ChildItem -Path "./packages" -Filter "*.nupkg" | Select-Object -First 1
if ($packageFile) {
    Write-Host "Archivo del paquete: $($packageFile.FullName)" -ForegroundColor Cyan
    Write-Host "Tamaño: $([math]::Round($packageFile.Length / 1KB, 2)) KB" -ForegroundColor Cyan
}

Write-Host "`nPara publicar el paquete, ejecuta:" -ForegroundColor Yellow
Write-Host "dotnet nuget push ./packages/JonjubNet.Logging.$Version.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor White
