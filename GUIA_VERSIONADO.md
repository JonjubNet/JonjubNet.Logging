# 📦 Guía de Versionado - JonjubNet.Logging

## 🎯 Estrategia de Versionado Actual

### Versión Actual en .csproj: `3.1.6`

## 📋 Flujo de Versionado

### 1. **Versión Base (en .csproj)**
- La versión en `Presentation/JonjubNet.Logging/JonjubNet.Logging.csproj` es la **versión base**
- Esta versión se usa como referencia para todas las ramas
- **Formato:** `MAJOR.MINOR.PATCH` (ej: `3.1.6`)

### 2. **Cómo Funciona el Workflow de GitHub Actions**

#### **A) Push a Tag `v*` (ej: `v3.1.6`)**
- ✅ **Extrae versión del tag:** `v3.1.6` → `3.1.6`
- ✅ **Actualiza el .csproj** con esa versión
- ✅ **Compila y empaqueta** con versión `3.1.6`
- ✅ **Publica a NuGet.org** (producción)
- ✅ **Publica a GitHub Packages**
- ✅ **Crea Release en GitHub**

#### **B) Push a Branch `main`**
- ✅ **Usa versión del .csproj** (ej: `3.1.6`)
- ✅ **Compila y empaqueta** con esa versión
- ✅ **Publica SOLO a GitHub Packages** (NO a NuGet.org)
- ❌ **NO crea Release**

#### **C) Push a Branch `test`**
- ✅ **Usa versión del .csproj + sufijo:** `3.1.6-test.20241215143000`
- ✅ **Publica SOLO a GitHub Packages**

#### **D) Push a Branch `feature/*`**
- ✅ **Usa versión del .csproj + sufijo:** `3.1.6-feature.nombre-rama.20241215143000`
- ✅ **Publica SOLO a GitHub Packages**

## 🔄 Proceso Recomendado para Publicar una Nueva Versión

### **Opción 1: Versión de Producción (NuGet.org)**

1. **Actualizar versión en .csproj:**
   ```xml
   <Version>3.1.7</Version>
   ```

2. **Commit y push:**
   ```bash
   git add Presentation/JonjubNet.Logging/JonjubNet.Logging.csproj
   git commit -m "Bump version to 3.1.7"
   git push origin main
   ```

3. **Crear tag y push:**
   ```bash
   git tag v3.1.7
   git push origin v3.1.7
   ```

4. **El workflow automáticamente:**
   - Actualizará el .csproj (si es necesario)
   - Compilará
   - Empaquetará con versión `3.1.7`
   - Publicará a NuGet.org
   - Publicará a GitHub Packages
   - Creará Release en GitHub

### **Opción 2: Versión de Prueba (Solo GitHub Packages)**

1. **Push a branch `test`:**
   ```bash
   git checkout test
   git merge main
   git push origin test
   ```

2. **El workflow automáticamente:**
   - Usará versión del .csproj + `-test.timestamp`
   - Publicará SOLO a GitHub Packages

## ⚠️ Problemas Actuales Identificados

### 1. **Confusión entre Versión en .csproj y Tag**
- El workflow actualiza el .csproj cuando hay un tag, pero esto puede causar confusión
- **Recomendación:** La versión en .csproj debe ser la "siguiente versión" que planeas publicar

### 2. **Múltiples Tags con la Misma Versión**
- Se han creado múltiples tags `v3.1.6` apuntando a diferentes commits
- **Recomendación:** Un tag debe ser inmutable, no re-crearlo

### 3. **Versión no Sincronizada**
- La versión en .csproj puede no coincidir con el último tag publicado
- **Recomendación:** Después de publicar, actualizar .csproj a la siguiente versión

## ✅ Mejores Prácticas Recomendadas

### **Estructura de Versiones (Semantic Versioning)**
- **MAJOR** (3): Cambios incompatibles con versiones anteriores
- **MINOR** (1): Nuevas funcionalidades compatibles hacia atrás
- **PATCH** (6): Correcciones de bugs compatibles

### **Flujo Ideal:**

```
1. Desarrollo en main → Versión en .csproj: 3.1.7 (siguiente versión planeada)
2. Cuando esté listo para publicar:
   a) Asegurar que .csproj tiene 3.1.7
   b) Crear tag: git tag v3.1.7
   c) Push tag: git push origin v3.1.7
3. Después de publicar exitosamente:
   a) Actualizar .csproj a 3.1.8 (siguiente versión)
   b) Commit y push
```

### **Reglas de Oro:**
1. ✅ **Nunca re-crear un tag existente** (son inmutables)
2. ✅ **La versión en .csproj debe ser >= al último tag publicado**
3. ✅ **Solo crear tags para versiones de producción**
4. ✅ **Usar branches `test` o `feature/*` para versiones pre-release**

## 🔧 Estado Actual del Proyecto (Análisis)

### Situación Actual:
- **Versión en .csproj:** `3.1.6`
- **Último tag publicado:** `v3.1.6` (commit: c709e74)
- **HEAD actual:** d6e6245 (1 commit después del tag)
- **Cambios pendientes:** Correcciones de warnings obsoletos

### ⚠️ Problema Detectado:
Hay **1 commit nuevo** después del tag `v3.1.6` que incluye correcciones importantes:
- Fix obsolete warnings (Rfc2898DeriveBytes, X509CertificateLoader, null references)

### ✅ Recomendación Inmediata:

**OPCIÓN A: Publicar como 3.1.7 (Recomendado)**
- Los cambios son correcciones de bugs/warnings (PATCH)
- Actualizar .csproj a `3.1.7`
- Crear tag `v3.1.7`
- Publicar a NuGet.org

**OPCIÓN B: Mantener 3.1.6**
- Solo si los cambios son muy menores
- Crear nuevo tag `v3.1.6` apuntando al commit actual (pero esto viola inmutabilidad de tags)

## 📝 Acción Recomendada AHORA:

**Versión recomendada: `3.1.7`**

Razón: Hay correcciones de código (warnings obsoletos) que son mejoras de calidad, merecen un PATCH increment.

### Pasos:
1. Actualizar .csproj a `3.1.7`
2. Commit y push
3. Crear tag `v3.1.7` 
4. Push tag → Se publicará automáticamente a NuGet.org

---

**¿Quieres que implemente alguna mejora específica en el sistema de versionado?**
