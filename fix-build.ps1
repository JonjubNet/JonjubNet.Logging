# Script para solucionar error de compilación en Visual Studio
# Ejecutar desde la raíz del proyecto

Write-Host "🧹 Limpiando carpetas bin y obj..." -ForegroundColor Yellow

# Eliminar carpetas bin y obj
Get-ChildItem -Path . -Recurse -Directory -Filter "bin" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path . -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "✅ Carpetas eliminadas" -ForegroundColor Green

Write-Host "📦 Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore

Write-Host "🔨 Compilando proyecto..." -ForegroundColor Yellow
dotnet build --no-incremental

Write-Host "`n🔍 Verificando archivo de referencia..." -ForegroundColor Yellow
$refFile = "Infrastructure\JonjubNet.Logging.Shared\obj\Debug\net10.0\ref\JonjubNet.Logging.Shared.dll"
if (Test-Path $refFile) {
    Write-Host "✅ Archivo de referencia generado correctamente" -ForegroundColor Green
    Write-Host "   Ubicación: $refFile" -ForegroundColor Gray
} else {
    Write-Host "❌ Archivo de referencia NO encontrado" -ForegroundColor Red
    Write-Host "   Revisa errores de compilación arriba" -ForegroundColor Yellow
}

Write-Host "`n📝 Próximos pasos:" -ForegroundColor Cyan
Write-Host "   1. Cierra Visual Studio completamente" -ForegroundColor White
Write-Host "   2. Abre Visual Studio nuevamente" -ForegroundColor White
Write-Host "   3. Build → Clean Solution" -ForegroundColor White
Write-Host "   4. Build → Rebuild Solution" -ForegroundColor White

