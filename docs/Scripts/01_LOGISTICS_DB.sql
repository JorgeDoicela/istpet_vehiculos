-- ============================================================
-- 01_LOGISTICS_DB.sql
-- Database: istpet_vehiculos
-- Purpose: Primary Operational Schema for Logistics Control
-- ============================================================

CREATE DATABASE IF NOT EXISTS istpet_vehiculos;
USE istpet_vehiculos;

-- Master Data: License Types
CREATE TABLE IF NOT EXISTS tipos_licencia (
    id_tipo_licencia INT PRIMARY KEY AUTO_INCREMENT,
    nombre VARCHAR(50) NOT NULL,
    descripcion TEXT
);

-- Master Data: Instructors
CREATE TABLE IF NOT EXISTS instructores (
    id_instructor INT PRIMARY KEY AUTO_INCREMENT,
    cedula VARCHAR(15) UNIQUE NOT NULL,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL,
    telefono VARCHAR(20),
    email VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

-- Master Data: Vehicles
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

-- Master Data: Students
CREATE TABLE IF NOT EXISTS estudiantes (
    id_estudiante INT PRIMARY KEY AUTO_INCREMENT,
    cedula VARCHAR(15) UNIQUE NOT NULL,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL,
    telefono VARCHAR(20),
    email VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

-- Operational: Courses & Enrollments
CREATE TABLE IF NOT EXISTS cursos (
    id_curso INT PRIMARY KEY AUTO_INCREMENT,
    nombre VARCHAR(100) NOT NULL,
    id_tipo_licencia INT,
    FOREIGN KEY (id_tipo_licencia) REFERENCES tipos_licencia(id_tipo_licencia)
);

CREATE TABLE IF NOT EXISTS matriculas (
    id_matricula INT PRIMARY KEY AUTO_INCREMENT,
    cedula_estudiante VARCHAR(15) NOT NULL,
    id_curso INT NOT NULL,
    fecha_matricula DATE NOT NULL,
    horas_completadas INT DEFAULT 0,
    estado ENUM('ACTIVO', 'SUSPENDIDO', 'FINALIZADO') DEFAULT 'ACTIVO',
    FOREIGN KEY (cedula_estudiante) REFERENCES estudiantes(cedula),
    FOREIGN KEY (id_curso) REFERENCES cursos(id_curso)
);

-- System: Users
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

-- Transactions: Departure Logs
CREATE TABLE IF NOT EXISTS registros_salida (
    id_registro INT PRIMARY KEY AUTO_INCREMENT,
    id_matricula INT NOT NULL,
    id_vehiculo INT NOT NULL,
    id_instructor INT NOT NULL,
    fecha_hora_salida DATETIME DEFAULT CURRENT_TIMESTAMP,
    observaciones_salida TEXT,
    registrado_por INT,
    FOREIGN KEY (id_matricula) REFERENCES matriculas(id_matricula),
    FOREIGN KEY (id_vehiculo) REFERENCES vehiculos(id_vehiculo),
    FOREIGN KEY (id_instructor) REFERENCES instructores(id_instructor),
    FOREIGN KEY (registrado_por) REFERENCES usuarios(id_usuario)
);

-- Transactions: Arrival Logs
CREATE TABLE IF NOT EXISTS registros_llegada (
    id_registro INT PRIMARY KEY,
    fecha_hora_llegada DATETIME DEFAULT CURRENT_TIMESTAMP,
    observaciones_llegada TEXT,
    km_llegada INT,
    registrado_por INT,
    FOREIGN KEY (id_registro) REFERENCES registros_salida(id_registro),
    FOREIGN KEY (registrado_por) REFERENCES usuarios(id_usuario)
);
