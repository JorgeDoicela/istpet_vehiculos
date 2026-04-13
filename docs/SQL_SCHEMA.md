# Esquema SQL y Topología de Datos (Revision 2026)

Este documento detalla el diseño relacional del sistema ISTPET Vehículos, optimizado para la **Paridad Absoluta** con SIGAFI y la operación logística de alto rendimiento.

---

## 1. El Protocolo de Gestión de Esquema

En la arquitectura 2026, el esquema no es estático. Se gestiona mediante el **Schema Healer Protocol**:
*   **Auto-DDL**: El backend (.NET 8) comprueba e inyecta las tablas faltantes al arrancar.
*   **Cotejamiento Industrial**: `utf8mb4_spanish_ci` para soporte nativo de caracteres extendidos.

---

## 2. Definición de Tablas Core (Mirroring Layer)

Estas tablas son réplicas estructurales de SIGAFI, optimizadas para el espejo local.

### `alumnos` (Espejo Estudiantil)
*   **Clave Primaria**: `idAlumno` (Cédula).
*   **Función**: Contenedor local para búsquedas JIT y autocompletado rápido.

### `vehiculos` (Espejo de Flota)
*   **Clave Primaria**: `idVehiculo` (ID Central).
*   **Unicidad**: Restricción `UNIQUE` en `placa` y `numero_vehiculo`.

### `cond_alumnos_practicas` (Espejo de Bitácora)
*   **Propósito**: Almacena el historial de movimientos. Es la tabla que se sincroniza durante el Master Sync.

---

## 3. Capa de Extensión Operativa (`_operacion`)

El sistema protege los datos institucionales separando el estado local en tablas de extensión.

### `vehiculos_operacion`
*   **id_tipo_licencia**: Mapeo local para el filtrado de garita.
*   **mantenimiento**: Flag operativo que bloquea el despacho en tiempo real.

### `matriculas_operacion`
*   **horas_acumuladas**: Contador local de progreso académico calculado en cada CHECK-IN.

---

## 4. Auditoría e Inteligencia (Audit & Views)

### `audit_logs` (Libro de Actas Digital)
Registra cada latido del sistema:
*   `User`, `Action`, `IP`, `Timestamp`, `Metadata`.

### `v_clases_activas` (Vista de Pista)
Determina dinámicamente qué unidades están "En Pista":
```sql
CREATE VIEW v_clases_activas AS
SELECT ...
FROM cond_alumnos_practicas
WHERE hora_llegada IS NULL AND cancelado = 0;
```

---

## 5. El Escudo Contra Desbordamiento (Data Protection)

El esquema soporta el **Data Shield** del backend:
*   **Strings Seguros**: Campos de auditoría y observaciones con capacidad de hasta 255 caracteres.
*   **Detección de Colisión**: Índices compuestos en `cond_practicas_horarios_alumnos` para evitar duplicidad de registros de asistencia.

---

## 6. Bootstrap de Datos Maestros

Al ejecutarse por primera vez, el sistema inyecta:
1.  **Tipos de Licencia**: C (Livianos), D (Buses), E (Pesados).
2.  **Usuario Maestro**: `admin_istpet` con rol `admin`.
3.  **Periodo Base**: Periodo `1` para habilitar el flujo inicial de matrículas.
