# Hoja de Ruta (Roadmap) — ISTPET Logística

Estado actual del sistema: **Operacional** (Control de salida/llegada, Puente SIGAFI, Dashboard).

---

## Funcionalidades Pendientes

### Prioridad Alta

| Funcionalidad | Descripción | Módulo |
| :--- | :--- | :--- |
| **Autenticación JWT** | Implementar JWT Bearer Tokens para proteger todos los endpoints. Actualmente no hay sesión ni autorización por rol en los controladores. | Backend + Frontend |
| **Autorización por Rol** | Agregar `[Authorize(Roles="admin")]` a los endpoints de gestión (vehiculos, instructores, cursos). Los guardias solo deberían acceder al Control Operativo. | Backend |
| **Gestión de Instructores (CRUD)** | La tabla `instructores` tiene CRUD parcial. Falta la UI para agregar, editar y desactivar instructores. | Frontend |
| **Gestión de Cursos (CRUD)** | Los cursos se pueden agregar solo via SQL directamente. Se necesita una UI administrativa. | Frontend |
| **Gestión de Vehículos (CRUD)** | Similar a instructores y cursos — solo vista de catálogo, sin edición. | Frontend |

---

### Prioridad Media

| Funcionalidad | Descripción | Módulo |
| :--- | :--- | :--- |
| **Registro de Mantenimiento** | La tabla `mantenimientos` existe pero no hay UI ni endpoint para agregar registros. | Backend + Frontend |
| **Historial por Estudiante** | Vista de todos los registros de salida/llegada de un estudiante con horas acumuladas. | Backend + Frontend |
| **Historial por Vehículo** | Registro de kilómetros y mantenimientos históricos por unidad. | Backend + Frontend |
| **Pruebas Unitarias** | Implementar `xUnit` para el backend y reemplazar el `echo` actual en el pipeline CI. | Backend + CI |
| **HTTPS en Producción** | Habilitar `app.UseHttpsRedirection()` y configurar certificado TLS. | Backend |
| **Variables de Entorno** | Mover la cadena de conexión de `appsettings.json` a variables de entorno para producción. | Backend |

---

### Prioridad Baja / Visión Futura

| Funcionalidad | Descripción |
| :--- | :--- |
| **Reportes de Horas por Período** | Generar reportes PDF/Excel de horas de práctica completadas por estudiante por período. |
| **Notificaciones en Tiempo Real** | Usar SignalR o WebSockets para que el panel de monitoreo se actualice automáticamente sin necesidad de refrescar. |
| **App Móvil** | Versión responsiva o PWA para uso desde tablets en la garita. |
| **Estadísticas de Flota** | Métricas de utilización de vehículos: horas de uso, kilómetros acumulados, frecuencia de mantenimiento. |
| **Integración GPS** | Rastreo en tiempo real de la posición de los vehículos durante las prácticas. |
| **Alertas Automáticas de Mantenimiento** | Notificación cuando un vehículo supera un umbral de horas de uso. |

---

## Deuda Técnica Identificada

| Ítem | Descripción |
| :--- | :--- |
| **CORS `AllowAll`** | La configuración actual permite cualquier origen. Debe restringirse al dominio de producción antes del despliegue. |
| **`registradoPor` hardcodeado** | En `logisticaService.js`, el campo `registradoPor` siempre se envía como `1`. Debe usar el `idUsuario` de la sesión activa una vez implementado JWT. |
| **Sin refresh automático** | El dashboard y el monitor de llegadas requieren recargar la página manualmente. Implementar polling o WebSockets. |
| **AutoMapper subutilizado** | El perfil de AutoMapper está configurado pero los mapeos se hacen con proyecciones LINQ directamente en los controladores. Unificar el enfoque. |
