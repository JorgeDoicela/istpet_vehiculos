# Manual de Operaciones y Contingencia (Runbooks)

Este manual es confidencial y está dirigido a personal SysAdmin e ingenieros de mantenimiento del Sistema Logístico ISPTEC / Zenith. Define las arquitecturas de contingencia de Alta Disponibilidad (HA) y resoluciones estándar para anomalías.

---

## 🔒 1. Gestión de Modos de Base de Datos y Paridad

ISTPET Logística opera bajo un ecosistema de infraestructura conmutable. La variable núcleo está alojada en la raíz del entorno: `DATABASE_MODE` (`.env` o OS Env Var).

### A. Modo Directo (Producción Principal)
- **Activación:** `DATABASE_MODE=Direct`
- **Diagnóstico:** El backend extrae identificadores en tiempo real mediante *JIT Queries* contra la base Central (`SIGAFI`).
- **Casos de Uso:** Es el modo que rige el día a día. Garantiza que inscripciones y nuevas asignaciones estén disponibles al milisegundo para los guardias.
- **Acción del Cluster:** El Job de sincronización nocturna `SigafiMirrorBackgroundService` se **pausa automáticamente**.

### B. Modo Espejo (Protocolo HA de Contingencia)
- **Activación:** `DATABASE_MODE=Mirror`
- **Diagnóstico:** Aísla el servidor de la nube académica y direcciona todo el I/O logístico y de lecturas sobre la DB `istpet_vehiculos` local.
- **Casos de Uso:** Latencia masiva en la VPN educativa, o paradas programadas de SIGAFI.

---

## 🛠 2. Runbook de Despliegue y Recuperación Continua (CD)

No existe necesidad de correr scripts largos SQL.

**Paso para Actualizar Servidor / Despliegue Zero-Downtime:**
```powershell
# Ingrese al entorno Windows/Server del ISTPET
cd C:\Ruta\Del\Repositorio
# Dispare el Release Generador:
.\scripts\create-release-bundle.ps1
```
*Lo que hace internamente:*
1. Compila front y back end de forma binaria.
2. Comprime assets e imágenes Docker locales.
3. Genera un archivo `.zip` portátil que puede ser enviado al servidor DMZ sin requerimientos de red adicional (Air-Gapped Deployment).

> [!CAUTION]
> **Schema Healer:** El sistema repara la BD automáticamente en cada reinicio. NO ingrese sentencias `CREATE TABLE` manuales; puede provocar choques con Entity Framework y corromper migraciones controladas.

---

## 🚑 3. Resolución de Incidentes Comunes (Troubleshooting)

### Fenómeno 1: `ERROR: "SIGAFI Muestra Latencia Crítica y Despacho Falla"`
El Polly Circuit Breaker entrará en escena.
- **Protocolo Inmediato:** El frontend no se caerá, pero botará alertas de indisponibilidad. Espere `30s` (Tiempo natural de reposo de Polly) antes de intentar dar paso a otro estudiante. 
- **Solución Final:** Si dura > 5min, rote a *Modo Espejo (Mirror)* cambiando la variable local temporalmente.

### Fenómeno 2: `Vehículo Fantasma (Vehículo bloqueado en modo "EN_PISTA")`
Provocado si el servidor central se corta justo en la fase de confirmación de registro de un retorno de garita.
- **Resolución UI:** Solicite a un usuario `admin` que elimine el flujo trabado presionando el botón "Forzar Corrección/Revertir".
- **Comprobación Interna:** Valide la vista `v_clases_activas`. Aquella tabla pivote es la fuente real de la interfaz. No edite manualmente filas transaccionales desde el CMS.
