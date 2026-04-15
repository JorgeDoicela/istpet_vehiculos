# Guía de Operaciones y Despliegue en Producción

Este documento define los estándares de despliegue para el sistema ISTPET Vehículos, garantizando la **Alta Disponibilidad** y el **Hardening de Seguridad** en entornos operativos reales.

---

## 1. Escenario A: Despliegue On-Premise (Infraestructura Local)

Ideal para servidores internos dentro de la red del instituto.

### 1.1. Orquestación con Docker Compose
El sistema utiliza contenedores optimizados para MariaDB y .NET 8.

```bash
# Sincronización de activos
git clone https://github.com/JorgeDoicela/istpet_vehiculos.git /opt/istpet_logistica
cd /opt/istpet_logistica

# Inicialización de Entorno Seguro
# Genere una llave JWT de 32+ caracteres
openssl rand -base64 48 > .env_jwt_key

# Despliegue Modo Directo (BDs existentes en servidor)
docker compose up -d --build

# Despliegue Modo Espejo (levanta MariaDB local del compose)
docker compose --profile mirror up -d --build
```

### 1.2. El Protocolo de Auto-Reparación
Al iniciar, el contenedor del backend ejecutará el **Schema Healer**. No es necesario importar archivos SQL manualmente si las cadenas de conexión son correctas. El sistema detectará el estado de la base de datos y la elevará a la versión actual automáticamente.

---

## 3. Escenario C: Integración Directa (Escritura en SIGAFI)

Este modo se activa mediante `DATABASE_MODE=Direct` y permite al sistema operar directamente sobre la base de datos de producción de SIGAFI sin necesidad de una base de datos local.

### 3.1. Arquitectura de Escritura Directa
*   **Conexión Única**: Tanto la lectura como la escritura se realizan en el servidor de SIGAFI.
*   **Tablas Operativas**: El sistema creará automáticamente las tablas `vehiculos_operacion` y `audit_logs` dentro de la base de datos de SIGAFI.

### 3.2. Escudo de Protección DDL
> [!IMPORTANT]
> Cuando el sistema opera en modo `Direct`, el **Schema Healer** activa un filtro de seguridad proactivo que **bloquea** cualquier comando `CREATE` o `ALTER` sobre las tablas maestras de SIGAFI (`alumnos`, `profesores`, `matriculas`, `vehiculos`, etc.). Esto garantiza la integridad absoluta de la base de datos central.

### 3.3. Optimización de Red
En este modo, el sistema desactiva automáticamente el servicio de sincronización masiva y las escrituras redundantes durante el inicio de sesión, reduciendo la carga sobre el servidor institucional.

---


## 3. Seguridad y Hardening de Producción

### 3.1. Protección de Secretos
> [!CAUTION]
> Nunca deje la `JWT_KEY` por defecto. En producción, el sistema rechazará llaves de menos de 256 bits (32 chars) por motivos de seguridad criptográfica.

### 3.2. Restricción CORS
En el archivo `Program.cs` de producción, asegúrese de que `AllowedOrigins` esté limitado estrictamente al dominio de Vercel (ej: `https://logistica-istpet.vercel.app`).

---

## 4. Mantenimiento y Logs Industrial

*   **Log Forwarding**: El backend genera logs estructurados. Se recomienda usar `docker compose logs -f backend` para monitorear el **Escudo de Datos** en tiempo real.
*   **Audit Trail**: La tabla `audit_logs` debe ser monitoreada semanalmente para detectar patrones de acceso inusuales o intentos de fuerza bruta en el **Control Hub**.
