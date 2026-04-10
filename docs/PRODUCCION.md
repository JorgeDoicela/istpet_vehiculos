# Guía de despliegue a producción

Tres escenarios cubiertos. Elige el que aplique a tu caso.

---

## Opción A — Servidor institucional (Docker Compose, red interna)

Es el caso más simple: todo corre en `192.168.7.50` con Docker.  
SIGAFI está en la misma red, por lo que la conexión es directa.

### Requisitos previos
- Docker + Docker Compose instalados en el servidor
- Puerto 80 (frontend) y 5112 (API) disponibles
- MariaDB/MySQL de SIGAFI accesible desde el servidor en `192.168.7.50:3307`

### Pasos

```bash
# 1. Clonar/copiar el proyecto en el servidor
git clone <repo> /opt/istpet_vehiculos
cd /opt/istpet_vehiculos

# 2. Crear el .env desde el ejemplo
cp .env.example .env

# 3. Editar .env con los valores del servidor
#    Los valores ya están pre-configurados para 192.168.7.50.
#    Solo cambia JWT_KEY por una clave segura real:
#    openssl rand -base64 48
nano .env

# 4. Levantar todo
docker compose up -d --build

# 5. Verificar que los servicios están sanos
docker compose ps
curl http://localhost:5112/health
```

### Verificar primer arranque
```bash
# Ver logs del backend
docker compose logs backend -f

# El backend ejecuta Master Sync al arrancar (StartupDelaySeconds=30).
# Deberías ver: "Espejo SIGAFI: OK. Registros procesados: ..."
```

### Actualizar después de un cambio de código
```bash
git pull
docker compose up -d --build
```

---

## Opción B — Render (API) + Vercel (Frontend) + TiDB Cloud (BD)

Para acceso público desde internet. SIGAFI debe ser accesible desde Render
(IP pública en SIGAFI o túnel; ver nota al final).

### 1. TiDB Cloud — Base de datos

1. Crear cuenta en [tidbcloud.com](https://tidbcloud.com) y crear un clúster.
2. Obtener la cadena de conexión (formato MySQL-compatible).
3. Conectarse con un cliente MySQL y ejecutar:
   ```sql
   source docs/Scripts/01_ISTPET_LOGISTICS_SCHEMA.sql
   ```
4. La cadena tendrá el formato:
   ```
   Server=gateway01.xxx.tidbcloud.com;Port=4000;Database=istpet_vehiculos;Uid=xxx;Pwd=xxx;SslMode=Required;
   ```
   El `SslMode=Required` lo añade automáticamente el backend si detecta `tidbcloud.com`.

### 2. Render — API .NET

1. Crear un nuevo **Web Service** en [render.com](https://render.com).
2. Conectar el repositorio de GitHub.
3. Configurar:
   - **Root directory:** `backend`
   - **Dockerfile path:** `Dockerfile`
   - **Environment:** Docker
4. Agregar estas **variables de entorno** en el panel de Render:

| Variable | Valor |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena TiDB → `istpet_vehiculos` |
| `ConnectionStrings__SigafiConnection` | Cadena TiDB → `sigafi_es` (réplica) o IP pública SIGAFI |
| `JWT_KEY` | Clave secreta (mínimo 32 chars). Generar con `openssl rand -base64 48` |
| `CORS_ALLOWED_ORIGINS` | URL de Vercel, p.ej. `https://istpet.vercel.app` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `SigafiMirrorSync__Enabled` | `true` (o `false` si SIGAFI no es alcanzable desde Render) |

5. Hacer deploy. La URL será algo como `https://istpet-api.onrender.com`.

### 3. Vercel — Frontend React

1. Importar el repositorio en [vercel.com](https://vercel.com).
2. Configurar:
   - **Root directory:** `frontend`
   - **Framework preset:** Vite
   - **Build command:** `npm run build`
   - **Output directory:** `dist`
3. Agregar variable de entorno:

| Variable | Valor |
|---|---|
| `VITE_API_URL` | `https://istpet-api.onrender.com/api` |

4. Hacer deploy. El `vercel.json` ya tiene las reescrituras para React Router.

### Verificar el despliegue en la nube

```bash
# Health check de la API
curl https://istpet-api.onrender.com/health

# Primer sync manual (necesita token de admin)
curl -X POST https://istpet-api.onrender.com/api/Sync/master \
  -H "Authorization: Bearer TU_TOKEN"
```

---

## Nota sobre SIGAFI desde Render/internet

SIGAFI (`192.168.7.50`) es una IP privada de red interna. Render **no puede** alcanzarla directamente.  
Tienes tres alternativas:

| Alternativa | Complejidad | Recomendación |
|---|---|---|
| Cloudflare Tunnel | Baja | Instalar `cloudflared` en el servidor ISTPET y crear un túnel TCP privado. Gratis. |
| IP pública + firewall | Media | Abrir puerto MySQL solo para IPs de Render con whitelist. |
| Réplica en TiDB | Alta | Exportar `sigafi_es` periódicamente con `mysqldump` y cargarlo en TiDB. |

Para uso en red interna (Opción A) esto no aplica ya que todo está en la misma red.

---

## Cambiar la JWT_KEY (obligatorio antes de producción real)

```bash
# Generar una clave segura
openssl rand -base64 48
# Ejemplo de salida: 3K9mQr7Xv2...

# Copiar esa salida y pegarla en:
# - .env → JWT_KEY=...  (para servidor)
# - Variables de Render (para nube)
```

> **IMPORTANTE:** Cambiar la JWT_KEY invalida todos los tokens existentes.  
> Todos los usuarios deberán volver a iniciar sesión.

---

## Estructura de scripts SQL

| Script | Propósito |
|---|---|
| `docs/Scripts/01_ISTPET_LOGISTICS_SCHEMA.sql` | Crea toda la BD `istpet_vehiculos` (tablas + `audit_logs` + datos iniciales). **Único script necesario.** |
| `docs/Scripts/02_SYNC_SIGAFI_DATA.sql` | Datos de ejemplo / seed para desarrollo. No ejecutar en producción real. |
