# Instrucciones para Publicar JonjubNet.Logging

## Configuración de GitHub Actions

Para que el workflow de GitHub Actions pueda publicar automáticamente el paquete, necesitas configurar la API key como un secreto en tu repositorio de GitHub.

### 1. Configurar el Secreto en GitHub

1. Ve a tu repositorio en GitHub
2. Haz clic en **Settings** (Configuración)
3. En el menú lateral, haz clic en **Secrets and variables** → **Actions**
4. Haz clic en **New repository secret**
5. Nombre: `NUGET_API_KEY`
6. Valor: `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`
7. Haz clic en **Add secret**

### 2. Publicación Automática

Una vez configurado el secreto, el paquete se publicará automáticamente cuando:

1. Creas un tag que comience con 'v' (ej: `v1.0.0`)
2. Haces push del tag al repositorio

```bash
# Crear y publicar un tag
git tag v1.0.0
git push origin v1.0.0
```

## Publicación Manual

### Opción 1: Usar el Script de PowerShell (Windows)

```powershell
# Ejecutar el script de publicación
.\publish-package.ps1

# O con parámetros personalizados
.\publish-package.ps1 -Version "1.0.0" -ApiKey "tu-api-key"
```

### Opción 2: Usar el Script de Bash (Linux/Mac)

```bash
# Hacer ejecutable el script
chmod +x publish-package.sh

# Ejecutar el script
./publish-package.sh
```

### Opción 3: Comando Directo

```bash
# Asegúrate de que el paquete esté generado
dotnet pack --configuration Release --output ./packages

# Publicar el paquete
dotnet nuget push ./packages/JonjubNet.Logging.1.0.0.nupkg --api-key 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 --source https://api.nuget.org/v3/index.json
```

## Verificación

Después de la publicación, puedes verificar que el paquete esté disponible en:

- **NuGet.org**: https://www.nuget.org/packages/JonjubNet.Logging
- **Búsqueda**: https://www.nuget.org/packages?q=JonjubNet.Logging

## Instalación del Paquete

Una vez publicado, otros desarrolladores pueden instalar el paquete usando:

```bash
dotnet add package JonjubNet.Logging
```

O agregando la referencia en el archivo `.csproj`:

```xml
<PackageReference Include="JonjubNet.Logging" Version="1.0.0" />
```

## Notas Importantes

1. **API Key**: La API key proporcionada es específica para este proyecto. No la compartas públicamente.

2. **Versiones**: Cada versión debe ser única. Si necesitas publicar una nueva versión, incrementa el número de versión en el archivo `.csproj`.

3. **Tiempo de Propagación**: Después de la publicación, puede tomar unos minutos para que el paquete esté disponible en NuGet.org.

4. **Verificación**: Siempre verifica que el paquete se haya publicado correctamente antes de anunciarlo.

## Troubleshooting

### Error: "Package already exists"
- Incrementa el número de versión en `JonjubNet.Logging.csproj`
- Regenera el paquete con `dotnet pack`

### Error: "Invalid API key"
- Verifica que la API key sea correcta
- Asegúrate de que la API key tenga permisos para publicar

### Error: "Package validation failed"
- Revisa que todos los metadatos del paquete estén correctos
- Verifica que no haya archivos duplicados o corruptos
