# Especificación de la API REST — ISTPET Logística

Base URL (desarrollo): la que muestre la consola al ejecutar el backend; suele ser `http://localhost:5112/api` o `http://localhost:5113/api` según `launchSettings.json`.

Swagger UI (desarrollo): `http://localhost:5112/swagger` (o el puerto equivalente).

**Verificación SIGAFI + Master Sync desde Swagger:** ver **[SYNC_VERIFICATION.md](SYNC_VERIFICATION.md)**.

Todas las respuestas utilizan el envelope estándar `ApiResponse<T>`:
```json
{ "success": bool, "message": "string", "data": <T>, "timestamp": "ISO 8601" }
```

---

## Autenticación — `/api/auth`

### POST `/api/auth/login`
Autentica a un usuario del sistema. Soporta dos tipos de hash: **BCrypt** (usuarios migrados de SIGAFI) y **SHA-256** (usuarios nativos del sistema).

**Request Body:**
```json
{
  "usuario": "admin_istpet",
  "password": "istpet2026"
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Ingreso exitoso mediante el Puente de Seguridad Híbrida.",
  "data": {
    "usuario": "admin_istpet",
    "nombre": "Administrador General ISTPET",
    "rol": "admin"
  }
}
```

**Respuesta de Error (401):**
```json
{
  "success": false,
  "message": "Contraseña incorrecta."
}
```

**Roles disponibles:** `admin`, `guardia`, `estacionable`

---

## Logística — `/api/logistica`

Este es el módulo central del sistema.

---

### GET `/api/logistica/estudiante/{cedula}`

Busca un estudiante por número de cédula. Implementa la lógica del **Puente Híbrido Universal**:
1. Búsqueda local en `estudiantes` + `matriculas` (estado ACTIVO).
2. Si no existe, consulta la BD Central SIGAFI (`sigafi_es`).
3. Si lo encuentra en SIGAFI, lo auto-registra localmente y retorna los datos.
4. En ambos casos, consulta si tiene práctica agendada para hoy en SIGAFI (`cond_alumnos_practicas`).
5. Si no hay agenda hoy, consulta si tiene un tutor fijo asignado (`cond_alumnos_vehiculos`).
6. Si el instructor detectado en SIGAFI no existe localmente, el sistema lo **auto-registra programáticamente** (Name Normalization incluido) antes de retornar la respuesta.

**Parámetros de ruta:** `cedula` — Número de cédula del estudiante (10 dígitos).

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Alumno localizado (Local).",
  "data": {
    "idAlumno": "1725555377",
    "estudianteNombre": "DOICELA MOLINA JORGE ISMAEL",
    "nivel": "DESARROLLO DE SOFTWARE CUARTO",
    "idPeriodo": "1",
    "paralelo": "A",
    "jornada": "MATUTINA",
    "tipoLicencia": "C",
    "idTipoLicencia": 1,
    "idMatricula": 5,
    "idPracticaCentral": 12,
    "practicaVehiculo": "#35 (PBA-1234)",
    "practicaInstructor": "TRUJILLO REDROBAN RICHARD MAURICIO",
    "practicaHora": "08:00",
    "tienePracticaHoy": true,
    "fotoBase64": "iVBORw0KGgo..."
  }
}
```

**Respuesta No Encontrado (404):**
```json
{
  "success": false,
  "message": "Estudiante no registrado en la BD Central del ISTPET."
}
```

---

### GET `/api/logistica/vehiculos-disponibles`

Retorna los vehículos OPERATIVOS que **no están actualmente en pista** (determinado consultando la vista `v_clases_activas`). Solo incluye vehículos activos con estado `OPERATIVO`.

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "data": [
    {
      "idVehiculo": 3,
      "numeroVehiculo": 7,
      "vehiculoStr": "PBA-1234 - #7",
      "idInstructorFijo": 2,
      "instructorNombre": "TRUJILLO REDROBAN RICHARD",
      "idTipoLicencia": 1
    }
  ]
}
```

---

### GET `/api/logistica/instructores`

Lista todos los instructores activos, ordenados alfabéticamente.

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "data": [
    { "id_Instructor": 1, "fullName": "APELLIDOS NOMBRES" }
  ]
}
```

---

### GET `/api/logistica/instructor/{cedula}`

Busca un instructor por cédula. Si no existe localmente, consulta la tabla `profesores` de SIGAFI y lo auto-registra.

---

### GET `/api/logistica/buscar?query={term}`

Búsqueda predictiva de estudiantes (autocomplete). Requiere mínimo 3 caracteres. Busca por cédula, nombres o apellidos. Retorna máximo 5 resultados.

**Parámetros de query:** `query` — Texto de búsqueda (mínimo 3 caracteres).

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "data": [
    {
      "idAlumno": "1725555377",
      "nombreCompleto": "DOICELA MOLINA JORGE",
      "carrera": "CURSO PROFESIONAL TIPO C"
    }
  ]
}
```

---

### POST `/api/logistica/salida`

