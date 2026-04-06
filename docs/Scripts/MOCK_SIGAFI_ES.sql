-- ============================================================
-- SCRIPT DE LABORATORIO: MOCK_SIGAFI_ES
-- Propósito: Simular la base de datos central.
-- ============================================================

DROP DATABASE IF EXISTS sigafi_es;
CREATE DATABASE sigafi_es CHARACTER SET utf8mb4 COLLATE utf8mb4_spanish_ci;
USE sigafi_es;

-- 1. ESTRUCTURA (SIGAFI ORIGINAL)
CREATE TABLE alumnos (
    idAlumno VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(50), segundoNombre VARCHAR(50),
    apellidoPaterno VARCHAR(50), apellidoMaterno VARCHAR(50),
    celular VARCHAR(15), email VARCHAR(100), foto LONGBLOB
);

CREATE TABLE profesores (
    idProfesor VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(50), segundoNombre VARCHAR(50),
    apellidoPaterno VARCHAR(50), apellidoMaterno VARCHAR(50),
    celular VARCHAR(15), email VARCHAR(100), activo INT DEFAULT 1
);

CREATE TABLE usuario (
    IdUsuario BIGINT PRIMARY KEY,
    Contrasenia VARCHAR(255),
    Activo INT DEFAULT 1
);

CREATE TABLE vehiculo (
    IdVehiculo INT PRIMARY KEY,
    NumeroVehiculo INT, Placa VARCHAR(15), Marca VARCHAR(50), Modelo VARCHAR(50), IdTipoVehiculo INT, Estado INT DEFAULT 0
);

CREATE TABLE cond_alumnos_practicas (
    idPractica INT PRIMARY KEY AUTO_INCREMENT,
    idalumno VARCHAR(15), idvehiculo INT, idProfesor VARCHAR(15), fecha DATE, hora_salida TIME, hora_llegada TIME, cancelado INT DEFAULT 0, user_asigna VARCHAR(20), user_llegada VARCHAR(20)
);

CREATE TABLE matriculas (
    idMatricula INT PRIMARY KEY AUTO_INCREMENT, idAlumno VARCHAR(15), idPeriodo INT, fechaMatricula DATE, valida INT DEFAULT 1
);

CREATE TABLE periodos (
    idPeriodo INT PRIMARY KEY, detalle VARCHAR(50), activo INT DEFAULT 1
);

-- 2. DATOS DE PRUEBA (Para validar el mapeo)
INSERT INTO alumnos VALUES
('1725555377', 'JORGE', 'ISMAEL', 'DOICELA', 'MOLINA', '0999999999', 'jorge@mail.com', NULL);

INSERT INTO profesores VALUES
('1712345678', 'RICHARD', 'MAURICIO', 'TRUJILLO', 'REDROBAN', '0888888888', 'richard@mail.com', 1);

-- Password para el profe: 123456 (Hash BCrypt real para probar el bridge)
INSERT INTO usuario VALUES (1712345678, '$2a$11$q9W1mufMebc9j6n0U8.H6D.5m0N1/6n7p6/U/L6N6/L6N6/L6N6/L6', 1);

INSERT INTO vehiculo VALUES (35, 35, 'PBA-1234', 'CHEVROLET', 'AVEO', 1, 0);

INSERT INTO matriculas (idAlumno, idPeriodo, fechaMatricula) VALUES ('1725555377', 1, CURDATE());

INSERT INTO periodos VALUES (1, 'OCT2025', 1);

-- Una práctica agendada para hoy
INSERT INTO cond_alumnos_practicas (idalumno, idvehiculo, idProfesor, fecha, hora_salida, user_asigna)
VALUES ('1725555377', 35, '1712345678', CURDATE(), '08:00:00', 'ADMIN');

SELECT 'MOCK_SIGAFI_ES CREADO EXITOSAMENTE' AS Status;
