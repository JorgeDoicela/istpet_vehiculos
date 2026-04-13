# Guía de Instalación: Sistema de Logística ISTPET

Esta guía detalla el despliegue del ecosistema de logística vehicular. El sistema cuenta con **Protocolos de Auto-Configuración (Schema Healer)** que simplifican drásticamente la puesta en marcha.

---

## 1. Requisitos Previos (Ecosistema 2026)

*   **Runtime**: .NET 8.0 SDK y Node.js 20+ (LTS).
*   **Persistencia**: MySQL 8.0+ o MariaDB 11+.
*   **Conectividad**: Acceso a la base de datos central SIGAFI (o uso del entorno Mock).

---

## 2. Despliegue del Backend (.NET 8)

### 2.1. Configuración de Entorno (.env)
El sistema utiliza un sistema de carga nativa de variables de entorno. Cree un archivo `.env` en la raíz del proyecto basándose en el siguiente ejemplo:

```bash
# MODO: Mirror (Sincronización local) | Direct (Escritura en SIGAFI)
DATABASE_MODE=Mirror

# BD LOCAL (istpet_vehiculos)
LOCAL_DB_HOST=localhost
LOCAL_DB_PORT=3306
LOCAL_DB_NAME=istpet_vehiculos
LOCAL_DB_USER=root
LOCAL_DB_PASS=****

# BD SIGAFI (Producción)
REMOTE_SIGAFI_SERVER=192.168.7.50
REMOTE_SIGAFI_PORT=3306
REMOTE_SIGAFI_DB=sigafi_es
REMOTE_SIGAFI_USER=root
REMOTE_SIGAFI_PASS=****

# SEGURIDAD
JWT_KEY=UNA_LLAVE_DE_AL_MENOS_32_CARACTERES
```

### 2.2. Conector Inteligente
El backend detecta automáticamente las variables del `.env` y construye la cadena de conexión. No es necesario escribir la URI completa a menos que se use la variable `DATABASE_URL` para sobrescribir el comportamiento por defecto.


### 2.2. El Protocolo Schema Healer (Auto-Run)
A diferencia de sistemas tradicionales, **no es obligatorio ejecutar scripts SQL manualmente**.
Al ejecutar `dotnet run`, el sistema activará el **Schema Healer**:
1.  Detectará si la base de datos existe.
2.  Creará automáticamente las 32 tablas y 4 vistas críticas.
3.  Inyectará los catálogos de licencias y el usuario administrador por defecto.

**Credenciales de Primer Acceso**:
*   **Usuario**: `admin_istpet`
*   **Password**: `istpet2026`

---

## 3. Despliegue del Frontend (React 19)

### 3.1. Instalación de Dependencias
```bash
cd frontend
npm install
```

### 3.2. Configuración de API Endpoint
Edite `.env` o el archivo de configuración de API:
```env
VITE_API_URL=http://localhost:5112/api
```

### 3.3. Ejecución
```bash
npm run dev
```

---

## 4. Verificación de la Matriz de Paridad

Una vez que ambos servicios estén en línea:
1.  Acceda a `/swagger` en el backend.
2.  Autentíquese mediante `POST /api/auth/login`.
3.  Ejecute `POST /api/sync/master` para realizar la primera sincronización masiva desde SIGAFI.
4.  Si no tiene una base SIGAFI real, ejecute el script `docs/Scripts/MOCK_SIGAFI_ES.sql` antes del paso anterior.

---

## 5. Solución de Problemas de Instalación

*   **Error de SSL**: Si usa TiDB Cloud o MySQL en la nube, asegúrese de incluir `SslMode=VerifyFull` y el certificado CA en la carpeta del backend.
*   **JWT Key Error**: El sistema se cerrará instantáneamente si la llave JWT tiene menos de 32 caracteres (256 bits). Es un mecanismo de protección proactivo.
*   **CORS Block**: Verifique que el puerto del frontend coincida con la política permitida en el `Program.cs` del backend.
