# Protocolo de Seguridad y Protección de Datos: Escudo de Datos (Data Shield)

Este documento detalla las capas de seguridad de grado industrial implementadas en el sistema ISTPET Vehículos para proteger la integridad del espejo de datos y la confidencialidad de la información académica.

---

## 1. Arquitectura de Defensa en Profundidad

El sistema se rige por el principio de **Defensa en Profundidad**, estructurado en cuatro capas concéntricas:

```
[ Capa 1: Blindaje de Red y SSL (TiDB Cloud) ]
       ↓
[ Capa 2: Autenticación Híbrida y Rotación JWT ]
       ↓
[ Capa 3: Escudo de Datos (Sanitización & Truncamiento) ]
       ↓
[ Capa 4: Auditoría Forense (Audit Ledger) ]
```

---

## 2. Capa 1: Seguridad de Transporte y Enlace
*   **Encripción TLS/SSL**: Conexión obligatoria mediante certificados SSL para TiDB Cloud y MariaDB.
*   **Protección SIGAFI**: Acceso de **Solo Lectura** a nivel de motor de base de datos. El usuario de enlace carece de permisos `DELETE` o `UPDATE` en la base central.

---

## 3. Capa 2: Identidad y Acceso Híbrido

### Autenticación Dual (Legacy & Modern)
El `AuthController` implementa un puente de compatibilidad que detecta el hash de origen:
*   **SIGAFI Legacy**: Soporta hashes BCrypt (`$2a$`, `$2b$`) heredados del sistema central.
*   **ISTPET Native**: Utiliza SHA-256 para usuarios creados localmente.

### Hardening de Sesión
*   **JWT Rotation**: Generación de tokens con llaves de 32 caracteres (256 bits) conforme a estándares JWA.
*   **Rate Limiting**: El endpoint de login implementa una política de demora exponencial ante intentos fallidos para mitigar ataques de fuerza bruta.

---

## 4. Capa 3: El Escudo de Datos (Sync Data Shield)

Implementado en `DataSyncService.cs`, este componente actúa como un firewall de datos durante la sincronización SIGAFI -> Local.

### 4.1. Sanitización de Esquema (Safe Truncation)
Para evitar que datos malformados de SIGAFI causen excepciones en el ORM, el sistema trunca automáticamente campos críticos:
| Entidad | Campo | Límite Seguro |
| :--- | :--- | :--- |
| Prácticas | `user_asigna`, `user_llegada` | 20 caracteres |
| Vehículos | `chasis`, `motor`, `observaciones` | 50-200 caracteres |
| Alumnos | `idPeriodo` | 7 caracteres |

### 4.2. Validación Proactiva
*   **Format Shaper**: Normalización de nombres a Mayúsculas y eliminación de caracteres no alfanuméricos en descripciones.
*   **Referencia Foránea Suave**: Si un registro de práctica intenta sincronizarse sin que su vehículo o alumno existan localmente, el escudo **omite** el registro en lugar de romper la transacción, priorizando la estabilidad del sistema.

---

## 5. Capa 4: Auditoría Forense (Digital Audit Ledger)

Cada acción crítica (Inicios de sesión, Sincronizaciones masivas, Registro de Salida de Vehículos) es documentada por el `SqlAuditService`.

### Estructura del Log de Auditoría:
*   **Rastreo de IP**: Se captura la `X-Forwarded-For` o la IP remota real.
*   **Fingerprinting**: Registro del User-Agent y geolocalización aproximada (si está disponible).
*   **Snapshot de Carga**: Almacena el estado de los datos *antes y después* de la operación crítica.

---

## 6. Configuración Recomendada para Producción

> [!CAUTION]
> En entornos de producción (Azure/Render), las siguientes variables son obligatorias para activar el escudo completo:

*   `JWT_KEY`: Mínimo 32 caracteres aleatorios.
*   `DATABASE_URL`: Debe incluir el parámetro `ssl-ca` para conexiones remotas.
*   `CORS_ALLOWED_ORIGINS`: Debe apuntar exclusivamente al dominio del frontend institucional.
