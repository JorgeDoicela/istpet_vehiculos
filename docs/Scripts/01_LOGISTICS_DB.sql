-- ============================================================
-- 01_LOGISTICS_DB.sql
-- Database: istpet_vehiculos
-- Purpose: Primary Operational Schema for Logistics Control
-- Absolute SIGAFI Parity Edition 2026.
-- ============================================================

CREATE DATABASE IF NOT EXISTS istpet_vehiculos;
USE istpet_vehiculos;

-- 1. Master Data: License Types (tipo_licencia)
CREATE TABLE IF NOT EXISTS tipo_licencia (
    id_tipo INT PRIMARY KEY AUTO_INCREMENT,
    codigo VARCHAR(10) UNIQUE NOT NULL, -- e.g. 'C', 'D', 'E'
    descripcion VARCHAR(200) NOT NULL,
    activo BOOLEAN DEFAULT TRUE
);

-- 2. Master Data: Instructors (instructores)
-- Mirroring SIGAFI/profesores schema for 1:1 parity
CREATE TABLE IF NOT EXISTS instructores (
    idProfesor VARCHAR(15) PRIMARY KEY, -- Primary identification (CEDULA)
    primerNombre VARCHAR(80) NOT NULL,
    segundoNombre VARCHAR(80),
    primerApellido VARCHAR(80) NOT NULL,
    segundoApellido VARCHAR(80),
    nombres VARCHAR(160), -- Compatibility field
    apellidos VARCHAR(160), -- Compatibility field
    celular VARCHAR(50),
    email VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

-- 3. Master Data: Vehicles (vehiculos)
-- Mirroring SIGAFI/vehiculos schema with local logistics augmentation
CREATE TABLE IF NOT EXISTS vehiculos (
    idVehiculo INT PRIMARY KEY, -- Matches SIGAFI idVehiculo
    numero_vehiculo VARCHAR(10) UNIQUE,
    placa VARCHAR(15) UNIQUE NOT NULL,
    marca VARCHAR(100),
    modelo VARCHAR(100),
    anio INT,
    chasis VARCHAR(50),
    motor VARCHAR(50),
    observacion VARCHAR(200),
    -- Local Logistics Fields
    id_tipo_licencia INT,
    id_instructor_fijo VARCHAR(15), 
    estado_mecanico ENUM('OPERATIVO', 'MANTENIMIENTO', 'FUERA_SERVICIO') DEFAULT 'OPERATIVO',
    activo BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (id_tipo_licencia) REFERENCES tipo_licencia(id_tipo),
    FOREIGN KEY (id_instructor_fijo) REFERENCES instructores(idProfesor)
);

CREATE TABLE IF NOT EXISTS mantenimientos (
    id_mantenimiento INT PRIMARY KEY AUTO_INCREMENT,
    id_vehiculo INT NOT NULL,
    fecha DATE NOT NULL,
    descripcion TEXT,
    FOREIGN KEY (id_vehiculo) REFERENCES vehiculos(idVehiculo)
);

-- 4. Master Data: Students (estudiantes)
-- Mirroring SIGAFI/alumnos schema
CREATE TABLE IF NOT EXISTS estudiantes (
    idAlumno VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(80) NOT NULL,
    segundoNombre VARCHAR(80),
    apellidoPaterno VARCHAR(80) NOT NULL,
    apellidoMaterno VARCHAR(80),
    celular VARCHAR(50),
    email VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

-- 5. Operational: Levels (niveles)
-- Mirroring SIGAFI/niveles schema
CREATE TABLE IF NOT EXISTS niveles (
    idNivel INT PRIMARY KEY,
    idCarrera INT,
    Nivel VARCHAR(100), -- Column name 'Nivel' matches central schema
    activo BOOLEAN DEFAULT TRUE
);

-- 6. Operational: Enrollments (matriculas)
-- Mirroring SIGAFI/matriculas schema with local historical tracking
CREATE TABLE IF NOT EXISTS matriculas (
    idMatricula INT PRIMARY KEY AUTO_INCREMENT,
    idAlumno VARCHAR(15) NOT NULL,
    idNivel INT NOT NULL,
    idSeccion INT DEFAULT 1,
    idModalidad INT DEFAULT 1,
    idPeriodo VARCHAR(10),
    paralelo VARCHAR(5) DEFAULT 'A',
    fecha_matricula DATE,
    -- Local Tracking
    horas_completadas DECIMAL(10,2) DEFAULT 0.00,
    estado ENUM('ACTIVO', 'SUSPENDIDO', 'FINALIZADO') DEFAULT 'ACTIVO',
    valida TINYINT DEFAULT 1,
    FOREIGN KEY (idAlumno) REFERENCES estudiantes(idAlumno),
    FOREIGN KEY (idNivel) REFERENCES niveles(idNivel)
);

-- 7. System: Users (usuarios)
CREATE TABLE IF NOT EXISTS usuarios (
    id_usuario INT PRIMARY KEY AUTO_INCREMENT,
    usuario VARCHAR(50) UNIQUE NOT NULL, -- SIGAFI Login
    password VARCHAR(255) NOT NULL, -- Col name matches central 'Contrasenia' translation
    rol VARCHAR(20) DEFAULT 'guardia', -- admin, logistica, guardia
    nombre_completo VARCHAR(160),
    activo BOOLEAN DEFAULT TRUE,
    creado_en DATETIME DEFAULT CURRENT_TIMESTAMP
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

-- 8. Transactions: Practice Logs (registros_salida)
-- Unifies Departure and Arrival for Absolute Parity with CENTRAL cond_alumnos_practicas
CREATE TABLE IF NOT EXISTS registros_salida (
    idPractica INT PRIMARY KEY AUTO_INCREMENT,
    idalumno VARCHAR(15) NOT NULL,
    idvehiculo INT NOT NULL,
    idProfesor VARCHAR(15) NOT NULL,
    idPeriodo VARCHAR(10),
    dia VARCHAR(15), -- Track day name
    fecha DATE NOT NULL,
    hora_salida TIME,
    hora_llegada TIME,
    tiempo TIME,
    ensalida TINYINT DEFAULT 1, -- 1: In track, 0: Returned
    verificada TINYINT DEFAULT 0,
    user_asigna VARCHAR(20), -- SIGAFI User
    user_llegada VARCHAR(20),
    cancelado TINYINT DEFAULT 0,
    observaciones TEXT,
    FOREIGN KEY (idalumno) REFERENCES estudiantes(idAlumno),
    FOREIGN KEY (idvehiculo) REFERENCES vehiculos(idVehiculo),
    FOREIGN KEY (idProfesor) REFERENCES instructores(idProfesor)
);
