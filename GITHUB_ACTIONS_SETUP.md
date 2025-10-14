# Configuración de GitHub Actions para JonjubNet.Logging

Este documento explica cómo configurar las variables y secretos necesarios en GitHub para que el workflow de CI/CD funcione correctamente.

## 🔐 Secretos Requeridos

### 1. NUGET_API_KEY
**Descripción**: API Key de NuGet.org para publicar paquetes
**Tipo**: Repository Secret
**Ubicación**: Settings → Secrets and variables → Actions → Repository secrets

#### Cómo obtener la API Key:
1. Ve a [nuget.org](https://www.nuget.org)
2. Inicia sesión con tu cuenta de Microsoft
3. Ve a tu perfil → **API Keys**
4. Haz clic en **Create** → **Create API Key**
5. Configura:
   - **Key name**: `JonjubNet.Logging`
   - **Package owner**: Tu cuenta
   - **Scopes**: `Push`
   - **Glob pattern**: `JonjubNet.Logging*`
6. Copia la API key (formato: `oy2...`)

#### Configurar en GitHub:
- **Name**: `NUGET_API_KEY`
- **Value**: `tu-api-key-de-nuget-aqui`

### 2. JONJUBNET_TOKEN
**Descripción**: Token de GitHub para publicar en GitHub Packages (Personal Access Token)
**Tipo**: Repository Secret
**Ubicación**: Settings → Secrets and variables → Actions → Repository secrets

#### Cómo obtener el Token:
1. Ve a GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. Haz clic en **Generate new token** → **Generate new token (classic)**
3. Configura:
   - **Note**: `JonjubNet.Logging - GitHub Packages`
   - **Expiration**: Según tu preferencia
   - **Scopes**: Selecciona `write:packages` y `read:packages`
4. Copia el token (formato: `ghp_...`)

#### Configurar en GitHub:
- **Name**: `JONJUBNET_TOKEN`
- **Value**: `tu-github-token-aqui`

### 3. JONJUBNET_NAME (Opcional)
**Descripción**: Nombre de usuario de GitHub para autenticación
**Tipo**: Repository Variable (opcional)
**Ubicación**: Settings → Secrets and variables → Actions → Variables

#### Configurar en GitHub:
- **Name**: `JONJUBNET_NAME`
- **Value**: `tu-usuario-github`

## 🔧 Variables de Entorno (Opcionales)

### Variables del Workflow
El workflow ya está configurado con estas variables por defecto:
- `DOTNET_VERSION`: `10.0.x`
- `PACKAGE_NAME`: `JonjubNet.Logging`

### Variables Personalizadas (Opcional)
Si necesitas personalizar el comportamiento, puedes agregar estas variables en:
**Settings → Secrets and variables → Actions → Variables**

| Variable | Descripción | Valor por Defecto |
|----------|-------------|-------------------|
| `BUILD_CONFIGURATION` | Configuración de compilación | `Release` |
| `NUGET_SOURCE` | Fuente de NuGet | `https://api.nuget.org/v3/index.json` |
| `GITHUB_PACKAGES_SOURCE` | Fuente de GitHub Packages | `https://nuget.pkg.github.com/OWNER/index.json` |

## 📋 Pasos de Configuración

### 1. Configurar Secretos
1. Ve a tu repositorio en GitHub
2. **Settings** → **Secrets and variables** → **Actions**
3. **Repository secrets** → **New repository secret**
4. Agrega estos secretos:
   - **`NUGET_API_KEY`** con tu API key de NuGet.org
   - **`JONJUBNET_TOKEN`** con tu GitHub Personal Access Token

### 2. Configurar Variables (Opcional)
1. **Variables** → **New repository variable**
2. Agrega **`JONJUBNET_NAME`** con tu nombre de usuario de GitHub

### 3. Verificar Permisos
Asegúrate de que el workflow tenga permisos para:
- **Contents**: Read (para checkout)
- **Packages**: Write (para publicar en GitHub Packages)
- **Actions**: Read (para usar artifacts)

### 4. Configurar Branch Protection (Recomendado)
1. **Settings** → **Branches**
2. Agrega regla para `main`:
   - ✅ Require a pull request before merging
   - ✅ Require status checks to pass before merging
   - ✅ Require branches to be up to date before merging
   - ✅ Require conversation resolution before merging

## 🚀 Triggers del Workflow

El workflow se ejecuta automáticamente en:

### Push Events
- **Branches**: `main`, `develop`
- **Tags**: `v*` (ej: `v1.0.0`, `v1.2.3`)

### Pull Request Events
- **Target branches**: `main`

## 📦 Publicación Automática

### Cuándo se Publica
- ✅ Push a `main` (versión de desarrollo)
- ✅ Push de tag `v*` (versión estable)
- ❌ Pull requests (solo build y test)

### Dónde se Publica
1. **NuGet.org** (público)
2. **GitHub Packages** (privado por defecto)

### Configurar GitHub Packages como Público
Si quieres que GitHub Packages sea público:
1. Ve a tu repositorio → **Packages**
2. Selecciona el paquete
3. **Package settings** → **Change visibility** → **Public**

## 🔍 Verificación

### Verificar que Funciona
1. Haz push a `main` o crea un tag `v1.0.1`
2. Ve a **Actions** en tu repositorio
3. Verifica que el workflow se ejecute correctamente
4. Comprueba que el paquete aparezca en:
   - [NuGet.org](https://www.nuget.org/packages/JonjubNet.Logging)
   - GitHub Packages de tu repositorio

### Logs de Debugging
Si algo falla, revisa:
1. **Actions** → Selecciona el workflow fallido
2. Revisa los logs de cada step
3. Verifica que los secretos estén configurados correctamente

## 🛠️ Personalización

### Modificar el Workflow
El archivo `.github/workflows/ci-cd.yml` puede ser personalizado para:
- Cambiar versiones de .NET
- Agregar más tests
- Modificar triggers
- Cambiar fuentes de publicación

### Agregar Tests
Para agregar tests unitarios:
1. Crea proyecto de test: `dotnet new xunit -n JonjubNet.Logging.Tests`
2. Agrega referencia al proyecto principal
3. El workflow ejecutará automáticamente los tests

## 📚 Recursos Adicionales

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [NuGet API Keys](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [GitHub Packages](https://docs.github.com/en/packages)
- [.NET GitHub Actions](https://github.com/actions/setup-dotnet)

## ⚠️ Notas de Seguridad

- **Nunca** expongas API keys en el código
- Usa siempre Repository Secrets para datos sensibles
- Revisa regularmente los permisos de las API keys
- Regenera las API keys si sospechas compromiso
