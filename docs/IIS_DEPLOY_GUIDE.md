# Guía de Despliegue en IIS (Manual & Automatizado)

Esta guía detalla el proceso para actualizar el sistema ISTPET Vehículos en el servidor de producción institucional.

## 1. Automatización (Recomendado)

Se ha creado un script de PowerShell en la raíz del proyecto para realizar todo el proceso de compilación y empaquetado automáticamente.

### Ejecución:
1. Abre una terminal en la carpeta raíz del proyecto.
2. Ejecuta: `.\scripts\build-and-package.ps1`
3. Al finalizar, obtendrás un archivo llamado `ISTPET_DEPLOY_BUNDLE_YYYYMMDD_HHMM.zip` en la raíz.

---

## 2. Procedimiento en el Servidor

Una vez que tengas el paquete ZIP, sigue estos pasos en el servidor IIS:

### A. Preparación (Importante)
Para evitar errores de "Archivo en uso", se recomienda detener los procesos activos:
1. Abre el **Administrador de IIS**.
2. En el panel izquierdo, ve a **Application Pools**.
3. Detén los pools asociados a `apiLogistica` y `logistica`.

### B. Actualización del Backend (API)
1. Ruta: `C:\inetpub\wwwroot\apiLogistica`
2. Realiza un backup de la carpeta actual por seguridad.
3. Borra el contenido actual (excepto archivos de configuración específicos como `.logs` si existen).
4. Copia el contenido de la carpeta `/backend` del ZIP a esta ruta.

### C. Actualización del Frontend (UI)
1. Ruta: `C:\inetpub\wwwroot\logistica`
2. Borra el contenido actual.
3. Copia el contenido de la carpeta `/frontend` del ZIP a esta ruta.

### D. Finalización
1. Inicia nuevamente los **Application Pools** en IIS.
2. Accede a la URL del sistema y verifica la funcionalidad.

---

## 3. Notas Técnicas
- **web.config**: El backend incluye un archivo `web.config` generado automáticamente por .NET que permite la integración con IIS.
- **CORS**: Asegúrate de que las URLs en el frontend build (VITE_API_URL) coincidan con la URL de producción del API.
