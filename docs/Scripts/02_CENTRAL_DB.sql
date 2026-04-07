-- ============================================================
-- 02_CENTRAL_DB.sql
-- Database: sigafi_es
-- Purpose: Central Academic (SIGAFI) Schema Simulation
-- ============================================================

CREATE DATABASE IF NOT EXISTS sigafi_es;
USE sigafi_es;

-- Academic Data: Students
CREATE TABLE IF NOT EXISTS alumnos (
    idAlumno VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(50), segundoNombre VARCHAR(50),
    apellidoPaterno VARCHAR(50), apellidoMaterno VARCHAR(50),
    celular VARCHAR(50), email VARCHAR(100), foto LONGBLOB
);

-- Academic Data: Professors
CREATE TABLE IF NOT EXISTS profesores (
    idProfesor VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(50), segundoNombre VARCHAR(50),
    primerApellido VARCHAR(50), segundoApellido VARCHAR(50),
    celular VARCHAR(50), email VARCHAR(100), activo INT DEFAULT 1
);

-- Logistic Data: Vehicles
CREATE TABLE IF NOT EXISTS vehiculos (
    idVehiculo INT PRIMARY KEY,
    numero_vehiculo INT, placa VARCHAR(15), Marca VARCHAR(50), Modelo VARCHAR(50), IdTipoVehiculo INT, Estado INT DEFAULT 0
);

-- Security Data: Central Users
CREATE TABLE IF NOT EXISTS usuario (
    IdUsuario INT PRIMARY KEY,
    Contrasenia VARCHAR(255),
    Activo INT DEFAULT 1
);

-- Operational: Activity Schedules
CREATE TABLE IF NOT EXISTS cond_alumnos_practicas (
    idPractica INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idVehiculo INT, idProfesor VARCHAR(15),
    fecha DATE, hora_salida TIME, hora_llegada TIME, cancelado INT DEFAULT 0, user_asigna VARCHAR(20), user_llegada VARCHAR(20)
);

-- Academic: Enrollments
CREATE TABLE IF NOT EXISTS matriculas (
    idMatricula INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idPeriodo INT, idNivel INT, idSeccion INT,
    fechaMatricula DATE, paralelo VARCHAR(5), valida INT DEFAULT 1
);

-- Academic: Periods
CREATE TABLE IF NOT EXISTS periodos (
    idPeriodo INT PRIMARY KEY, detalle VARCHAR(50), activo INT DEFAULT 1
);

-- ------------------------------------------------------------
-- INITIAL SEED DATA (Simulation)
-- ------------------------------------------------------------

-- Central User (Jorge Ismael)
INSERT IGNORE INTO usuario VALUES (1725555377, '$2a$12$K79B1f.w.v/WfA.h.X.v.Ou.O.v.O.v.O.v.O.v.O.v.O.v.O', 1);

-- Central Student (Jorge Ismael)
INSERT IGNORE INTO alumnos VALUES ('1725555377', 'JORGE', 'ISMAEL', 'DOICELA', 'MOLINA', '0999999999', 'jorge@mail.com', NULL);

-- Central Professor
INSERT IGNORE INTO profesores VALUES ('1712345678', 'RICHARD', 'MAURICIO', 'TRUJILLO', 'REDROBAN', '0888888888', 'richard@mail.com', 1);

-- Central Vehicle
INSERT IGNORE INTO vehiculos VALUES (35, 35, 'PBA-1234', 'CHEVROLET', 'AVEO', 1, 0);

-- Periods
INSERT IGNORE INTO periodos VALUES (1, 'OCT2025', 1), (2, '2026-I', 1);

-- Enrollment
INSERT IGNORE INTO matriculas (idAlumno, idPeriodo, idNivel, idSeccion, fechaMatricula, paralelo) 
VALUES ('1725555377', 1, 100, 1, CURDATE(), 'A');

-- Daily Schedule
INSERT IGNORE INTO cond_alumnos_practicas (idAlumno, idVehiculo, idProfesor, fecha, hora_salida, user_asigna) 
VALUES ('1725555377', 35, '1712345678', CURDATE(), '08:00:00', 'ADMIN');
