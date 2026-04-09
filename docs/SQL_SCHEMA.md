# Esquema SQL Comentado — ISTPET Logística

> Archivo fuente: `docs/Scripts/SQL_SCHEMA.sql`

Este documento explica las decisiones de diseño detrás del esquema de base de datos.

---

## Configuración del Motor

```sql
CREATE DATABASE istpet_vehiculos CHARACTER SET utf8mb4 COLLATE utf8mb4_spanish_ci;
```

**Decisión:** `utf8mb4_spanish_ci` garantiza soporte completo para caracteres acentuados del español (á, é, ñ...) sin errores de codificación. `utf8mb4` es el superconjunto de UTF-8 que soporta emojis y caracteres fuera del BMP.

---

CREATE TABLE usuarios_web (
    usuario         VARCHAR(20)    NOT NULL,
    password        VARCHAR(255)   NOT NULL,
    rol             VARCHAR(50),
    salida          TINYINT(1)     NOT NULL DEFAULT 0,
    ingreso         TINYINT(1)     NOT NULL DEFAULT 0,
    activo          TINYINT(1)     NOT NULL DEFAULT 1,
    creado_en       DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (usuario)
);

**Decisiones:**
- `password_hash VARCHAR(255)`: Size suficiente para BCrypt (60 chars) y SHA-256 (64 chars hex).
- `rol ENUM`: Restricción a nivel de base de datos — imposible insertar un rol inválido.
- `activo TINYINT(1)`: Patrón de **soft delete** — no se eliminan usuarios, solo se desactivan.

---

## Bloque 2: Parametrización — `tipo_licencia`

```sql
CREATE TABLE tipo_licencia (
    id_tipo     INT          NOT NULL AUTO_INCREMENT,
    codigo      VARCHAR(5)   NOT NULL UNIQUE,
    descripcion VARCHAR(200) NOT NULL,
    activo      TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (id_tipo)
);
```

**Datos iniciales:** `C` (livianos), `D` (buses), `E` (carga pesada).

**Decisión:** Separar en tabla maestra permite agregar nuevas categorías de licencia sin modificar el código, y garantiza integridad referencial.

---

## Bloque 3: Recursos Humanos

```sql
CREATE TABLE profesores (
    idProfesor   VARCHAR(15)  NOT NULL,
    primerNombre VARCHAR(50),
    primerApellido VARCHAR(50),
    nombres      VARCHAR(150),
    apellidos    VARCHAR(150),
    PRIMARY KEY (idProfesor)
);

CREATE TABLE instructor_licencias (
    id_instructor    INT NOT NULL,
    id_tipo_licencia INT NOT NULL,
    PRIMARY KEY (id_instructor, id_tipo_licencia),
    CONSTRAINT fk_rel_instructor FOREIGN KEY (id_instructor)
        REFERENCES instructores (id_instructor) ON DELETE CASCADE
);
```

**Decisión:** `instructor_licencias` usa **llave primaria compuesta** para evitar duplicados sin necesidad de índices adicionales. `ON DELETE CASCADE` garantiza que al eliminar un instructor, sus habilitaciones se limpian automáticamente.

---

## Bloque 4: Gestión de Flota

```sql
CREATE TABLE vehiculos (
    numero_vehiculo  INT  NOT NULL UNIQUE,
    placa            VARCHAR(15) NOT NULL UNIQUE,
    estado_mecanico  ENUM('OPERATIVO', 'MANTENIMIENTO', 'FUERA_SERVICIO') DEFAULT 'OPERATIVO',
    id_tipo_licencia INT NOT NULL,
    id_instructor_fijo INT NOT NULL,
    ...
);
```

**Decisiones:**
- `numero_vehiculo UNIQUE` + `placa UNIQUE`: Doble garantía de unicidad del vehículo.
- `estado_mecanico ENUM`: Solo 3 estados posibles, controlados a nivel de BD.
- `id_instructor_fijo`: Relación fija instructor-vehículo para simplificar el despacho — el guardia ya sabe qué instructor corresponde a cada unidad.

