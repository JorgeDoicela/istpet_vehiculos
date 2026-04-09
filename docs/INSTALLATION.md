# Guía de Instalación y Configuración — ISTPET Logística

## Requisitos del Sistema

| Componente | Versión Mínima | Notas |
| :--- | :--- | :--- |
| .NET SDK | 8.0 | [Descargar](https://dotnet.microsoft.com/download/dotnet/8) |
| Node.js | 20 LTS | Para el frontend |
| MySQL / MariaDB | 8.0 / 11+ | Servidor local o remoto |
| Navegador | Moderno | Chrome, Edge, Firefox |

---

## Paso 1: Clonar el Repositorio

```bash
git clone https://github.com/JorgeDoicela/istpet_vehiculos.git
cd istpet_vehiculos
```

---

## Paso 2: Configurar la Base de Datos

### 2.1 Crear la Base de Datos Principal

Abrir MySQL Workbench o el cliente de línea de comandos y ejecutar:

```bash
mysql -u root -p < docs/Scripts/SQL_SCHEMA.sql
```

Esto crea la base de datos `istpet_vehiculos` con sus tablas core sincronizadas con SIGAFI (alumnos, profesores, matriculas, cond_alumnos_practicas), el usuario administrador inicial y los catálogos base.

**Credenciales iniciales:**
| Campo | Valor |
| :--- | :--- |
| Usuario | `admin_istpet` |
| Contraseña | `istpet2026` |

### 2.2 (Opcional) Simular la Base de Datos Central SIGAFI

Si no tienes acceso al servidor real de SIGAFI, ejecuta el script de simulación para desarrollo:

```bash
mysql -u root -p < docs/Scripts/MOCK_SIGAFI_ES.sql
```

Este script crea la base de datos `sigafi_es` con datos de prueba, incluyendo un alumno, un profesor, un vehículo y una práctica agendada para hoy.

---

## Paso 3: Configurar el Backend

### 3.1 Cadena de Conexión

Editar `backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=istpet_vehiculos;User=root;Password=TU_PASSWORD;"
  }
}
```

**Parámetros de la cadena de conexión:**

| Parámetro | Descripción | Ejemplo |
| :--- | :--- | :--- |
| `Server` | Hostname del servidor MySQL | `localhost` o `192.168.1.5` |
| `Database` | Nombre de la BD principal | `istpet_vehiculos` |
| `User` | Usuario MySQL | `root` o `istpet_user` |
| `Password` | Contraseña del usuario MySQL | `****` |
| `Port` | Puerto (omitir si es el default 3306) | `3307` |

### 3.2 Ejecutar el Backend

```bash
cd backend
dotnet restore
dotnet run
```

La API estará disponible en el puerto que indique la consola al arrancar. En desarrollo, `backend/Properties/launchSettings.json` define por defecto **HTTP `5112`** o **`5113`** (perfil `https-alt`). Ejemplos:

- API: `http://localhost:5112`
- Swagger UI: `http://localhost:5112/swagger`

Si tu entorno usa otro puerto (por ejemplo `5000`), usa esa URL en el navegador y en el frontend.

> En producción, usar `dotnet publish -c Release` y servir con IIS, nginx o como servicio de Windows.

Para validar **lectura de SIGAFI** y **Master Sync** desde Swagger y con SQL, sigue **[SYNC_VERIFICATION.md](SYNC_VERIFICATION.md)**.

---

## Paso 4: Configurar el Frontend

### 4.1 URL de la API

La URL base de la API se configura en `frontend/src/services/api.js`. Por defecto apunta a `http://localhost:5000/api`.

Si el backend corre en un puerto diferente, editar ese archivo:

```javascript
// frontend/src/services/api.js
const api = axios.create({
  baseURL: 'http://localhost:5000/api',  // Cambiar aquí
});
```

### 4.2 Instalar y Ejecutar

```bash
cd frontend
npm install
npm run dev
```

La interfaz estará disponible en `http://localhost:5173`.

---

## Paso 5: Verificar la Instalación

1. Abrir `http://localhost:5173` en el navegador.
2. Aparecerá la pantalla de **Control Operativo**.
3. Las listas de vehículos e instructores deben cargarse desde el menú de Salida.
4. Abrir Swagger en el puerto del backend (p. ej. `http://localhost:5112/swagger`) y comprobar que la API responde.
5. Probar el endpoint `GET /api/dashboard/clases-activas` — debe devolver `[]` si no hay clases activas.

**Conexión SIGAFI y espejo:** guía paso a paso con `ping-sigafi`, `sigafi-probe`, `POST /api/Sync/master` y consultas SQL en **[SYNC_VERIFICATION.md](SYNC_VERIFICATION.md)**.

---

## Estructura de Archivos de Configuración

| Archivo | Propósito |
| :--- | :--- |
| `backend/appsettings.json` | Configuración base (connection string, logging) |
| `backend/appsettings.Development.json` | Sobrescritura para entorno de desarrollo |
| `frontend/vite.config.js` | Configuración del servidor de desarrollo de Vite |
| `frontend/tailwind.config.js` | Configuración de Tailwind CSS |

---

## Solución de Problemas Comunes

### Error: "Cannot connect to database"
- Verificar que el servicio de MySQL esté corriendo.
- Comprobar usuario y contraseña en `appsettings.json`.
- Asegurarse de que la base de datos `istpet_vehiculos` fue creada ejecutando el script SQL.

### Error: "Table 'alumnos' doesn't exist"
Asegúrese de haber ejecutado `SQL_SCHEMA.sql`. En la versión 2026, la tabla de estudiantes se llama `alumnos` para mantener la paridad absoluta con el sistema central.

### Error CORS en el navegador
Verificar que el backend está corriendo antes de acceder al frontend. La configuración CORS en `Program.cs` permite todos los orígenes en desarrollo.

### El widget "Agenda SIGAFI Hoy" no carga datos
Si no se instaló `MOCK_SIGAFI_ES.sql`, la BD `sigafi_es` no existe. El sistema lo maneja sin error — simplemente mostrará la lista vacía. Para habilitar esta funcionalidad, ejecutar el script de simulación o conectarse al servidor real de SIGAFI.

### Las fotos de los alumnos no cargan
La foto solo se muestra si el alumno viene del puente SIGAFI (que incluye `TO_BASE64(a.foto)`). Alumnos registrados localmente no tienen foto.
