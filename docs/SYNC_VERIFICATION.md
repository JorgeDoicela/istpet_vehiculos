# Verificación de SIGAFI, Master Sync y espejo local

Guía operativa para comprobar que la API **lee bien `sigafi_es`** y **rellena `istpet_vehiculos`**, usando **Swagger** y consultas SQL opcionales.

---

## Qué estás validando

| Conexión | Base de datos | Rol |
| :--- | :--- | :--- |
| `ConnectionStrings:SigafiConnection` | `sigafi_es` | Solo lectura: extracción de datos académicos y operativos de SIGAFI. |
| `ConnectionStrings:DefaultConnection` | `istpet_vehiculos` | Lectura y escritura: espejo + prácticas, exámenes y datos que la app genera. |

El **Master Sync** copia desde SIGAFI hacia el espejo local; la app **no escribe** en `sigafi_es`.

---

## Requisitos previos

1. Esquema local creado (por ejemplo `docs/Scripts/01_ISTPET_LOGISTICS_SCHEMA.sql`).
2. En `backend/appsettings.json` (o variables de entorno) configuradas **ambas** cadenas: `DefaultConnection` y `SigafiConnection`, apuntando al servidor correcto (host, puerto, usuario, contraseña).
3. API en ejecución (`dotnet run` desde `backend`).
4. Un usuario con rol **`admin`** en `usuarios_web` (para JWT en los endpoints de Sync).

---

## Abrir Swagger y el puerto

Swagger UI en desarrollo suele ser:

- `http://localhost:5112/swagger` (perfil `http` / `https` en `backend/Properties/launchSettings.json`), o  
- `http://localhost:5113/swagger` (perfil `https-alt`).

El puerto exacto lo muestra la consola al arrancar el backend. Si usas otro host/puerto, sustituye en las URLs de esta guía.

---

## Paso 1: Obtener token JWT (Swagger)

1. En Swagger, localiza **`POST /api/Auth/login`** (o la ruta de login que exponga tu build).
2. Ejecuta el cuerpo con credenciales de un usuario **admin**, por ejemplo:

```json
{
  "usuario": "administrador",
  "password": "tu_contraseña"
}
```

3. Copia el **token** del campo correspondiente en la respuesta (según el formato de tu `ApiResponse`).

---

## Paso 2: Autorizar en Swagger

1. Pulsa **Authorize** (candado).
2. Escribe: `Bearer <pega_aquí_el_token>` (deja un espacio después de `Bearer`).
3. Confirma y cierra el diálogo.

A partir de aquí, las peticiones a endpoints protegidos irán con el JWT.

---

## Paso 3: Comprobar conexión a SIGAFI

**`GET /api/Sync/ping-sigafi`**

- **200** con mensaje de conexión OK → la API alcanza `sigafi_es` con `SigafiConnection`.
- **503** → revisar red, firewall, credenciales y que la base `sigafi_es` exista en ese servidor/puerto.

---

## Paso 4: Probar todas las extracciones (sin escribir en local)

**`GET /api/Sync/sigafi-probe`**

Ejecuta las **mismas lecturas** que usa el Master Sync (cursos, alumnos, matrículas válidas, vehículos, prácticas, horarios, etc.) y devuelve, por módulo:

| Campo | Significado |
| :--- | :--- |
| `ok` | `true` si el SELECT terminó sin error. |
| `rowCount` | Filas devueltas por esa consulta. |
| `error` | Mensaje de error si `ok` es `false`. |

Además:

- **`sampleAlumnoId`** / **`sampleAlumnoDetailOk`**: toma un alumno de la lista y llama a la lógica de detalle (`GetFromCentralAsync`). Si `sampleAlumnoDetailOk` es `true`, el puente de alumno + matrícula responde bien para al menos un caso.

**Interpretación rápida**

- Todos los módulos `ok: true` y conteos razonables → extracción desde SIGAFI coherente.
- Un módulo con `ok: false` → revisar el mensaje y alinear consultas en `SqlCentralStudentProvider` con el esquema real de SIGAFI.
- `rowCount: 0` en una tabla que en SIGAFI sí tiene datos → revisar filtros (por ejemplo matrículas válidas, periodos activos).

---

## Paso 5: Sincronizar espejo local (Master Sync)

**`POST /api/Sync/master`**

- Cuerpo vacío está bien (no suele llevar parámetros).
- Respuesta típica exitosa: `estado` **OK**, `registrosFallidos` **0**.

**`registrosProcesados`**

- Es la **suma de filas procesadas** en cada paso del sync (insert/update/saltos según reglas), **no** la suma directa de todos los `rowCount` del probe.
- Es normal que no coincida con la suma de módulos del probe; ambos indicadores son útiles por separado.

Si `estado` es **ADVERTENCIA**, el mensaje indica qué módulos fallaron u omitieron; revisar logs del servidor y el detalle en `SyncLog`.

---

## Paso 6: Comprobar en MySQL (opcional pero muy claro)

### Conteo de alumnos

```sql
SELECT COUNT(*) AS alumnos_sigafi FROM sigafi_es.alumnos;
SELECT COUNT(*) AS alumnos_local FROM istpet_vehiculos.alumnos;
```

Si los totales coinciden (como en una verificación reciente con **47 143** en ambas), el espejo de **alumnos** está alineado en volumen.

### Misma persona en las dos bases

Sustituye la cédula por una real:

```sql
USE sigafi_es;
SELECT idAlumno, primerNombre, segundoNombre, apellidoPaterno, apellidoMaterno, email, celular
FROM alumnos
WHERE idAlumno = '0101675205';

USE istpet_vehiculos;
SELECT idAlumno, primerNombre, segundoNombre, apellidoPaterno, apellidoMaterno, email, celular
FROM alumnos
WHERE idAlumno = '0101675205';
```

Los datos de negocio relevantes deberían coincidir (salvo normalizaciones como mayúsculas).

### Alumnos en SIGAFI que no están en local

Debería devolver **0 filas** tras un Master Sync completo correcto:

```sql
SELECT s.idAlumno
FROM sigafi_es.alumnos s
LEFT JOIN istpet_vehiculos.alumnos l ON l.idAlumno = s.idAlumno
WHERE l.idAlumno IS NULL
LIMIT 50;
```

---

## Sincronización automática en segundo plano

En `appsettings.json`, la sección **`SigafiMirrorSync`** controla si el servicio hospedado ejecuta `MasterSyncAsync` al arrancar y cada ciertos minutos. Si está desactivada (`Enabled: false`), puedes depender solo de **`POST /api/Sync/master`** manual desde Swagger u otro cliente.

---

## Seguridad

- **No compartas** el JWT en chats, capturas públicas ni repositorios. Si se filtra, inicia sesión de nuevo y considera rotar credenciales si aplica.
- En producción, use HTTPS y restrinja CORS; la política permisiva es propia de desarrollo.

---

## Referencias en el código

| Pieza | Ubicación |
| :--- | :--- |
| Endpoints Sync | `backend/Controllers/SyncController.cs` |
| Master Sync | `backend/Services/Implementations/DataSyncService.cs` → `MasterSyncAsync` |
| Lecturas SIGAFI | `backend/Services/Implementations/SqlCentralStudentProvider.cs` |
| Probe sin escritura local | `backend/Services/Implementations/SigafiExtractionProbe.cs` |
| Job periódico | `backend/Hosting/SigafiMirrorBackgroundService.cs` |

Para la especificación breve de rutas REST, ver también **[API_SPEC.md](API_SPEC.md)** (sección Sincronización).
