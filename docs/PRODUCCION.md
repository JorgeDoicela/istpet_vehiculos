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

# Despliegue Atómico
docker compose up -d --build
```

### 1.2. El Protocolo de Auto-Reparación
Al iniciar, el contenedor del backend ejecutará el **Schema Healer**. No es necesario importar archivos SQL manualmente si las cadenas de conexión son correctas. El sistema detectará el estado de la base de datos y la elevará a la versión actual automáticamente.

---

## 2. Escenario B: Despliegue en la Nube (Arquitectura Distribuida)

Para accesibilidad global con redundancia.

### 2.1. Persistencia (TiDB Cloud)
*   **SSL Enforced**: La conexión hacia TiDB Cloud utiliza Triple-DES/AES con certificados SSL gestionados automáticamente por el backend.
*   **Modo Réplica**: Se recomienda mantener una base `sigafi_es` en el mismo clúster para minimizar la latencia del **Master Sync**.

### 2.2. Backend (Render / PaaS)
*   **Docker Context**: Utilice la carpeta `backend/` como raíz de construcción.
*   **Health Check Endpoint**: `/health`. Render monitorizará este endpoint para garantizar que el servicio esté respondiendo antes de promover el despliegue.

### 2.3. Frontend (Vercel)
*   **Edge Optimization**: El frontend se sirve desde nodos de borde (CDN) para garantizar latencia mínima en carga de activos pesados y modo oscuro.

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
