-- ============================================================
-- SCRIPT DE LABORATORIO: MOCK_SIGAFI_ES
-- Propósito: Simular la base de datos central SIGAFI con soporte
-- para preselección de instructores y tutorías.
-- ============================================================

DROP DATABASE IF EXISTS sigafi_es;
CREATE DATABASE sigafi_es CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE sigafi_es;

-- 1. ESTRUCTURA ACADÉMICA (SIGAFI EXTERNO)
CREATE TABLE alumnos (
    idAlumno VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(50), segundoNombre VARCHAR(50),
    apellidoPaterno VARCHAR(50), apellidoMaterno VARCHAR(50),
    celular VARCHAR(50), email VARCHAR(100), foto LONGBLOB
);

CREATE TABLE profesores (
    idProfesor VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(50), segundoNombre VARCHAR(50),
    primerApellido VARCHAR(50), segundoApellido VARCHAR(50),
    celular VARCHAR(50), email VARCHAR(100), activo INT DEFAULT 1
);

CREATE TABLE secciones (
    idSeccion INT PRIMARY KEY,
    nombre VARCHAR(50)
);

CREATE TABLE cursos (
    idNivel INT PRIMARY KEY,
    nombre VARCHAR(100)
);

CREATE TABLE vehiculo (
    IdVehiculo INT PRIMARY KEY,
    NumeroVehiculo INT, Placa VARCHAR(15), Marca VARCHAR(50), Modelo VARCHAR(50), IdTipoVehiculo INT, Estado INT DEFAULT 0
);

-- 2. ESTRUCTURA LOGÍSTICA (COND)
CREATE TABLE cond_alumnos_practicas (
    idPractica INT PRIMARY KEY AUTO_INCREMENT,
    idalumno VARCHAR(15), idvehiculo INT, idProfesor VARCHAR(15),
    fecha DATE, hora_salida TIME, cancelado INT DEFAULT 0, user_asigna VARCHAR(20)
);

CREATE TABLE cond_alumnos_vehiculos (
    idAsignacion INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idVehiculo INT, idPeriodo INT, idProfesor VARCHAR(15),
    activa INT DEFAULT 1, fechaAsignacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE matriculas (
    idMatricula INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idPeriodo INT, idNivel INT, idSeccion INT,
    fechaMatricula DATE, paralelo VARCHAR(5), valida INT DEFAULT 1
);

CREATE TABLE periodos (
    idPeriodo INT PRIMARY KEY, detalle VARCHAR(50), activo INT DEFAULT 1
);

-- 3. DATOS DE PRUEBA ESTRATÉGICOS
INSERT INTO alumnos VALUES
-- Jorge Doicela
('1725555377', 'JORGE', 'ISMAEL', 'DOICELA', 'MOLINA', '0999999999', 'jorge@mail.com', NULL),
-- Ana Torres
('1750000002', 'ANA', 'BELEN', 'TORRES', 'LOZANO', '0990000002', 'ana@mail.com', NULL),
-- Luis Fernandez
('1750000001', 'LUIS', 'ALBERTO', 'FERNANDEZ', 'RUIZ', '0990000001', 'luis@mail.com', NULL);

INSERT INTO profesores VALUES
('1712345678', 'RICHARD', 'MAURICIO', 'TRUJILLO', 'REDROBAN', '0888888888', 'richard@mail.com', 1),
('1700000001', 'JUAN', 'CARLOS', 'LOPEZ', 'MENDOZA', '0988888881', 'juan.lopez@mail.com', 1),
('1700000002', 'MARIA', 'FERNANDA', 'GARCIA', 'SALAZAR', '0988888882', 'maria.garcia@mail.com', 1);

INSERT INTO vehiculo VALUES
(35, 35, 'PBA-1234', 'CHEVROLET', 'AVEO', 1, 0),
(36, 1, 'PBA-1001', 'CHEVROLET', 'AVEO', 1, 0),
(37, 2, 'PBA-1002', 'HYUNDAI', 'ACCENT', 1, 0);

INSERT INTO periodos VALUES (1, 'OCT2025', 1), (2, '2026-I', 1);
INSERT INTO secciones VALUES (1, 'MATUTINA'), (2, 'NOCTURNA');
INSERT INTO cursos VALUES (100, 'SOFTWARE'), (102, 'TIPO C');

INSERT INTO matriculas (idAlumno, idPeriodo, idNivel, idSeccion, fechaMatricula, paralelo) VALUES
('1725555377', 1, 100, 1, CURDATE(), 'A'),
('1750000001', 2, 102, 1, CURDATE(), 'A'),
('1750000002', 2, 102, 1, CURDATE(), 'A');

-- Agenda de HOY (Pre-selección por práctica)
INSERT INTO cond_alumnos_practicas (idalumno, idvehiculo, idProfesor, fecha, hora_salida, user_asigna)
VALUES
('1725555377', 35, '1712345678', CURDATE(), '08:00:00', 'ADMIN'),
('1750000001', 36, '1700000001', CURDATE(), '09:00:00', 'ADMIN'),
('1750000002', 37, '1700000002', CURDATE(), '10:00:00', 'ADMIN');

-- Tutoría General (Respaldo)
INSERT INTO cond_alumnos_vehiculos (idAlumno, idVehiculo, idPeriodo, idProfesor, activa)
VALUES
('1725555377', 35, 1, '1712345678', 1),
('1750000001', 36, 2, '1700000001', 1),
('1750000002', 37, 2, '1700000002', 1);

SELECT 'MOCK_SIGAFI_ES ACTUALIZADO EXITOSAMENTE' AS Status;
