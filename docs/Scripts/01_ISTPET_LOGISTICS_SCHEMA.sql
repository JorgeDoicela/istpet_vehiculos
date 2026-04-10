-- ============================================================
-- 01_ISTPET_LOGISTICS_SCHEMA.sql
-- Version: 2026.1 (Professional Architecture)
-- Propósito: Esquema de la BD LOCAL istpet_vehiculos (espejo operativo).
-- Los datos de negocio se originan en SIGAFI (sigafi_es remoto) y se rellenan vía Master Sync API.
-- Índice de scripts: docs/Scripts/README.md
-- ============================================================

DROP DATABASE IF EXISTS istpet_vehiculos;
CREATE DATABASE IF NOT EXISTS istpet_vehiculos;
USE istpet_vehiculos;

-- 1. Tablas de Soporte
CREATE TABLE IF NOT EXISTS tipo_licencia (
    id_tipo INT PRIMARY KEY AUTO_INCREMENT,
    codigo VARCHAR(10) UNIQUE NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN DEFAULT TRUE,
    id_categoria_sigafi INT NULL UNIQUE COMMENT 'FK lógica a SIGAFI categoria_vehiculos.idCategoria'
);

-- 2. Espejos de SIGAFI (Solo se crean en istpet_vehiculos)
-- Almacenamos copias locales para evitar latencia y reportes offline.

