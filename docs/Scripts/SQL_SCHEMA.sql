-- ============================================================
--  BASE DE DATOS: istpet_vehiculos
--  Licencias: C, D, E (Escuela de Conducción ISTPET)
-- ============================================================

DROP DATABASE IF EXISTS istpet_vehiculos;
CREATE DATABASE istpet_vehiculos CHARACTER SET utf8mb4 COLLATE utf8mb4_spanish_ci;
USE istpet_vehiculos;

SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;
SET collation_connection = 'utf8mb4_spanish_ci';

-- ------------------------------------------------------------
-- 1. SEGURIDAD Y ACCESO
-- ------------------------------------------------------------
CREATE TABLE usuarios (
    id_usuario    INT            NOT NULL AUTO_INCREMENT,
    usuario        VARCHAR(50)    NOT NULL UNIQUE,
    password_hash VARCHAR(255)   NOT NULL,
    rol            ENUM('admin', 'guardia', 'estacionable') NOT NULL DEFAULT 'guardia',
    nombre_completo VARCHAR(100),
    activo         TINYINT(1)     NOT NULL DEFAULT 1,
    creado_en      DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id_usuario)
);

-- ------------------------------------------------------------
-- 2. PARAMETRIZACIÓN
-- ------------------------------------------------------------
CREATE TABLE tipo_licencia (
    id_tipo     INT          NOT NULL AUTO_INCREMENT,
    codigo      VARCHAR(5)   NOT NULL UNIQUE,
    descripcion VARCHAR(200) NOT NULL,
    activo      TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (id_tipo)
);

-- ------------------------------------------------------------
-- 3. RECURSOS HUMANOS
-- ------------------------------------------------------------
CREATE TABLE instructores (
    id_instructor INT          NOT NULL AUTO_INCREMENT,
    cedula        VARCHAR(15)  NOT NULL UNIQUE,
    nombres       VARCHAR(100) NOT NULL,
    apellidos     VARCHAR(100) NOT NULL,
    telefono      VARCHAR(15)  NULL,
    email         VARCHAR(100) NULL,
    activo        TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (id_instructor)
);

CREATE TABLE instructor_licencias (
    id_instructor INT NOT NULL,
    id_tipo_licencia INT NOT NULL,
    fecha_obtencion DATE NULL,
    PRIMARY KEY (id_instructor, id_tipo_licencia),
    CONSTRAINT fk_rel_instructor FOREIGN KEY (id_instructor) REFERENCES instructores (id_instructor) ON DELETE CASCADE,
    CONSTRAINT fk_rel_tipo FOREIGN KEY (id_tipo_licencia) REFERENCES tipo_licencia (id_tipo)
);

-- ------------------------------------------------------------
-- 4. GESTIÓN DE FLOTA
-- ------------------------------------------------------------
CREATE TABLE vehiculos (
    id_vehiculo      INT          NOT NULL AUTO_INCREMENT,
    numero_vehiculo  INT          NOT NULL UNIQUE,
    placa            VARCHAR(15)  NOT NULL UNIQUE,
    marca            VARCHAR(80)  NULL,
    modelo           VARCHAR(80)  NULL,
    id_tipo_licencia INT          NOT NULL,
    id_instructor_fijo INT        NOT NULL,
    estado_mecanico  ENUM('OPERATIVO', 'MANTENIMIENTO', 'FUERA_SERVICIO') DEFAULT 'OPERATIVO',
    activo           TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (id_vehiculo),
    CONSTRAINT fk_veh_tipo FOREIGN KEY (id_tipo_licencia) REFERENCES tipo_licencia (id_tipo),
    CONSTRAINT fk_veh_instructor_fijo FOREIGN KEY (id_instructor_fijo) REFERENCES instructores (id_instructor)
);

CREATE TABLE mantenimientos (
    id_mantenimiento INT NOT NULL AUTO_INCREMENT,
    id_vehiculo      INT NOT NULL,
    fecha            DATE NOT NULL,
    descripcion      TEXT,
    costo            DECIMAL(10,2) DEFAULT 0.00,
    PRIMARY KEY (id_mantenimiento),
    CONSTRAINT fk_mant_vehiculo FOREIGN KEY (id_vehiculo) REFERENCES vehiculos (id_vehiculo)
);

-- ------------------------------------------------------------
-- 5. ACADÉMICO
-- ------------------------------------------------------------
CREATE TABLE cursos (
    id_curso          INT          NOT NULL AUTO_INCREMENT,
    id_tipo_licencia  INT          NOT NULL,
    nombre            VARCHAR(150) NOT NULL,
    nivel             VARCHAR(50)  NOT NULL,
    paralelo          VARCHAR(10)  NOT NULL,
    jornada           VARCHAR(20)  NOT NULL DEFAULT 'MATUTINA',
    periodo           VARCHAR(20)  NOT NULL,
    fecha_inicio      DATE         NOT NULL,
    fecha_fin         DATE         NOT NULL,
    cupo_maximo       INT          NOT NULL DEFAULT 20,
    cupos_disponibles INT          NOT NULL DEFAULT 20,
    horas_practica_total INT       NOT NULL DEFAULT 15,
    estado            VARCHAR(20)  NOT NULL DEFAULT 'ACTIVO',
    PRIMARY KEY (id_curso),
    CONSTRAINT fk_curso_tipo FOREIGN KEY (id_tipo_licencia) REFERENCES tipo_licencia (id_tipo)
);

