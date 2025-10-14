# Configuración de API Key para NuGet.org

## ⚠️ API Key Incorrecta

La API key que proporcionaste (`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`) es una **GitHub Personal Access Token**, no una API key de NuGet.org.

## Cómo Obtener la API Key Correcta de NuGet

### 1. Crear Cuenta en NuGet.org

1. Ve a [nuget.org](https://www.nuget.org)
2. Haz clic en **Sign in** (Iniciar sesión)
3. Inicia sesión con tu cuenta de Microsoft o crea una nueva

### 2. Obtener la API Key

1. Una vez iniciada la sesión, haz clic en tu nombre de usuario en la esquina superior derecha
2. Selecciona **API Keys** del menú desplegable
3. Haz clic en **Create** → **Create API Key**
4. Completa el formulario:
   - **Key name**: `JonjubNet.Logging` (o el nombre que prefieras)
   - **Package owner**: Selecciona tu cuenta
   - **Scopes**: Selecciona **Push** para permitir publicación
   - **Glob pattern**: `JonjubNet.Logging*` (para limitar a este paquete específico)
5. Haz clic en **Create**
6. **IMPORTANTE**: Copia la API key que se genera (comenzará con `oy2...` o similar)

### 3. Usar la API Key Correcta

Una vez que tengas la API key correcta de NuGet, actualiza los scripts:

#### En `publish-package.ps1`:
```powershell
[string]$ApiKey = "tu-api-key-de-nuget-aqui"
```

#### En `publish-package.sh`:
```bash
API_KEY="tu-api-key-de-nuget-aqui"
```

#### Comando directo:
```bash
dotnet nuget push ./packages/JonjubNet.Logging.1.0.0.nupkg --api-key tu-api-key-de-nuget-aqui --source https://api.nuget.org/v3/index.json
```

## Configuración para GitHub Actions

Para GitHub Actions, también necesitas usar la API key de NuGet, no la de GitHub:

1. Ve a tu repositorio en GitHub
2. **Settings** → **Secrets and variables** → **Actions**
3. **New repository secret**:
   - Name: `NUGET_API_KEY`
   - Value: `tu-api-key-de-nuget-aqui` (la que obtuviste de nuget.org)

## Diferencias entre las API Keys

| Tipo | Formato | Uso |
|------|---------|-----|
| GitHub Personal Access Token | `ghp_...` | Acceso a GitHub API, repositorios, etc. |
| NuGet API Key | `oy2...` o similar | Publicación de paquetes en NuGet.org |

## Seguridad

- **Nunca** compartas tu API key públicamente
- **Nunca** la incluyas en código fuente
- Usa siempre variables de entorno o secretos para almacenarla
- Regenera la API key si sospechas que ha sido comprometida

## Próximos Pasos

1. Obtén la API key correcta de NuGet.org
2. Actualiza los scripts con la nueva API key
3. Ejecuta la publicación del paquete
4. Verifica que el paquete esté disponible en [nuget.org](https://www.nuget.org/packages/JonjubNet.Logging)
