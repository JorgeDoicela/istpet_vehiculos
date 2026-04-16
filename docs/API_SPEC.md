# Especificación de la API REST — ISTPET Zenith Edición 2026

Este documento define el contrato técnico oficial de la API del Sistema ISTPET Vehículos. Todas las comunicaciones se realizan vía HTTPS/JSON bajo el estándar **Industrial ApiResponse<T>**.

---

## 1. Núcleo Operativo (Logistics Controller)

### GET `/api/logistica/estudiante/{cedula}`
**Pilar**: Bridge JIT (Just-In-Time).
Consulta simultánea en SIGAFI Central y Espejo Local.
*   **Enriquecimiento Automático**: Si el alumno tiene una cita agendada hoy en SIGAFI, inyecta automáticamente el vehículo e instructor sugeridos.

### GET `/api/logistica/vehiculos-disponibles`
Muestra unidades activas y operativas que no están actualmente en pista (según la vista `v_clases_activas`).

### POST `/api/logistica/salida`
Registra el despacho de una unidad.
*   **Validación**: Requiere Matrícula, Vehículo e Instructor válidos en SIGAFI.
*   **Business Rules**: Impide la salida si el vehículo ya está fuera o si el alumno tiene una práctica pendiente.

### POST `/api/logistica/llegada`
Registra el retorno y libera la unidad. Calcula automáticamente el tiempo en pista para el historial académico.

---

## 2. Motor de Inteligencia y Sync (Sync Engine)

### POST `/api/sync/master`
Ejecuta la orquestación de **23 módulos de paridad**.
1.  **Académico**: Carreras, Periodos, Secciones, Modalidades, Cursos.
2.  **Infraestructura**: Vehículos, Categorías, Licencias, Exam Links.
3.  **Planificación**: Horarios, Fechas, Calendarios de Profesores.
4.  **Seguridad**: Sincronización de credenciales `usuarios_web`.

### GET `/api/sync/audit`
Devuelve el reporte de deriva de datos (*Data Drift*). Compara conteos remotos vs. locales por tabla.

---

## 3. Reportes Unificados (Reports Controller)

### GET `/api/reports/practicas`
**Endpoint Primario de Auditoría**.
Genera un reporte consolidado entre SIGAFI y el Espejo Local.
*   **Filtros**: `fechaInicio`, `fechaFin`, `instructorId`.
*   **Merge Logic**: Si una práctica existe localmente con estados más frescos (ej: hora de llegada cargada en el momento), prioriza el dato local sobre el académico.

---

## 4. Estándares de Respuesta

Estructura global `ApiResponse<T>`:
```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": { ... },
  "timestamp": "2026-04-16T14:40:00Z"
}
```

### Códigos de Lógica Operativa:
- `VEHICULO_EN_USO`: Bloqueo de concurrencia en pista.
- `ESTUDIANTE_EN_PISTA`: El alumno debe cerrar su retorno antes de iniciar otra práctica.
- `INVALID_MATRICULA`: El estudiante no tiene una matrícula válida para el periodo actual en SIGAFI.
- `CIRCUIT_OPEN`: El enlace con SIGAFI ha sido suspendido temporalmente por resiliencia de red.

---

## 5. Seguridad y Roles
- **admin**: Acceso total (Reportes, Sync Master).
- **logistica**: Gestión de flotas e instructores.
- **guardia**: Registro de Salidas/Llegadas y Pista.
