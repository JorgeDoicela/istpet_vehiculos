# Pipelines de CI/CD: Automatización de Grado Industrial

El ecosistema ISTPET utiliza **GitHub Actions** para garantizar la integridad del código, la seguridad de las dependencias y la preparación para el despliegue mediante contenedores.

---

## 1. Pipeline de Backend (Docker-Ready CI)

**Archivo:** `.github/workflows/backend-ci.yml`

Este pipeline valida la lógica de negocio y la compatibilidad con el entorno de contenedores en cada integración.

### Etapas Críticas:
1.  **Exploración de Dependencias**: Ejecuta `dotnet restore` validando vulnerabilidades conocidas en paquetes NuGet.
2.  **Compilación Distribuida**: Realiza un `dotnet build` en modo Release para detectar errores de tipado o inconsistencias en los DTOs.
3.  **Preparación de Artefacto**: Ejecuta `dotnet publish` generando los binarios optimizados que el `Dockerfile` de producción utilizará en Render.
4.  **Security Scan (Advisory)**: Análisis estático del código para prevenir inyecciones SQL en los SQL Providers.

---

## 2. Pipeline de Frontend (Vercel Core CI)

**Archivo:** `.github/workflows/frontend-ci.yml`

Optimizado para el motor de construcción de Vite y la infraestructura de Vercel.

### Etapas Críticas:
1.  **Integridad de Módulos**: Ejecuta `npm install` verificando que el `package-lock.json` sea consistente.
2.  **Vite Transpilation**: Ejecuta `npm run build` para asegurar que todo el JSX y Tailwind CSS se compila correctamente en activos estáticos optimizados.
3.  **Asset Optimization**: Verificación de que los SVG y activos multimedia estén listos para ser servidos mediante CDN.

---

## 3. Estrategia de Ramas y Promoción

El flujo de trabajo sigue un modelo de **Entrega Continua**:

*   **`feature/*`**: Ramas de desarrollo. Requieren PR para integrarse.
*   **`develop`**: Rama de integración continua. Dispara el entorno de Staging (Mock SIGAFI).
*   **`main`**: Rama de producción. Solo se integra tras superar los tests de paridad. Dispara el despliegue automático a Render y Vercel.

---

## 4. Próximas Implementaciones en CI/CD
*   **Automated Smoke Tests**: Ejecución de una suite de `Postman/Newman` contra el ambiente de staging para validar los 20 pasos del Master Sync.
*   **Docker Hub Push**: Publicación automatizada de imágenes de contenedor al registro tras cada lanzamiento exitoso en `main`.
