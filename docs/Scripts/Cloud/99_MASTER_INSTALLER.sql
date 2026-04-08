-- ============================================================
-- MASTER_INSTALLER.sql
-- Version: 2026.1 (Production Ready)
-- Purpose: Unified Deployment Script for Cloud Environments
-- Includes: Logistics DB, Central DB, and Data Synchronization
-- ============================================================

-- ------------------------------------------------------------
-- SECTION 1: DATABASE CLEANUP & INITIALIZATION
-- ------------------------------------------------------------
DROP DATABASE IF EXISTS istpet_vehiculos;
DROP DATABASE IF EXISTS sigafi_es;

CREATE DATABASE IF NOT EXISTS istpet_vehiculos;
CREATE DATABASE IF NOT EXISTS sigafi_es;

-- ------------------------------------------------------------
-- SECTION 2: LOGISTICS SCHEMA (istpet_vehiculos)
-- ------------------------------------------------------------
USE istpet_vehiculos;

CREATE TABLE IF NOT EXISTS tipos_licencia (
    id_tipo_licencia INT PRIMARY KEY AUTO_INCREMENT,
    nombre VARCHAR(50) NOT NULL,
    descripcion TEXT
);

CREATE TABLE IF NOT EXISTS instructores (
    id_instructor INT PRIMARY KEY AUTO_INCREMENT,
    cedula VARCHAR(15) UNIQUE NOT NULL,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL,
    telefono VARCHAR(20),
    email VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS vehiculos (
    id_vehiculo INT PRIMARY KEY AUTO_INCREMENT,
    numero_vehiculo INT UNIQUE NOT NULL,
    placa VARCHAR(15) UNIQUE NOT NULL,
    marca VARCHAR(50),
    modelo VARCHAR(50),
    id_tipo_licencia INT,
    id_instructor_fijo INT,
    estado_mecanico ENUM('OPERATIVO', 'MANTENIMIENTO', 'FUERA_SERVICIO') DEFAULT 'OPERATIVO',
    activo BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (id_tipo_licencia) REFERENCES tipos_licencia(id_tipo_licencia),
    FOREIGN KEY (id_instructor_fijo) REFERENCES instructores(id_instructor)
);

CREATE TABLE IF NOT EXISTS mantenimientos (
    id_mantenimiento INT PRIMARY KEY AUTO_INCREMENT,
    id_vehiculo INT NOT NULL,
    fecha DATE NOT NULL,
    descripcion TEXT,
    FOREIGN KEY (id_vehiculo) REFERENCES vehiculos(id_vehiculo)
);

CREATE TABLE IF NOT EXISTS estudiantes (
    cedula VARCHAR(15) PRIMARY KEY,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL,
    telefono VARCHAR(20),
    email VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS cursos (
    id_curso INT PRIMARY KEY AUTO_INCREMENT,
    nombre VARCHAR(150) NOT NULL,
    id_tipo_licencia INT,
    nivel VARCHAR(50) NOT NULL,
    paralelo VARCHAR(10) NOT NULL,
    jornada VARCHAR(50) DEFAULT 'MATUTINA',
    periodo VARCHAR(20) NOT NULL,
    fecha_inicio DATE,
    fecha_fin DATE,
    cupo_maximo INT DEFAULT 20,
    cupos_disponibles INT DEFAULT 20,
    horas_practica_total INT DEFAULT 15,
    estado VARCHAR(20) DEFAULT 'ACTIVO',
    FOREIGN KEY (id_tipo_licencia) REFERENCES tipos_licencia(id_tipo_licencia)
);

CREATE TABLE IF NOT EXISTS matriculas (
    id_matricula INT PRIMARY KEY AUTO_INCREMENT,
    cedula_estudiante VARCHAR(15) NOT NULL,
    id_curso INT NOT NULL,
    fecha_matricula DATE NOT NULL,
    horas_completadas DECIMAL(5,2) DEFAULT 0.00,
    estado VARCHAR(20) DEFAULT 'ACTIVO',
    FOREIGN KEY (cedula_estudiante) REFERENCES estudiantes(cedula),
    FOREIGN KEY (id_curso) REFERENCES cursos(id_curso)
);

CREATE TABLE IF NOT EXISTS usuarios (
    id_usuario INT PRIMARY KEY AUTO_INCREMENT,
    usuario VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    rol ENUM('admin', 'logistica', 'guardia') DEFAULT 'guardia',
    nombre_completo VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS sync_logs (
    id_log INT PRIMARY KEY AUTO_INCREMENT,
    fecha DATETIME DEFAULT CURRENT_TIMESTAMP,
    modulo VARCHAR(50),
    origen VARCHAR(50),
    estado VARCHAR(20),
    mensaje TEXT,
    registros_procesados INT DEFAULT 0,
    registros_fallidos INT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS registros_salida (
    id_registro INT PRIMARY KEY AUTO_INCREMENT,
    id_matricula INT NOT NULL,
    id_vehiculo INT NOT NULL,
    id_instructor INT NOT NULL,
    fecha_hora_salida DATETIME DEFAULT CURRENT_TIMESTAMP,
    observaciones_salida TEXT,
    registrado_por INT,
    cancelado BOOLEAN DEFAULT FALSE,
    motivo_cancelacion TEXT,
    FOREIGN KEY (id_matricula) REFERENCES matriculas(id_matricula),
    FOREIGN KEY (id_vehiculo) REFERENCES vehiculos(id_vehiculo),
    FOREIGN KEY (id_instructor) REFERENCES instructores(id_instructor),
    FOREIGN KEY (registrado_por) REFERENCES usuarios(id_usuario)
);

CREATE TABLE IF NOT EXISTS registros_llegada (
    id_llegada INT PRIMARY KEY AUTO_INCREMENT,
    id_registro INT NOT NULL,
    fecha_hora_llegada DATETIME DEFAULT CURRENT_TIMESTAMP,
    observaciones_llegada TEXT,
    registrado_por INT,
    FOREIGN KEY (id_registro) REFERENCES registros_salida(id_registro),
    FOREIGN KEY (registrado_por) REFERENCES usuarios(id_usuario)
);

-- ------------------------------------------------------------
-- OPERATIONAL VIEWS (Dashboard Logic)
-- ------------------------------------------------------------
CREATE OR REPLACE VIEW v_clases_activas AS
SELECT 
    s.id_registro,
    s.id_vehiculo,
    e.cedula,
    CONCAT(e.nombres, ' ', e.apellidos) AS estudiante,
    v.placa,
    v.numero_vehiculo,
    CONCAT(i.nombres, ' ', i.apellidos) AS instructor,
    s.fecha_hora_salida AS salida
FROM registros_salida s
JOIN matriculas m ON s.id_matricula = m.id_matricula
JOIN estudiantes e ON m.cedula_estudiante = e.cedula
JOIN vehiculos v ON s.id_vehiculo = v.id_vehiculo
JOIN instructores i ON s.id_instructor = i.id_instructor
LEFT JOIN registros_llegada l ON s.id_registro = l.id_registro
WHERE l.id_llegada IS NULL;

CREATE OR REPLACE VIEW v_alerta_mantenimiento AS
SELECT 
    id_vehiculo,
    COUNT(*) AS total_mantenimientos
FROM mantenimientos
GROUP BY id_vehiculo;

-- ------------------------------------------------------------
-- SECTION 3: CENTRAL SCHEMA & DATA (sigafi_es)
-- ------------------------------------------------------------
USE sigafi_es;

CREATE TABLE IF NOT EXISTS alumnos (
    idAlumno VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(50), segundoNombre VARCHAR(50),
    apellidoPaterno VARCHAR(50), apellidoMaterno VARCHAR(50),
    celular VARCHAR(50), email VARCHAR(100), foto LONGBLOB
);

CREATE TABLE IF NOT EXISTS profesores (
    idProfesor VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(50), segundoNombre VARCHAR(50),
    primerApellido VARCHAR(50), segundoApellido VARCHAR(50),
    celular VARCHAR(50), email VARCHAR(100), activo INT DEFAULT 1
);

CREATE TABLE IF NOT EXISTS vehiculos (
    idVehiculo INT PRIMARY KEY,
    numero_vehiculo INT, placa VARCHAR(15), Marca VARCHAR(50), Modelo VARCHAR(50), IdTipoVehiculo INT, Estado INT DEFAULT 0
);

CREATE TABLE IF NOT EXISTS usuario (
    IdUsuario INT PRIMARY KEY,
    Contrasenia VARCHAR(255),
    Activo INT DEFAULT 1
);

CREATE TABLE IF NOT EXISTS cond_alumnos_practicas (
    idPractica INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idVehiculo INT, idProfesor VARCHAR(15),
    fecha DATE, hora_salida TIME, hora_llegada TIME, cancelado INT DEFAULT 0, user_asigna VARCHAR(20), user_llegada VARCHAR(20)
);

CREATE TABLE IF NOT EXISTS matriculas (
    idMatricula INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idPeriodo INT, idNivel INT, idSeccion INT,
    fechaMatricula DATE, paralelo VARCHAR(5), valida INT DEFAULT 1
);

CREATE TABLE IF NOT EXISTS periodos (
    idPeriodo INT PRIMARY KEY, detalle VARCHAR(50), activo INT DEFAULT 1
);

CREATE TABLE IF NOT EXISTS secciones (
    idSeccion INT PRIMARY KEY, seccion VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS cursos (
    idNivel INT PRIMARY KEY, Nivel VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS cond_alumnos_vehiculos (
    idAlumno VARCHAR(15), idProfesor VARCHAR(15), activa INT DEFAULT 1
);

-- Simulation Data
INSERT IGNORE INTO usuario VALUES (1725555377, '$2a$12$K79B1f.w.v/WfA.h.X.v.Ou.O.v.O.v.O.v.O.v.O.v.O.v.O', 1);
INSERT IGNORE INTO alumnos (idAlumno, primerNombre, segundoNombre, apellidoPaterno, apellidoMaterno, celular, email) VALUES ('1725555377', 'JORGE', 'ISMAEL', 'DOICELA', 'MOLINA', '0999999999', 'jorge@mail.com');
INSERT IGNORE INTO profesores VALUES ('1725555377', 'RICHARD', 'MAURICIO', 'TRUJILLO', 'REDROBAN', '0888888888', 'richard@mail.com', 1);
INSERT IGNORE INTO vehiculos VALUES (35, 35, 'PBA-1234', 'CHEVROLET', 'AVEO', 1, 0);
INSERT IGNORE INTO periodos VALUES (1, 'OCT2025', 1), (2, '2026-I', 1);
INSERT IGNORE INTO secciones VALUES (1, 'MATUTINA'), (2, 'VESPERTINA'), (3, 'NOCTURNA');
INSERT IGNORE INTO cursos VALUES (1, 'CURSO TIPO C - LICENCIA REGULAR');
INSERT IGNORE INTO cond_alumnos_vehiculos VALUES ('1725555377', '1725555377', 1);
INSERT IGNORE INTO matriculas (idAlumno, idPeriodo, idNivel, idSeccion, fechaMatricula, paralelo) VALUES ('1725555377', 2, 1, 1, CURDATE(), 'A');
INSERT IGNORE INTO cond_alumnos_practicas (idAlumno, idVehiculo, idProfesor, fecha, hora_salida, user_asigna) VALUES ('1725555377', 35, '1725555377', CURDATE(), '08:00:00', 'ADMIN');



-- ------------------------------------------------------------
-- SECTION 4: DATA SYNCHRONIZATION
-- ------------------------------------------------------------
USE istpet_vehiculos;

INSERT IGNORE INTO tipos_licencia (id_tipo_licencia, nombre, descripcion) VALUES (1, 'C', 'Liviana'), (2, 'D', 'Pesada'), (3, 'E', 'Extra Pesada');

INSERT IGNORE INTO instructores (cedula, nombres, apellidos, telefono, email, activo)
SELECT idProfesor, CONCAT_WS(' ', primerNombre, segundoNombre), CONCAT_WS(' ', primerApellido, segundoApellido), celular, email, COALESCE(activo, 1)
FROM sigafi_es.profesores;

INSERT IGNORE INTO vehiculos (id_vehiculo, numero_vehiculo, placa, marca, modelo, id_tipo_licencia, id_instructor_fijo, estado_mecanico)
SELECT v.IdVehiculo, v.numero_vehiculo, v.placa, v.Marca, v.Modelo, 1, 1, 'OPERATIVO'
FROM sigafi_es.vehiculos v;

-- Create Default Admin User (Essential for Foreign Keys)
INSERT IGNORE INTO usuarios (id_usuario, usuario, password_hash, rol, nombre_completo, activo)
VALUES (1, 'admin', '$2a$12$K79B1f.w.v/WfA.h.X.v.Ou.O.v.O.v.O.v.O.v.O.v.O.v.O', 'admin', 'ADMINISTRADOR DEL SISTEMA', 1);

INSERT IGNORE INTO usuarios (usuario, password_hash, rol, nombre_completo, activo)
SELECT p.idProfesor, u.Contrasenia, 'admin', CONCAT_WS(' ', p.primerApellido, p.segundoApellido, p.primerNombre, p.segundoNombre), 1
FROM sigafi_es.usuario u
JOIN sigafi_es.profesores p ON u.IdUsuario = CAST(p.idProfesor AS UNSIGNED)
WHERE u.IdUsuario != 1;


INSERT IGNORE INTO estudiantes (cedula, nombres, apellidos, telefono, email, activo)
SELECT idAlumno, CONCAT_WS(' ', primerNombre, segundoNombre), CONCAT_WS(' ', apellidoPaterno, apellidoMaterno), celular, email, 1
FROM sigafi_es.alumnos;

-- Create Default Course to avoid null references
INSERT IGNORE INTO cursos (id_curso, nombre, id_tipo_licencia, nivel, paralelo, jornada, periodo, fecha_inicio, fecha_fin, estado)
VALUES (1, 'CURSO DE CONDUCCIÓN PROFESIONAL', 1, 'PRIMERO', 'A', 'MATUTINA', '2026-I', CURDATE(), DATE_ADD(CURDATE(), INTERVAL 6 MONTH), 'ACTIVO');

INSERT IGNORE INTO matriculas (id_matricula, cedula_estudiante, id_curso, fecha_matricula, horas_completadas, estado)
SELECT idMatricula, idAlumno, 1, COALESCE(fechaMatricula, CURDATE()), 0, 'ACTIVO'
FROM sigafi_es.matriculas WHERE valida = 1;

-- Final Log
SELECT 'DEPLOYMENT AND SYNCHRONIZATION COMPLETED SUCCESSFULLY' AS Status;

