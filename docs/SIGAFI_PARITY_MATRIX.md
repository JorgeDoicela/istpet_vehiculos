# Matriz de Paridad Técnica: SIGAFI Core

Este documento define la topología de espejado y las reglas de integridad que rigen el enlace entre el núcleo académico SIGAFI y el ecosistema ISTPET Vehículos.

---

## 1. Filosofía de Mirroring (Espejo Estricto)

El sistema opera bajo una política de **Paridad Atómica**. El objetivo es que la lógica de negocio local actúe sobre datos que son réplicas exactas de la fuente central, minimizando la latencia de red y garantizando la autonomía operativa.

### Reglas de Oro:
*   **Fuente de Verdad (SoT)**: SIGAFI (`sigafi_es`) es el único origen de datos maestros.
*   **Exclusión Selectiva**: Los campos BLOB (`foto`) se excluyen del espejado persistente para optimizar los tiempos de I/O de la base de datos local. El sistema los recupera mediante el **Puente JIT** solo cuando es necesario mostrarlos en el UI.
*   **Encapsulamiento Operativo**: Ninguna tabla espejo debe tener columnas adicionales que no existan en SIGAFI. Los datos locales residen exclusivamente en tablas con el sufijo `_operacion`.

---

## 2. Clasificación de Entidades y Nivel de Paridad

| Capacidad | Nivel de Espejo | Observación Técnica |
| :--- | :--- | :--- |
| **Maestros Académicos** | 1:1 Completo | `carreras`, `periodos`, `cursos`, `secciones`, `modalidades`. |
| **Recursos Humanos** | 1:1 Filtro Activos | `profesores`. Se sincronizan solo cuentas habilitadas. |
| **Estudiantes** | 1:1 JIT Enabled | `alumnos`. Esquema idéntico (sin BLOB). |
| **Control Vehicular** | 1:1 Mirror + Ext | `vehiculos`. Columnas SIGAFI + Extensión `vehiculos_operacion`. |
| **Logística Central** | 1:1 Transaccional | `cond_alumnos_practicas`, `cond_alumnos_vehiculos`. |
| **Horarios y Agendas** | High Volume Mirror | `cond_alumnos_horarios`, `fechas_horarios`. |

---

## 3. El Escudo de Datos (Sync Data Shield)

Durante el proceso de paridad, el `DataSyncService` aplica filtros de protección:
1.  **Sanitización de Strings**: Conversión a formatos compatibles con el cotejamiento `utf8mb4_spanish_ci`.
2.  **Validación de Llaves Foráneas**: Si una matrícula referencia a un periodo inexistente localmente, el sistema activa una **Sincronización en Cascada** del catálogo maestro antes de persistir la matrícula.
3.  **Deduplicación por Clave Única**: Uso de `idAlumno` y `idVehiculo` como anclas de sincronización para evitar duplicidad de registros.

---

## 4. Auditoría de Deriva (Data Drift Audit)

Para asegurar que el espejo no se degrade, el sistema proporciona herramientas de auditoría:
*   **Drift Check**: Comparación de `COUNT(*)` entre SIGAFI y Local.
*   **Inspección Granular**: Comparación campo por campo de registros específicos mediante el endpoint `/api/sync/inspect`.
*   **Health Status**: El dashboard de administración muestra una alerta roja si la paridad de una tabla crítica cae por debajo del 95% de confianza.

---

## 5. Prevención de Inconsistencias

> [!WARNING]
> Cualquier alteración manual de las tablas espejo (Manual SQL Update) en `istpet_vehiculos` será sobrescrita por el motor de **Master Sync** en su siguiente ejecución. Toda persistencia local debe residir en las tablas `_operacion`.
