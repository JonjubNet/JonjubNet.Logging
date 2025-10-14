# Instrucciones de Configuración - JonjubNet.Logging

## ⚠️ Archivos con Credenciales Sensibles

Los siguientes archivos contienen credenciales y **NO deben subirse al repositorio**:

- `nuget.config`
- `publish-package.ps1`
- `publish-package.sh`
- `publish-config.ps1`

## 🔧 Configuración Inicial

### 1. Configurar NuGet (para GitHub Packages)

```bash
# Copiar el archivo de ejemplo
cp nuget.config.example nuget.config
```

Edita `nuget.config` y reemplaza:
- `REPLACE_WITH_YOUR_GITHUB_USERNAME` → Tu nombre de usuario de GitHub
- `REPLACE_WITH_YOUR_GITHUB_TOKEN` → Tu Personal Access Token de GitHub

**Obtener GitHub Token:**
1. Ve a GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate new token (classic)
3. Selecciona scopes: `write:packages`, `read:packages`
4. Copia el token generado

### 2. Configurar Scripts de Publicación

#### Para Windows (PowerShell):
```powershell
# Copiar el archivo de ejemplo
Copy-Item publish-package.ps1.example publish-package.ps1

# Configurar variable de entorno
$env:NUGET_API_KEY = "tu-api-key-de-nuget-aqui"
```

#### Para Linux/macOS (Bash):
```bash
# Copiar el archivo de ejemplo
cp publish-package.sh.example publish-package.sh

# Hacer ejecutable
chmod +x publish-package.sh

# Configurar variable de entorno
export NUGET_API_KEY="tu-api-key-de-nuget-aqui"
```

### 3. Obtener API Key de NuGet.org

1. Ve a [nuget.org](https://www.nuget.org)
2. Inicia sesión con tu cuenta de Microsoft
3. Ve a tu perfil → API Keys
4. Create → Create API Key
5. Configura:
   - **Key name**: `JonjubNet.Logging`
   - **Package owner**: Tu cuenta
   - **Scopes**: Push
   - **Glob pattern**: `JonjubNet.Logging*`
6. Copia la API key generada

## 🚀 Uso de los Scripts

### Publicar a NuGet.org

#### Windows:
```powershell
# Opción 1: Con variable de entorno
$env:NUGET_API_KEY = "tu-api-key"
.\publish-package.ps1

# Opción 2: Con parámetro
.\publish-package.ps1 -ApiKey "tu-api-key" -Version "1.0.1"
```

#### Linux/macOS:
```bash
# Opción 1: Con variable de entorno
export NUGET_API_KEY="tu-api-key"
./publish-package.sh

# Opción 2: Con variable inline
NUGET_API_KEY="tu-api-key" ./publish-package.sh
```

### Publicar a GitHub Packages

```bash
# Compilar el paquete
dotnet pack --configuration Release --output ./packages

# Publicar a GitHub Packages
dotnet nuget push ./packages/JonjubNet.Logging.1.0.0.nupkg --source "github"
```

## 🔒 Seguridad

### Variables de Entorno (Recomendado)

#### Windows (PowerShell):
```powershell
# Temporal (solo para la sesión actual)
$env:NUGET_API_KEY = "tu-api-key"

# Permanente (usuario actual)
[Environment]::SetEnvironmentVariable("NUGET_API_KEY", "tu-api-key", "User")

# Permanente (sistema)
[Environment]::SetEnvironmentVariable("NUGET_API_KEY", "tu-api-key", "Machine")
```

#### Linux/macOS:
```bash
# Temporal (solo para la sesión actual)
export NUGET_API_KEY="tu-api-key"

# Permanente (agregar a ~/.bashrc o ~/.zshrc)
echo 'export NUGET_API_KEY="tu-api-key"' >> ~/.bashrc
source ~/.bashrc
```

### Archivo de Configuración Local

Crea un archivo `publish-config.ps1` (ya está en .gitignore):

```powershell
# publish-config.ps1
$env:NUGET_API_KEY = "tu-api-key-aqui"
$env:GITHUB_TOKEN = "tu-github-token-aqui"

# Ejecutar el script
.\publish-package.ps1
```

## 📁 Estructura de Archivos

```
JonjubNet.Logging/
├── nuget.config.example          # ✅ Plantilla para nuget.config
├── publish-package.ps1.example   # ✅ Plantilla para PowerShell
├── publish-package.sh.example    # ✅ Plantilla para Bash
├── publish-config.example.ps1    # ✅ Plantilla para configuración
├── nuget.config                  # ❌ NO subir (contiene credenciales)
├── publish-package.ps1           # ❌ NO subir (contiene credenciales)
├── publish-package.sh            # ❌ NO subir (contiene credenciales)
└── publish-config.ps1            # ❌ NO subir (contiene credenciales)
```

## 🔄 Flujo de Trabajo

1. **Desarrollo**: Usa los archivos `.example` como plantilla
2. **Configuración**: Copia y personaliza los archivos sin `.example`
3. **Publicación**: Ejecuta los scripts con las credenciales configuradas
4. **Git**: Los archivos con credenciales están en `.gitignore`

## 🆘 Solución de Problemas

### Error: "No se encontró la API key"
- Verifica que la variable de entorno esté configurada
- En PowerShell: `$env:NUGET_API_KEY`
- En Bash: `echo $NUGET_API_KEY`

### Error: "Unauthorized"
- Verifica que la API key sea correcta
- Asegúrate de que la API key tenga permisos de Push
- Para GitHub Packages, verifica el token y username

### Error: "Package already exists"
- Incrementa la versión en el archivo `.csproj`
- O usa el parámetro `-Version` en los scripts

## 📚 Referencias

- [NuGet API Keys](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [GitHub Packages](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)
- [dotnet nuget push](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push)