CREATE TABLE IF NOT EXISTS periodos (
    idPeriodo CHAR(7) PRIMARY KEY,
    detalle VARCHAR(100),
    fecha_inicial DATE,
    fecha_final DATE,
    cerrado TINYINT(1) DEFAULT 0,
    fecha_maxima_autocierre DATE,
    activo TINYINT(1) DEFAULT 1,
    creditos TINYINT(1) DEFAULT 0,
    numero_pagos INT UNSIGNED DEFAULT 1,
    fecha_matrucla_extraordinaria DATE,
    foliop INT,
    permiteMatricula TINYINT DEFAULT 0,
    ingresoCalificaciones TINYINT DEFAULT 0,
    permiteCalificacionesInstituto TINYINT DEFAULT 0,
    periodoactivoinstituto TINYINT DEFAULT 0,
    visualizaPowerBi TINYINT DEFAULT 0,
    esInstituto TINYINT DEFAULT 0,
    periodoPlanificacion TINYINT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS carreras (
    idCarrera INT PRIMARY KEY,
    Carrera VARCHAR(100),
    fechaCreacion DATE,
    activa TINYINT(1) DEFAULT 1,
    directorCarrera VARCHAR(100),
    numero_creditos INT,
    ordenCarrera INT DEFAULT 0,
    numero_alumnos INT,
    revisaArrastres TINYINT DEFAULT 1,
    codigo_cases VARCHAR(20),
    aliasCarrera VARCHAR(5),
    BolsaEmpleo TINYINT(1) DEFAULT 0,
    esInstituto TINYINT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS secciones (
    idSeccion INT PRIMARY KEY,
    seccion VARCHAR(30),
    sufijo CHAR(1)
);

CREATE TABLE IF NOT EXISTS modalidades (
    idModalidad INT PRIMARY KEY,
    modalidad VARCHAR(100),
    sufijo CHAR(1)
);

CREATE TABLE IF NOT EXISTS instituciones (
    idInstitucion INT PRIMARY KEY,
    Institucion VARCHAR(200),
    ciudad VARCHAR(100),
    provincia VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS profesores (
    idProfesor VARCHAR(15) PRIMARY KEY,
    tipodocumento VARCHAR(1) DEFAULT 'C',
    primerNombre VARCHAR(80),
    segundoNombre VARCHAR(80),
    primerApellido VARCHAR(80),
    segundoApellido VARCHAR(80),
    nombres VARCHAR(160), -- Concatenación local
    apellidos VARCHAR(160), -- Concatenación local
    estadoCivil INT DEFAULT 1,
    direccion VARCHAR(100),
    telefono VARCHAR(30),
    celular VARCHAR(50),
    email VARCHAR(100),
    fecha_nacimiento DATE,
    sexo VARCHAR(1),
    clave VARCHAR(20) DEFAULT '321',
    practicas INT DEFAULT 0,
    tipo VARCHAR(1) DEFAULT 'P',
    titulo VARCHAR(200),
    abreviatura VARCHAR(5),
    emailInstitucional VARCHAR(255),
    tipoSangre VARCHAR(5),
    foto VARCHAR(255),
    nacionalidad VARCHAR(50),
    activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS alumnos (
    idAlumno VARCHAR(15) PRIMARY KEY,
    tipoDocumento VARCHAR(1) DEFAULT 'C',
    primerNombre VARCHAR(80),
    segundoNombre VARCHAR(80),
    apellidoPaterno VARCHAR(80),
    apellidoMaterno VARCHAR(80),
    fecha_Nacimiento DATE,
    direccion VARCHAR(60),
    telefono VARCHAR(20),
    celular VARCHAR(50),
    email VARCHAR(100),
    sexo VARCHAR(1),
    nacionalidad VARCHAR(50),
    idNivel INT,
    idPeriodo VARCHAR(7),
    idSeccion INT,
    idModalidad INT,
    idInstitucion INT,
    tituloColegio VARCHAR(200),
    fecha_Inscripcion DATE,
    nombre_padre VARCHAR(150),
    ciudad_residencia VARCHAR(100),
    tipo_sangre VARCHAR(6),
    user_alumno VARCHAR(20),
    password VARCHAR(20),
    email_institucional VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS cursos (
    idNivel INT PRIMARY KEY,
    idCarrera INT,
    Nivel VARCHAR(160),
    jerarquia INT,
    orden INT,
    esRecuperacion TINYINT,
    aliasCurso VARCHAR(10),
    activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS usuarios_web (
    usuario VARCHAR(50) PRIMARY KEY,
    password VARCHAR(255) NOT NULL,
    salida TINYINT DEFAULT 0,
    ingreso TINYINT DEFAULT 0,
    activo BOOLEAN DEFAULT TRUE,
    asistencia TINYINT DEFAULT 0,
    esRrhh TINYINT DEFAULT 0,
    creado_en DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS categoria_vehiculos (
    idCategoria INT PRIMARY KEY,
    categoria VARCHAR(160) NOT NULL
);

CREATE TABLE IF NOT EXISTS categorias_examenes_conduccion (
    IdCategoria INT PRIMARY KEY,
    categoria VARCHAR(160) NOT NULL,
    tieneNota BOOLEAN DEFAULT TRUE,
    activa BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS cond_alumnos_horarios (
    idAsignacionHorario INT PRIMARY KEY,
    idAsignacion INT NOT NULL,
    idFecha INT,
    idHora INT,
    asiste TINYINT DEFAULT 0,
    activo BOOLEAN DEFAULT TRUE,
    observacion TEXT,
    INDEX idx_asig_horario (idAsignacion)
);

-- 3. Tablas Operativas del Sistema
CREATE TABLE IF NOT EXISTS vehiculos (
    idVehiculo INT PRIMARY KEY,
    idSubcategoria INT,
    numero_vehiculo VARCHAR(10) UNIQUE,
    placa VARCHAR(15) UNIQUE NOT NULL,
    marca VARCHAR(160),
    anio INT,
    idCategoria INT,
    activo BOOLEAN DEFAULT TRUE,
    observacion TEXT,
    chasis VARCHAR(160),
    motor VARCHAR(160),
    modelo VARCHAR(160),
    -- Logistics / Operational Fields
    id_tipo_licencia INT,
    id_instructor_fijo VARCHAR(15),
    estado_mecanico VARCHAR(30) DEFAULT 'OPERATIVO',
    FOREIGN KEY (id_tipo_licencia) REFERENCES tipo_licencia(id_tipo),
    FOREIGN KEY (id_instructor_fijo) REFERENCES profesores(idProfesor)
);

CREATE TABLE IF NOT EXISTS matriculas (
    idMatricula INT PRIMARY KEY,
    idAlumno VARCHAR(15) NOT NULL,
    idNivel INT NOT NULL,
    idSeccion INT DEFAULT 1,
    idModalidad INT DEFAULT 1,
    idPeriodo VARCHAR(10),
    paralelo VARCHAR(10) DEFAULT 'A',
    fechaMatricula DATE,
    arrastres TINYINT,
    folio INT,
    beca_matricula DECIMAL(5,2),
    retirado TINYINT,
    esOyente TINYINT DEFAULT 0,
    horas_completadas DECIMAL(10,2) DEFAULT 0.00,
    estado VARCHAR(20) DEFAULT 'ACTIVO',
    valida TINYINT DEFAULT 1,
    FOREIGN KEY (idAlumno) REFERENCES alumnos(idAlumno),
    FOREIGN KEY (idNivel) REFERENCES cursos(idNivel)
);

CREATE TABLE IF NOT EXISTS matriculas_examen_conduccion (
    idMatricula INT NOT NULL,
    idCategoria INT NOT NULL,
    nota DECIMAL(6,2) NULL,
    observacion TEXT NULL,
    usuario VARCHAR(50) NULL,
    fechaExamen DATE NULL,
    fechaIngreso DATETIME NULL,
    instructor VARCHAR(80) NULL,
    PRIMARY KEY (idMatricula, idCategoria),
    FOREIGN KEY (idMatricula) REFERENCES matriculas(idMatricula),
    FOREIGN KEY (idCategoria) REFERENCES categorias_examenes_conduccion(IdCategoria)
);

CREATE TABLE IF NOT EXISTS cond_alumnos_practicas (
    idPractica INT PRIMARY KEY AUTO_INCREMENT,
    idalumno VARCHAR(15) NOT NULL,
    idvehiculo INT NOT NULL,
    idProfesor VARCHAR(15) NOT NULL,
    idPeriodo VARCHAR(10),
    dia VARCHAR(15),
    fecha DATE NOT NULL,
    hora_salida TIME,
    hora_llegada TIME,
    tiempo TIME,
    ensalida TINYINT DEFAULT 1,
    verificada TINYINT DEFAULT 0,
    user_asigna VARCHAR(20),
    user_llegada VARCHAR(20),
    cancelado TINYINT DEFAULT 0,
    -- SIGAFI cond_alumnos_practicas: sin columna observaciones; campo opcional en réplica (notas locales / salidas).
    observaciones TEXT,
    FOREIGN KEY (idalumno) REFERENCES alumnos(idAlumno),
    FOREIGN KEY (idvehiculo) REFERENCES vehiculos(idVehiculo),
    FOREIGN KEY (idProfesor) REFERENCES profesores(idProfesor)
);

CREATE TABLE IF NOT EXISTS cond_alumnos_vehiculos (
    idAsignacion INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15) NOT NULL,
    idVehiculo INT NOT NULL,
    idProfesor VARCHAR(15) NOT NULL,
    idPeriodo VARCHAR(10),
    fechaAsignacion DATETIME,
    fechaInicio DATETIME,
    fechaFin DATETIME,
    activa TINYINT DEFAULT 1,
    observacion VARCHAR(200),
    FOREIGN KEY (idAlumno) REFERENCES alumnos(idAlumno),
    FOREIGN KEY (idVehiculo) REFERENCES vehiculos(idVehiculo),
    FOREIGN KEY (idProfesor) REFERENCES profesores(idProfesor)
);

CREATE TABLE IF NOT EXISTS asignacion_instructores_vehiculos (
    idAsignacion INT PRIMARY KEY,
    idVehiculo INT NOT NULL,
    idProfesor VARCHAR(15) NOT NULL,
    fecha_asignacion DATETIME,
    fecha_salida DATETIME,
    activo BOOLEAN DEFAULT TRUE,
    usuario_asigna VARCHAR(20),
    usuario_desactiva VARCHAR(20),
    observacion TEXT,
    FOREIGN KEY (idVehiculo) REFERENCES vehiculos(idVehiculo),
    FOREIGN KEY (idProfesor) REFERENCES profesores(idProfesor)
);

CREATE TABLE IF NOT EXISTS cond_practicas_horarios_alumnos (
    idPractica INT NOT NULL,
    idAsignacionHorario INT NOT NULL,
    PRIMARY KEY (idPractica, idAsignacionHorario),
    FOREIGN KEY (idPractica) REFERENCES cond_alumnos_practicas(idPractica),
    FOREIGN KEY (idAsignacionHorario) REFERENCES cond_alumnos_horarios(idAsignacionHorario)
);

-- 4. Auditoría del sistema
CREATE TABLE IF NOT EXISTS audit_logs (
    id          INT          NOT NULL AUTO_INCREMENT,
    usuario     VARCHAR(50)  NOT NULL COMMENT 'Cédula o login del operador',
    accion      VARCHAR(50)  NOT NULL COMMENT 'LOGIN | LOGIN_FAIL | SALIDA | LLEGADA | SYNC | SYNC_FAIL',
    entidad_id  VARCHAR(100) NULL     COMMENT 'PK de la entidad afectada (idPractica, idAlumno, etc.)',
    detalles    TEXT         NULL     COMMENT 'Información adicional en texto libre',
    ip_origen   VARCHAR(45)  NULL     COMMENT 'IPv4 o IPv6 del cliente',
    fecha_hora  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_audit_usuario (usuario),
    INDEX idx_audit_accion  (accion),
    INDEX idx_audit_fecha   (fecha_hora)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci
  COMMENT='Registro de acciones relevantes para auditoría del sistema';

-- 5. Vistas Operativas
CREATE OR REPLACE VIEW v_clases_activas AS
SELECT
    p.idPractica AS id_registro,
    p.idalumno AS idAlumno,
    e.primerNombre AS primer_nombre,
    e.apellidoPaterno AS apellido_paterno,
    COALESCE(CONCAT(e.apellidoPaterno, ' ', e.primerNombre), p.idalumno) AS estudiante,
    v.idVehiculo AS id_vehiculo,
    v.numero_vehiculo AS numero_vehiculo,
    v.placa AS placa,
    COALESCE(CONCAT(i.primerApellido, ' ', i.primerNombre), p.idProfesor) AS instructor,
    p.hora_salida AS salida
FROM cond_alumnos_practicas p
LEFT JOIN alumnos e ON p.idalumno = e.idAlumno
LEFT JOIN vehiculos v ON p.idvehiculo = v.idVehiculo
LEFT JOIN profesores i ON p.idProfesor = i.idProfesor
WHERE p.ensalida = 1 AND p.cancelado = 0;

CREATE OR REPLACE VIEW v_alerta_mantenimiento AS
SELECT
    idVehiculo AS id_vehiculo,
    numero_vehiculo AS numero_vehiculo,
    placa AS placa
FROM vehiculos
WHERE activo = 1 AND estado_mecanico != 'OPERATIVO';


-- 6. Carga de Metadatos del Sistema (Requeridos)
INSERT IGNORE INTO tipo_licencia (codigo, descripcion)
VALUES
('C', 'CONDUCCIÓN NO PROFESIONAL TIPO C'),
('D', 'CONDUCCIÓN PROFESIONAL TIPO D'),
('E', 'CONDUCCIÓN PROFESIONAL TIPO E');

-- Usuario administrador bootstrap para primer acceso local.
INSERT IGNORE INTO usuarios_web (usuario, password, salida, ingreso, activo, asistencia, esRrhh)
VALUES ('admin', 'admin123', 1, 1, 1, 0, 1);

SELECT 'ESQUEMA ISTPET_VEHICULOS COMPLETO (tablas + audit_logs + metadatos)' AS Status;