Registra la salida de un vehículo. Ejecuta las siguientes validaciones **dentro de una transacción de base de datos**:
1. Vehículo existe, está activo y en estado `OPERATIVO`.
2. Vehículo no está actualmente en pista (sin `registros_llegada` asociado).
3. Instructor no está ocupado en otra práctica simultánea.
4. Estudiante no está ya en pista con esta matrícula.

**Request Body:**
```json
{
  "idMatricula": 5,
  "idVehiculo": 3,
  "idInstructor": 1,
  "observaciones": "Salida normal",
  "registradoPor": 1
}
```

**Respuesta Exitosa (200):**
```json
{ "success": true, "message": "Salida registrada con éxito.", "data": "EXITO" }
```

**Respuesta de Conflicto (400) — ejemplos de errores de negocio:**
- `"VEHICULO_EN_USO"` — El vehículo ya está en pista.
- `"INSTRUCTOR_OCUPADO"` — El instructor tiene clase activa.
- `"ESTUDIANTE_EN_PISTA"` — El estudiante ya salió con este curso.

---

### POST `/api/logistica/llegada`

Registra el retorno de un vehículo. Automáticamente **calcula y acumula las horas de práctica** completadas en la `matricula` del estudiante.

**Request Body:**
```json
{
  "idRegistro": 15,
  "observaciones": "Retorno sin novedad",
  "registradoPor": 1
}
```

**Respuesta Exitosa (200):**
```json
{ "success": true, "message": "Llegada registrada con éxito.", "data": "EXITO" }
```

---

## Dashboard — `/api/Dashboard`

### GET `/api/dashboard/clases-activas`

Consulta la vista SQL `v_clases_activas` para obtener los vehículos que están actualmente en pista (salida sin llegada registrada).

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "data": [
    {
      "id_Registro": 15,
      "id_Vehiculo": 3,
      "cedula": "1725555377",
      "estudiante": "JORGE DOICELA",
      "placa": "PBA-1234",
      "numeroVehiculo": 7,
      "instructor": "RICHARD TRUJILLO",
      "salida": "2026-04-07T08:00:00"
    }
  ]
}
```

---

### GET `/api/dashboard/alertas-mantenimiento`

Consulta la vista SQL `v_alerta_mantenimiento` retornando vehículos con estado `MANTENIMIENTO`.

### GET `/api/Dashboard/agenda-reciente?limit=100`

Agenda reciente (servicio `IAgendaPanelService`): lectura desde SIGAFI (`cond_alumnos_practicas`), estado desde `cancelado` / `ensalida` / `hora_llegada` y, si hay fila en el espejo local, override con `Practicas` local. Si SIGAFI no devuelve filas, se usa el espejo local. Roles: `admin`, `logistica`, `guardia`.

---

## Sincronización — `/api/Sync`

> Rutas con prefijo **`/api/Sync`** (ASP.NET acepta mayúsculas/minúsculas). Requieren rol **`admin`** salvo que se indique lo contrario.

### GET `/api/Sync/ping-sigafi`

Comprueba que `SigafiConnection` alcanza la base `sigafi_es`. **200** si la conexión responde; **503** si no hay conectividad o credenciales incorrectas.

### GET `/api/Sync/sigafi-probe`

Ejecuta las mismas lecturas que usa el Master Sync (por módulo) **sin escribir** en `istpet_vehiculos`. Devuelve `rowCount` y `ok` por tabla/consulta y una prueba de detalle de alumno (`sampleAlumnoDetailOk`). Sirve para validar extracción antes de un sync masivo.

### POST `/api/Sync/master`

Ejecuta la sincronización integral desde SIGAFI hacia el espejo local. Respuesta tipo `SyncLog`: `estado` (`OK` / `ADVERTENCIA`), `registrosProcesados`, `registrosFallidos`. El contador de procesados es la suma por pasos del pipeline, no necesariamente la suma de los `rowCount` del probe.

### POST `/api/Sync/instructors`

Sincroniza instructores desde SIGAFI hacia la BD local.

### POST `/api/Sync/students`

Recibe un arreglo JSON de estudiantes externos y los ingesta con protección del **Data Shield**. Válida cada registro, rechaza cédulas inválidas, limpia nombres con caracteres especiales y persiste los nuevos estudiantes. Retorna un `SyncLog` con el resultado.

**Request Body:** `[{ "id_externo": "1725555377", "nombre_completo": "Jorge Rodriguez", "correo_universidad": "jorge@istpet.edu" }, ...]`

Si se envía el arreglo vacío, ejecuta un ciclo de demostración con datos de prueba predefinidos (incluyendo un dato válido, uno con cédula inválida y uno con nombre malformado).

**Flujo recomendado en Swagger:** login → Authorize `Bearer` → `ping-sigafi` → `sigafi-probe` → `master`. Detalle en **[SYNC_VERIFICATION.md](SYNC_VERIFICATION.md)**.

---

## Catálogos

### GET `/api/vehiculos` — Lista todos los vehículos

### GET `/api/estudiantes` — Lista todos los estudiantes

### GET `/api/tipoliciencia` (implícito en dominio) — Catálogo de tipos de licencia (C, D, E)
