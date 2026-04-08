-- ============================================================
-- 02_CENTRAL_DB.sql
-- Database: sigafi_es
-- Purpose: Central Academic (SIGAFI) Schema Simulation
-- Guarantees the 1:1 structure for the SQL Bridge
-- ============================================================

CREATE DATABASE IF NOT EXISTS sigafi_es;
USE sigafi_es;

-- 1. Academic Data: Students (alumnos)
CREATE TABLE IF NOT EXISTS alumnos (
    idAlumno VARCHAR(14) PRIMARY KEY,
    tipoDocumento CHAR(1) DEFAULT 'C',
    apellidoPaterno VARCHAR(30),
    apellidoMaterno VARCHAR(30),
    primerNombre VARCHAR(30),
    segundoNombre VARCHAR(30),
    fecha_Nacimiento DATE,
    direccion VARCHAR(60),
    telefono VARCHAR(20),
    celular VARCHAR(20),
    email VARCHAR(40),
    foto LONGBLOB,
    sexo CHAR(1),
    nacionalidad VARCHAR(50),
    idNivel INT DEFAULT 1,
    idPeriodo CHAR(7),
    idSeccion INT
);

-- 2. Academic Data: Professors (profesores)
CREATE TABLE IF NOT EXISTS profesores (
    idProfesor VARCHAR(14) PRIMARY KEY,
    primerApellido VARCHAR(30),
    segundoApellido VARCHAR(30),
    primerNombre VARCHAR(30),
    segundoNombre VARCHAR(30),
    celular VARCHAR(20),
    email VARCHAR(40),
    activo INT DEFAULT 1
);

-- 3. Logistic Data: Vehicles (vehiculos)
CREATE TABLE IF NOT EXISTS vehiculos (
    idVehiculo INT PRIMARY KEY,
    numero_vehiculo VARCHAR(10),
    placa VARCHAR(15),
    Marca VARCHAR(50),
    Modelo VARCHAR(50),
    IdTipoVehiculo INT,
    Estado INT DEFAULT 0,
    anio INT,
    chasis VARCHAR(50),
    motor VARCHAR(50)
);

-- 4. Security Data: Central Users (usuario)
CREATE TABLE IF NOT EXISTS usuario (
    IdUsuario INT PRIMARY KEY, -- Linked to idProfesor or idAlumno numeric part
    Contrasenia VARCHAR(255),
    Activo INT DEFAULT 1,
    salida INT DEFAULT 0,
    ingreso INT DEFAULT 0,
    rrhh INT DEFAULT 0
);

-- 5. Operational: Activity Schedules (cond_alumnos_practicas)
CREATE TABLE IF NOT EXISTS cond_alumnos_practicas (
    idPractica INT PRIMARY KEY AUTO_INCREMENT,
    idalumno VARCHAR(14), 
    idvehiculo INT, 
    idProfesor VARCHAR(14),
    fecha DATE, 
    hora_salida TIME, 
    hora_llegada TIME, 
    cancelado INT DEFAULT 0, 
    user_asigna VARCHAR(20), 
    user_llegada VARCHAR(20)
);

-- 6. Academic: Enrollments (matriculas)
CREATE TABLE IF NOT EXISTS matriculas (
    idMatricula INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(14), 
    idPeriodo CHAR(7), 
    idNivel INT, 
    idSeccion INT,
    fechaMatricula DATE, 
    paralelo VARCHAR(5), 
    valida INT DEFAULT 1
);

-- 7. Academic: Levels (niveles)
CREATE TABLE IF NOT EXISTS niveles (
    idNivel INT PRIMARY KEY,
    idCarrera INT,
    Nivel VARCHAR(100) -- Matches C# 'NivelNombre' via mapping
);

-- ------------------------------------------------------------
-- INITIAL SEED DATA (Simulation)
-- ------------------------------------------------------------

-- Central User (Jorge Ismael)
-- Using a hashed version for safety (Simulation)
INSERT IGNORE INTO usuario (IdUsuario, Contrasenia, Activo, salida, ingreso, rrhh) 
VALUES (1725555377, '$2a$12$K79B1f.w.v/WfA.h.X.v.Ou.O.v.O.v.O.v.O.v.O.v.O.v.O.v.O', 1, 1, 1, 1);

-- Central Student (Jorge Ismael)
INSERT IGNORE INTO alumnos (idAlumno, apellidoPaterno, apellidoMaterno, primerNombre, segundoNombre, celular, email, idNivel, idPeriodo) 
VALUES ('1725555377', 'DOICELA', 'MOLINA', 'JORGE', 'ISMAEL', '0999999999', 'jorge@mail.com', 1, 'OCT2025');

-- Central Professor
INSERT IGNORE INTO profesores (idProfesor, primerApellido, segundoApellido, primerNombre, segundoNombre, celular, email, activo) 
VALUES ('1712345678', 'TRUJILLO', 'REDROBAN', 'RICHARD', 'MAURICIO', '0888888888', 'richard@mail.com', 1);

-- Central Vehicle
INSERT IGNORE INTO vehiculos (idVehiculo, numero_vehiculo, placa, Marca, Modelo, IdTipoVehiculo, anio) 
VALUES (35, '35', 'PBA-1234', 'CHEVROLET', 'AVEO', 1, 2024);

-- Level
INSERT IGNORE INTO niveles (idNivel, idCarrera, Nivel) VALUES (1, 1, 'PRIMERO - CONDUCCIÓN PROFESIONAL C');

-- Enrollment
INSERT IGNORE INTO matriculas (idAlumno, idPeriodo, idNivel, idSeccion, fechaMatricula, paralelo) 
VALUES ('1725555377', 'OCT2025', 1, 1, CURDATE(), 'A');

-- Daily Schedule (Practice)
INSERT IGNORE INTO cond_alumnos_practicas (idalumno, idvehiculo, idProfesor, fecha, hora_salida, user_asigna) 
VALUES ('1725555377', 35, '1712345678', CURDATE(), '08:00:00', 'ADMIN');
