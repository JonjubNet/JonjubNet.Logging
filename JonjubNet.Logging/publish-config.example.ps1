# Archivo de configuración de ejemplo para publicación
# Copia este archivo como publish-config.ps1 y actualiza con tus valores

# API Key de NuGet.org (obtén una desde https://www.nuget.org/account/apikeys)
$NUGET_API_KEY = "REPLACE_WITH_YOUR_NUGET_API_KEY"

# Versión del paquete
$PACKAGE_VERSION = "1.0.0"

# Configuración de compilación
$BUILD_CONFIGURATION = "Release"

# URL del repositorio (opcional, para documentación)
$REPOSITORY_URL = "https://github.com/tu-usuario/JonjubNet.Logging"

# Información del autor
$AUTHOR_NAME = "Tu Nombre"
$AUTHOR_EMAIL = "tu-email@ejemplo.com"

# Ejemplo de uso:
# .\publish-config.ps1
# .\publish-package.ps1 -ApiKey $NUGET_API_KEY -Version $PACKAGE_VERSION
