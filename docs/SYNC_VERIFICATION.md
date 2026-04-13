# Protocolo de Verificación e Integridad (Sync Data Shield)

Este documento define los procedimientos técnicos para garantizar la **Matriz de Paridad** entre el núcleo académico SIGAFI y el espejo operativo local. El sistema utiliza el motor **Master Sync** junto con el **Escudo de Datos** para asegurar una ingesta limpia y resiliente.

---

## 1. Arquitectura de Verificación

El proceso de validación se divide en tres capas de seguridad:

1.  **Capa de Alivio (Probe)**: Verificación de lectura sin compromiso de escritura.
2.  **Capa de Ingesta (Master Sync)**: Ejecución del motor de sincronización con reglas de sanitización.
3.  **Capa de Auditoría (Parity Audit)**: Comparación estadística y granular de registros.

---

## 2. Procedimiento de Verificación JIT (Just-In-Time)

### 2.1. Comprobación de Enlace (Ping)
**Endpoint**: `GET /api/Sync/ping-sigafi`
Valida la conectividad física y de red entre el servidor de la aplicación y el clúster de base de datos SIGAFI.

### 2.2. Sonda de Extracción (Sigafi Probe / Extraction Probe)
**Endpoint**: `GET /api/Sync/sigafi-probe`
Este servicio ejecuta las 20+ consultas de extracción de `SqlCentralStudentProvider` de forma aislada.
*   **Módulos Verificados**: Alumnos, Profesores, Vehículos, Matrículas, Prácticas, Horarios, Carreras, etc.
*   **Métricas de Sonda**: Devuelve `rowCount` y `sample` para cada entidad, permitiendo detectar cambios en el esquema de SIGAFI antes de una carga real.

---

## 3. El Motor Master Sync (Sync Engine)

**Endpoint**: `POST /api/Sync/master`
Ejecuta la orquestación masiva de paridad. Sigue el **Escudo de Datos (Data Shield)**:

*   **Sanitización Automática**: El motor limpia caracteres especiales y trunca cadenas que exceden los límites del esquema local (Ej: chasis > 50 chars).
*   **Deduplicación Híbrida**: Los vehículos se agrupan por placa e ID para evitar duplicidad de unidades en la flota local.
*   **Cierre de Integridad**: Una práctica solo se sincroniza si sus dependencias (Estudiante, Vehículo, Instructor) han superado el escudo de datos previamente.

### Resultado de Ingesta:
```json
{
  "estado": "OK",
  "registrosProcesados": 45120,
  "registrosFallidos": 0,
  "mensaje": "Master Sync completado: 13 módulos alineados."
}
```

---

## 4. Auditoría de Paridad (Data Parity Audit)

Para garantizar que el espejo local es una copia fiel a nivel operacional, el sistema cuenta con herramientas de auditoría independientes:

### 4.1. Auditoría Estadística
**Endpoint**: `GET /api/Sync/audit` (Público/Diagnóstico)
Compara masivamente los conteos entre SIGAFI y `istpet_vehiculos` para las 13 entidades críticas.

### 4.2. Inspección Granular (Parity Inspection)
**Endpoints**:
*   `GET /api/Sync/inspect/student/{idAlumno}`
*   `GET /api/Sync/inspect/instructor/{idProfesor}`
*   `GET /api/Sync/inspect/vehicle/{placa}`

Esta herramienta realiza un **Deep Mapping** comparando campo por campo. Si detecta un desajuste (ej: cambio de email en SIGAFI no reflejado localmente), el inspector marcará el registro como "Out of Parity" y listará los campos discordantes.

---

## 5. Mantenimiento del Espejo (Background Service)

La paridad se mantiene de forma autónoma mediante `SigafiMirrorBackgroundService`:
*   **Startup Delay**: Evita colisiones de red al iniciar el contenedor.
*   **Periodic Tick**: Ejecuta el `MasterSync` cada N minutos (configurable).
*   **Locking System**: Evita ejecuciones paralelas que podrían causar *deadlocks* en la base de datos local.

---

## 6. Resolución de Conflictos de Paridad

Si un registro no se sincroniza:
1.  **Validar Formato**: Cédulas de menos de 10 dígitos o IDs nulos son rechazados por el **Data Shield**.
2.  **Verificar Dependencias**: Asegúrese de que el catálogo raíz (Periodos/Carreras) esté sincronizado antes que las matrículas.
3.  **Logs de Diagnóstico**: Revise el log del servidor para detectar "Data Truncation Error" o "Foreign Key Constraint" que el escudo no haya podido auto-resolver.
