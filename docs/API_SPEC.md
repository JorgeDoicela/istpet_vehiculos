# Especificación de la API REST (Industrial Edition 2026)

Este documento define el contrato técnico de la API del Sistema ISTPET Vehículos. Todas las comunicaciones se realizan vía HTTPS/JSON bajo el estándar **Industrial ApiResponse**.

---

## 1. Núcleo Académico y Puente (Logística Hub)

### GET `/api/logistica/estudiante/{cedula}`
**Pilar**: Puente Híbrido Universal.
Consulta simultánea en el Espejo Local y en la BD Central SIGAFI.
*   **JIT Enrichment**: Si el alumno no reside localmente, se materializa instantáneamente desde SIGAFI.
*   **Agenda Mapping**: Cruza automágicamente con la tabla `cond_alumnos_practicas` de SIGAFI para sugerir vehículo e instructor.

### GET `/api/logistica/vehiculos-disponibles`
**Pilar**: Flota Inteligente.
Retorna unidades con estado `OPERATIVO` que no tienen registros de salida pendientes en la vista `v_clases_activas`.

---

## 2. Motor de Sincronización e Integridad (Sync Engine)

### GET `/api/sync/ping-sigafi`
Valida el **Enlace de Paridad**. Retorna latencia y estado del clúster SIGAFI.

### GET `/api/sync/sigafi-probe`
**Sonda de Extracción**: Ejecuta una simulación de carga masiva para los 13 módulos críticos sin comprometer la base de datos local.

### POST `/api/sync/master`
**Orquestación de Paridad**: Ejecuta el pipeline de 20 pasos del motor Master Sync. Aplica el **Escudo de Datos (Data Shield)** para sanitización y truncamiento proactivo.

---

## 3. Auditoría e Inspección de Paridad

### GET `/api/sync/audit`
**Auditoría Estadística**: Devuelve una comparativa de conteo de registros entre la fuente de verdad (SIGAFI) y el espejo local para detección de derivas de datos.

### GET `/api/sync/inspect/student/{idAlumno}`
**Inspección Granular**: Realiza un *Deep Mapping* comparando campo por campo (Nombre, Email, Nivel) entre el registro local y el central.

---

## 4. Operaciones de Pista (Control Operativo)

### POST `/api/logistica/salida`
Registra la salida física de una unidad.
*   **Validaciones**: Triángulo de Disponibilidad (Estudiante, Vehículo, Instructor).
*   **Audit**: Genera una firma digital de la transacción en `audit_logs`.

### POST `/api/logistica/llegada`
Cierra el ciclo operativo.
*   **Cálculo Acumulado**: Calcula el diferencial de tiempo y actualiza las horas de práctica en el expediente local del alumno.

---

## 5. Dashboards y Monitoreo (Mission Control)

### GET `/api/dashboard/clases-activas`
Consumo de la vista persistente `v_clases_activas`. Base para el panel de "Garita de Retorno".

### GET `/api/dashboard/alertas-mantenimiento`
Consumo de la vista `v_alerta_mantenimiento`. Filtra unidades con desperfectos reportados por el instructor o guardia.

---

## 6. Estándares de Respuesta (The Envelope)

Todas las respuestas cumplen con el esquema `ApiResponse<T>`:
```json
{
  "success": true,
  "message": "Operación completada bajo el protocolo Data Shield.",
  "data": { ... },
  "timestamp": "2026-04-13T03:00:00Z"
}
```

### Códigos de Negocio Críticos:
*   `VEHICULO_EN_USO`: La unidad ya ha sido despachada.
*   `ESTUDIANTE_EN_PISTA`: El alumno tiene una salida pendiente sin retorno.
*   `OUT_OF_PARITY`: El registro local ha divergido significativamente de la fuente central.