```sql
CREATE TABLE mantenimientos (
    costo DECIMAL(10,2) DEFAULT 0.00
);
```

**Decisión:** `DECIMAL` para el costo evita errores de redondeo de punto flotante.

---

## Bloque 5: Módulo Académico

```sql
CREATE TABLE alumnos (
    idAlumno VARCHAR(15) NOT NULL,
    PRIMARY KEY (idAlumno)
);
```

**Decisión crítica:** La cédula es la PK directamente, sin `id` auto-incremental. Esto garantiza que no exista el mismo estudiante con dos cédulas distintas y simplifica las joins.

```sql
CREATE TABLE matriculas (
    horas_completadas DECIMAL(5,2) DEFAULT 0.00,
    estado            VARCHAR(20) NOT NULL DEFAULT 'ACTIVO'
);
```

**Decisión:** `horas_completadas` en la matrícula se actualiza automáticamente en C# (`SqlLogisticaService.RegistrarLlegadaAsync`) cada vez que un vehículo retorna.

---

## Bloque 6: Control Logístico

```sql
CREATE TABLE cond_alumnos_practicas (
    idPractica    INT NOT NULL AUTO_INCREMENT,
    idMatricula   INT NOT NULL,
    idvehiculo    INT NOT NULL,
    idProfesor    VARCHAR(15) NOT NULL,
    PRIMARY KEY (idPractica)
);
```

**Decisión clave — Detección de "En Pista":** La lógica no usa campos booleanos. Un vehículo está "en pista" si y solo si tiene un `registro_salida` sin `registro_llegada` correspondiente. Esto evita estados inconsistentes:
```sql
-- Vista v_clases_activas: vehículos sin llegada
LEFT JOIN registros_llegada rl ON rs.id_registro = rl.id_registro
WHERE rl.id_llegada IS NULL;
```

El `UNIQUE` en `registros_llegada.id_registro` garantiza a nivel de base de datos que un registro de salida **no puede cerrarse dos veces**.

---

## Bloque 7: Objetivos de Negocio Implementados en C#

El comentario en el SQL es importante:
```sql
-- Nota: La lógica de negocio (SP, Triggers) ha sido migrada
-- al Backend (C# EF Core) para mayor seguridad y mantenimiento.
```

Las validaciones que en la versión original usaban Stored Procedures ahora viven en `SqlLogisticaService.cs`:

| Validación | Implementación |
| :--- | :--- |
| Vehículo OPERATIVO | `vehiculo.EstadoMecanico != "OPERATIVO"` → error |
| Vehículo no en pista | Query sobre `RegistrosSalida` sin `RegistrosLlegada` |
| Instructor no ocupado | Mismo patrón para `id_instructor` |
| Estudiante no duplicado | Mismo patrón para `id_matricula` |
| Transacción atómica | `BeginTransactionAsync()` + `CommitAsync()` / `RollbackAsync()` |

---

## Datos Iniciales del Script

```sql
-- Tipos de licencia
INSERT INTO tipo_licencia (codigo, descripcion) VALUES
('C', 'Profesional (Taxis, autos livianos)'),
('D', 'Profesional (Buses de pasajeros)'),
('E', 'Profesional (Camiones y carga pesada)');

-- Usuario administrador (password: istpet2026)
INSERT INTO usuarios_web (usuario, password, rol, nombre_completo, activo)
VALUES ('admin_istpet', 'istpet2026', 'admin', 'Administrador General ISTPET', 1);

-- Curso contenedor para el Puente Híbrido (auto-registro de SIGAFI)
INSERT INTO cursos (idNivel, idCarrera, Nivel)
VALUES (1, 1, 'CURSO PROFESIONAL TIPO C');
```

El **curso con `id_curso = 1`** es el contenedor genérico al que se asignan automáticamente los estudiantes traídos desde SIGAFI si no existe un curso local más específico. Tiene cupo de 500 intencionalmente para funcionar como "curso abierto".