CREATE TABLE estudiantes (
    cedula         VARCHAR(15)  NOT NULL,
    nombres        VARCHAR(100) NOT NULL,
    apellidos      VARCHAR(100) NOT NULL,
    telefono       VARCHAR(15)  NULL,
    email          VARCHAR(100) NULL,
    activo         TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (cedula)
);

CREATE TABLE matriculas (
    id_matricula      INT         NOT NULL AUTO_INCREMENT,
    cedula_estudiante VARCHAR(15) NOT NULL,
    id_curso          INT         NOT NULL,
    fecha_matricula   DATE        NOT NULL,
    horas_completadas DECIMAL(5,2) DEFAULT 0.00,
    estado            VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',
    PRIMARY KEY (id_matricula),
    CONSTRAINT fk_mat_estudiante FOREIGN KEY (cedula_estudiante) REFERENCES estudiantes (cedula),
    CONSTRAINT fk_mat_curso FOREIGN KEY (id_curso) REFERENCES cursos (id_curso)
);

-- ------------------------------------------------------------
-- 6. CONTROL LOGÍSTICO
-- ------------------------------------------------------------
CREATE TABLE registros_salida (
    id_registro    INT          NOT NULL AUTO_INCREMENT,
    id_matricula   INT          NOT NULL,
    id_vehiculo    INT          NOT NULL,
    id_instructor  INT          NOT NULL,
    fecha_hora_salida DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    observaciones_salida TEXT   NULL,
    registrado_por INT          NULL,
    PRIMARY KEY (id_registro),
    CONSTRAINT fk_sal_matricula FOREIGN KEY (id_matricula) REFERENCES matriculas (id_matricula),
    CONSTRAINT fk_sal_vehiculo  FOREIGN KEY (id_vehiculo) REFERENCES vehiculos (id_vehiculo),
    CONSTRAINT fk_sal_instructor FOREIGN KEY (id_instructor) REFERENCES instructores (id_instructor),
    CONSTRAINT fk_sal_usuario   FOREIGN KEY (registrado_por) REFERENCES usuarios (id_usuario)
);

CREATE TABLE registros_llegada (
    id_llegada      INT NOT NULL AUTO_INCREMENT,
    id_registro     INT NOT NULL UNIQUE,
    fecha_hora_llegada DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    observaciones_llegada TEXT NULL,
    registrado_por  INT NULL,
    PRIMARY KEY (id_llegada),
    CONSTRAINT fk_lleg_registro FOREIGN KEY (id_registro) REFERENCES registros_salida (id_registro),
    CONSTRAINT fk_lleg_usuario  FOREIGN KEY (registrado_por) REFERENCES usuarios (id_usuario)
);

-- ------------------------------------------------------------
-- 7. LÓGICA DE NEGOCIO
-- Nota: La lógica de negocio (Sp, Triggers) ha sido migrada
-- al Backend (C# EF Core) para mayor seguridad y mantenimiento.
-- ------------------------------------------------------------


-- ------------------------------------------------------------
-- 8. VISTAS
-- ------------------------------------------------------------
CREATE OR REPLACE VIEW v_clases_activas AS
SELECT 
    rs.id_registro,
    v.id_vehiculo,
    e.cedula,
    CONCAT(e.nombres, ' ', e.apellidos) AS estudiante,
    v.placa,
    v.numero_vehiculo,
    CONCAT(ins.nombres, ' ', ins.apellidos) AS instructor,
    rs.fecha_hora_salida AS salida
FROM registros_salida rs
JOIN matriculas m ON rs.id_matricula = m.id_matricula
JOIN estudiantes e ON m.cedula_estudiante = e.cedula
JOIN vehiculos v ON rs.id_vehiculo = v.id_vehiculo
JOIN instructores ins ON rs.id_instructor = ins.id_instructor
LEFT JOIN registros_llegada rl ON rs.id_registro = rl.id_registro
WHERE rl.id_llegada IS NULL;

CREATE OR REPLACE VIEW v_alerta_mantenimiento AS
SELECT
    v.id_vehiculo,
    v.numero_vehiculo,
    v.placa
FROM vehiculos v
WHERE v.estado_mecanico = 'MANTENIMIENTO';

-- ------------------------------------------------------------
-- 9. DATOS INICIALES
-- ------------------------------------------------------------
INSERT INTO tipo_licencia (codigo, descripcion) VALUES
('C', 'Profesional (Taxis, autos livianos)'),
('D', 'Profesional (Buses de pasajeros)'),
('E', 'Profesional (Camiones y carga pesada)');

-- Admin Initial: istpet2026
INSERT INTO usuarios (usuario, password_hash, rol, nombre_completo)
VALUES ('admin_istpet', SHA2('istpet2026', 256), 'admin', 'Administrador General ISTPET');
