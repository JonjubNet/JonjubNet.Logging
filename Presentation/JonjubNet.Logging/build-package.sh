#!/bin/bash

# Script para construir y empaquetar JonjubNet.Logging
VERSION="1.0.0"
CONFIGURATION="Release"
SKIP_TESTS=false

# Parsear argumentos
while [[ $# -gt 0 ]]; do
    case $1 in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        *)
            echo "Argumento desconocido: $1"
            exit 1
            ;;
    esac
done

echo "Construyendo JonjubNet.Logging v$VERSION"

# Limpiar directorios anteriores
echo "Limpiando directorios anteriores..."
rm -rf bin obj

# Restaurar paquetes NuGet
echo "Restaurando paquetes NuGet..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "Error al restaurar paquetes NuGet"
    exit 1
fi

# Compilar el proyecto
echo "Compilando proyecto..."
dotnet build --configuration $CONFIGURATION --no-restore

if [ $? -ne 0 ]; then
    echo "Error al compilar el proyecto"
    exit 1
fi

# Ejecutar pruebas (si no se omite)
if [ "$SKIP_TESTS" = false ]; then
    echo "Ejecutando pruebas..."
    # Aquí podrías agregar pruebas si las tienes
    # dotnet test --configuration $CONFIGURATION --no-build
fi

# Empaquetar
echo "Empaquetando NuGet..."
dotnet pack --configuration $CONFIGURATION --no-build --output ./packages

if [ $? -ne 0 ]; then
    echo "Error al empaquetar"
    exit 1
fi

echo "Paquete NuGet creado exitosamente en ./packages/"

# Mostrar información del paquete
PACKAGE_FILE=$(find ./packages -name "*.nupkg" | head -1)
if [ -n "$PACKAGE_FILE" ]; then
    echo "Archivo del paquete: $PACKAGE_FILE"
    echo "Tamaño: $(du -h "$PACKAGE_FILE" | cut -f1)"
fi

echo ""
echo "Para publicar el paquete, ejecuta:"
echo "dotnet nuget push ./packages/JonjubNet.Logging.$VERSION.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
