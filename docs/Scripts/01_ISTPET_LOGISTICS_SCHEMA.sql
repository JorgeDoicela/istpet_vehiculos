-- ============================================================
-- 01_ISTPET_LOGISTICS_SCHEMA.sql
-- Version: 2026.1 (Professional Architecture)
-- Propósito: Definición del esquema principal del sistema de Logística.
-- ============================================================

CREATE DATABASE IF NOT EXISTS istpet_vehiculos;
USE istpet_vehiculos;

-- 1. Tablas de Soporte
CREATE TABLE IF NOT EXISTS tipo_licencia (
    id_tipo INT PRIMARY KEY AUTO_INCREMENT,
    codigo VARCHAR(10) UNIQUE NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN DEFAULT TRUE
);

-- 2. Espejos de SIGAFI (Solo se crean en istpet_vehiculos)
-- Almacenamos copias locales para evitar latencia y reportes offline.

CREATE TABLE IF NOT EXISTS profesores (
    idProfesor VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(80),
    segundoNombre VARCHAR(80),
    primerApellido VARCHAR(80),
    segundoApellido VARCHAR(80),
    nombres VARCHAR(160), -- Concatenación local
    apellidos VARCHAR(160), -- Concatenación local
    celular VARCHAR(50),
    email VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS alumnos (
    idAlumno VARCHAR(15) PRIMARY KEY,
    primerNombre VARCHAR(80),
    segundoNombre VARCHAR(80),
    apellidoPaterno VARCHAR(80),
    apellidoMaterno VARCHAR(80),
    celular VARCHAR(50),
    email VARCHAR(100),
    activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS cursos (
    idNivel INT PRIMARY KEY,
    idCarrera INT,
    Nivel VARCHAR(100),
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

-- 3. Tablas Operativas del Sistema
CREATE TABLE IF NOT EXISTS vehiculos (
    idVehiculo INT PRIMARY KEY,
    numero_vehiculo VARCHAR(10) UNIQUE,
    placa VARCHAR(15) UNIQUE NOT NULL,
    marca VARCHAR(100),
    modelo VARCHAR(100),
    anio INT,
    chasis VARCHAR(100),
    motor VARCHAR(100),
    id_tipo_licencia INT,
    id_instructor_fijo VARCHAR(15),
    estado_mecanico VARCHAR(30) DEFAULT 'OPERATIVO',
    activo BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (id_tipo_licencia) REFERENCES tipo_licencia(id_tipo),
    FOREIGN KEY (id_instructor_fijo) REFERENCES profesores(idProfesor)
);

CREATE TABLE IF NOT EXISTS matriculas (
    idMatricula INT PRIMARY KEY,
    idAlumno VARCHAR(15) NOT NULL,
    idNivel INT NOT NULL,
    idSeccion INT DEFAULT 1,
    idPeriodo VARCHAR(10),
    paralelo VARCHAR(5) DEFAULT 'A',
    fecha_matricula DATE,
    horas_completadas DECIMAL(10,2) DEFAULT 0.00,
    estado VARCHAR(20) DEFAULT 'ACTIVO',
    valida TINYINT DEFAULT 1,
    FOREIGN KEY (idAlumno) REFERENCES alumnos(idAlumno),
    FOREIGN KEY (idNivel) REFERENCES cursos(idNivel)
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
    activa TINYINT DEFAULT 1,
    FOREIGN KEY (idAlumno) REFERENCES alumnos(idAlumno),
    FOREIGN KEY (idVehiculo) REFERENCES vehiculos(idVehiculo),
    FOREIGN KEY (idProfesor) REFERENCES profesores(idProfesor)
);

-- 4. Vistas Operativas
CREATE OR REPLACE VIEW v_clases_activas AS
SELECT 
    p.idPractica AS id_registro,
    p.idalumno AS idAlumno,
    e.primerNombre AS primer_nombre,
    e.apellidoPaterno AS apellido_paterno,
    CONCAT(e.apellidoPaterno, ' ', e.primerNombre) AS estudiante,
    v.idVehiculo AS id_vehiculo,
    v.numero_vehiculo AS numero_vehiculo,
    v.placa AS placa,
    CONCAT(i.primerApellido, ' ', i.primerNombre) AS instructor,
    p.hora_salida AS salida
FROM cond_alumnos_practicas p
JOIN alumnos e ON p.idalumno = e.idAlumno
JOIN vehiculos v ON p.idvehiculo = v.idVehiculo
JOIN profesores i ON p.idProfesor = i.idProfesor
WHERE p.ensalida = 1 AND p.cancelado = 0;

-- 5. Carga de Metadatos del Sistema (Requeridos)
INSERT IGNORE INTO tipo_licencia (codigo, descripcion) 
VALUES 
('C', 'CONDUCCIÓN NO PROFESIONAL TIPO C'),
('D', 'CONDUCCIÓN PROFESIONAL TIPO D'),
('E', 'CONDUCCIÓN PROFESIONAL TIPO E');

SELECT 'ESQUEMA ISTPET_VEHICULOS Y METADATOS CREADOS' AS Status;
