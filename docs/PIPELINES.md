# Automatización y Pipelines CI/CD: ISTPET

Este documento detalla la infraestructura de Integración y Despliegue Continuo configurada mediante GitHub Actions.

## Estructura de Automatización
El sistema cuenta con dos tuberías independientes para garantizar la velocidad y calidad de las entregas.

### 1. Pipeline de Backend (`backend-ci.yml`)
- **Entorno**: Ubuntu Latest / .NET 8 SDK.
- **Acciones**:
  - `Restore`: Descarga de paquetes NuGet.
  - `Build`: Compilación en modo Release.
  - `Publish`: Generación del artefacto listo para ser desplegado en el servidor.
- **Seguridad**: El pipeline valida que todo el código cumpla con los estándares de compilación de .NET antes de permitir un cambio en la rama principal.

### 2. Pipeline de Frontend (`frontend-ci.yml`)
- **Entorno**: Ubuntu Latest / Node.js 20.
- **Acciones**:
  - `Install`: Instalación de dependencias (Vite, React, Tailwind).
  - `Build`: Generación del paquete estático optimizado (Minificado y comprimido).
- **Control de Calidad**: Asegura que no existan errores de sintaxis o de importación en la interfaz de usuario.

## Cómo Ejecutar los Pipelines
1. Los pipelines se activan automáticamente en cada `push` o `pull_request` a las ramas `main` o `develop`.
2. Puedes monitorear el estado de las ejecuciones en la pestaña **Actions** de tu repositorio en GitHub.
