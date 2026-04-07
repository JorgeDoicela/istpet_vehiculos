-- ============================================================
-- SCRIPT DE PRODUCCIÓN NUBE: sigafi_es (Arquitectura Espejo)
-- Propósito: Crear la base de datos central en TiDB Cloud
-- tal cual existe en el servidor físico de la oficina.
-- ============================================================

-- IMPORTANTE: Ejecutar esto en la consola SQL de TiDB Cloud
CREATE DATABASE IF NOT EXISTS sigafi_es;
USE sigafi_es;

-- 1. ESTRUCTURA ACADÉMICA (ESTÁNDAR SIGAFI)
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

CREATE TABLE IF NOT EXISTS secciones (
    idSeccion INT PRIMARY KEY,
    nombre VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS cursos (
    idNivel INT PRIMARY KEY,
    nombre VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS vehiculos (
    idVehiculo INT PRIMARY KEY,
    numero_vehiculo INT, placa VARCHAR(15), Marca VARCHAR(50), Modelo VARCHAR(50), IdTipoVehiculo INT, Estado INT DEFAULT 0
);

-- 2. ESTRUCTURA LOGÍSTICA (ESTÁNDAR COND)
CREATE TABLE IF NOT EXISTS cond_alumnos_practicas (
    idPractica INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idVehiculo INT, idProfesor VARCHAR(15),
    fecha DATE, hora_salida TIME, hora_llegada TIME, cancelado INT DEFAULT 0, user_asigna VARCHAR(20), user_llegada VARCHAR(20)
);

CREATE TABLE IF NOT EXISTS cond_alumnos_vehiculos (
    idAsignacion INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idVehiculo INT, idPeriodo INT, idProfesor VARCHAR(15),
    activa INT DEFAULT 1, fechaAsignacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS matriculas (
    idMatricula INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15), idPeriodo INT, idNivel INT, idSeccion INT,
    fechaMatricula DATE, paralelo VARCHAR(5), valida INT DEFAULT 1
);

CREATE TABLE IF NOT EXISTS periodos (
    idPeriodo INT PRIMARY KEY, detalle VARCHAR(50), activo INT DEFAULT 1
);

-- 3. DATOS DE PRUEBA (MISMOS QUE LOCAL)
INSERT IGNORE INTO alumnos VALUES
('1725555377', 'JORGE', 'ISMAEL', 'DOICELA', 'MOLINA', '0999999999', 'jorge@mail.com', NULL),
('1750000001', 'LUIS', 'ALBERTO', 'FERNANDEZ', 'RUIZ', '0990000001', 'luis@mail.com', NULL);

INSERT IGNORE INTO profesores VALUES
('1712345678', 'RICHARD', 'MAURICIO', 'TRUJILLO', 'REDROBAN', '0888888888', 'richard@mail.com', 1);

INSERT IGNORE INTO vehiculos VALUES
(35, 35, 'PBA-1234', 'CHEVROLET', 'AVEO', 1, 0);

INSERT IGNORE INTO periodos VALUES (1, 'OCT2025', 1), (2, '2026-I', 1);
INSERT IGNORE INTO secciones VALUES (1, 'MATUTINA'), (2, 'NOCTURNA');
INSERT IGNORE INTO cursos VALUES (100, 'SOFTWARE'), (102, 'TIPO C');

INSERT IGNORE INTO matriculas (idAlumno, idPeriodo, idNivel, idSeccion, fechaMatricula, paralelo) VALUES
('1725555377', 1, 100, 1, CURDATE(), 'A'),
('1750000001', 2, 102, 1, CURDATE(), 'A');

INSERT IGNORE INTO cond_alumnos_practicas (idAlumno, idVehiculo, idProfesor, fecha, hora_salida, user_asigna)
VALUES
('1725555377', 35, '1712345678', CURDATE(), '08:00:00', 'ADMIN');

SELECT 'BASE DE DATOS sigafi_es CREADA Y POBLADA EXITOSAMENTE EN LA NUBE' AS Status;
