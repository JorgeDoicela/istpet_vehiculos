-- ============================================================
-- 99_MASTER_DEPLOYMENT.sql
-- Version: 2026.1 (Cloud Testing Edition)
-- Propósito: Despliegue unificado para entornos de prueba en la nube.
-- ============================================================

-- 1. Reset de bases de datos
DROP DATABASE IF EXISTS istpet_vehiculos;
DROP DATABASE IF EXISTS sigafi_es;

CREATE DATABASE sigafi_es;
CREATE DATABASE istpet_vehiculos;

-- 2. Esquema Mock SIGAFI (Solo para pruebas en la nube)
USE sigafi_es;
CREATE TABLE alumnos (idAlumno VARCHAR(15) PRIMARY KEY, primerNombre VARCHAR(80), segundoNombre VARCHAR(80), apellidoPaterno VARCHAR(80), apellidoMaterno VARCHAR(80), email VARCHAR(100));
CREATE TABLE profesores (idProfesor VARCHAR(15) PRIMARY KEY, primerNombre VARCHAR(80), segundoNombre VARCHAR(80), primerApellido VARCHAR(80), segundoApellido VARCHAR(80), activo INT DEFAULT 1);
CREATE TABLE vehiculos (idVehiculo INT PRIMARY KEY, numero_vehiculo VARCHAR(10), placa VARCHAR(15), Marca VARCHAR(100), Modelo VARCHAR(100));
CREATE TABLE usuarios_web (usuario VARCHAR(50) PRIMARY KEY, password VARCHAR(255), salida INT DEFAULT 0, ingreso INT DEFAULT 0, activo INT DEFAULT 1, asistencia INT DEFAULT 0, esRrhh INT DEFAULT 0);
CREATE TABLE matriculas (idMatricula INT PRIMARY KEY, idAlumno VARCHAR(15), idNivel INT, idPeriodo VARCHAR(10), paralelo VARCHAR(5), valida INT DEFAULT 1);
CREATE TABLE niveles (idNivel INT PRIMARY KEY, idCarrera INT, Nivel VARCHAR(100));
CREATE TABLE cond_alumnos_practicas (idPractica INT PRIMARY KEY AUTO_INCREMENT, idalumno VARCHAR(15), idvehiculo INT, idProfesor VARCHAR(15), fecha DATE, hora_salida TIME, hora_llegada TIME, cancelado INT DEFAULT 0, user_asigna VARCHAR(20));

-- Data Mock Inicial
INSERT INTO niveles VALUES (1, 1, 'PRIMERO - CONDUCCIÓN PROFESIONAL C');
INSERT INTO usuarios_web VALUES ('admin', 'admin123', 1, 1, 1, 1, 0);
INSERT INTO alumnos (idAlumno, primerNombre, apellidoPaterno) VALUES ('1725555377', 'JORGE', 'DOICELA');
INSERT INTO profesores (idProfesor, primerNombre, primerApellido) VALUES ('1712345678', 'RICHARD', 'TRUJILLO');
INSERT INTO vehiculos VALUES (35, '35', 'PBA-1234', 'CHEVROLET', 'AVEO');
INSERT INTO matriculas (idMatricula, idAlumno, idNivel, idPeriodo, paralelo) VALUES (1, '1725555377', 1, '2026-I', 'A');

-- 3. Esquema de Logística (Lógica del Sistema)
USE istpet_vehiculos;
CREATE TABLE tipo_licencia (id_tipo INT PRIMARY KEY AUTO_INCREMENT, codigo VARCHAR(10) UNIQUE NOT NULL, descripcion VARCHAR(200));
CREATE TABLE profesores (idProfesor VARCHAR(15) PRIMARY KEY, primerNombre VARCHAR(80), segundoNombre VARCHAR(80), primerApellido VARCHAR(80), segundoApellido VARCHAR(80), nombres VARCHAR(160), apellidos VARCHAR(160), activo BOOLEAN DEFAULT TRUE);
CREATE TABLE alumnos (idAlumno VARCHAR(15) PRIMARY KEY, primerNombre VARCHAR(80), segundoNombre VARCHAR(80), apellidoPaterno VARCHAR(80), apellidoMaterno VARCHAR(80), activo BOOLEAN DEFAULT TRUE);
CREATE TABLE cursos (idNivel INT PRIMARY KEY, idCarrera INT, Nivel VARCHAR(100), activo BOOLEAN DEFAULT TRUE);
CREATE TABLE usuarios_web (usuario VARCHAR(50) PRIMARY KEY, password VARCHAR(255) NOT NULL, salida TINYINT DEFAULT 0, ingreso TINYINT DEFAULT 0, activo BOOLEAN DEFAULT TRUE, asistencia TINYINT DEFAULT 0, esRrhh TINYINT DEFAULT 0);
CREATE TABLE vehiculos (idVehiculo INT PRIMARY KEY, numero_vehiculo VARCHAR(10) UNIQUE, placa VARCHAR(15) UNIQUE NOT NULL, marca VARCHAR(100), modelo VARCHAR(100), id_tipo_licencia INT, id_instructor_fijo VARCHAR(15), estado_mecanico VARCHAR(30) DEFAULT 'OPERATIVO', FOREIGN KEY (id_tipo_licencia) REFERENCES tipo_licencia(id_tipo), FOREIGN KEY (id_instructor_fijo) REFERENCES profesores(idProfesor));
CREATE TABLE matriculas (idMatricula INT PRIMARY KEY, idAlumno VARCHAR(15) NOT NULL, idNivel INT NOT NULL, idPeriodo VARCHAR(10), paralelo VARCHAR(5), FOREIGN KEY (idAlumno) REFERENCES alumnos(idAlumno), FOREIGN KEY (idNivel) REFERENCES cursos(idNivel));
CREATE TABLE cond_alumnos_practicas (idPractica INT PRIMARY KEY AUTO_INCREMENT, idalumno VARCHAR(15) NOT NULL, idvehiculo INT NOT NULL, idProfesor VARCHAR(15) NOT NULL, fecha DATE NOT NULL, hora_salida TIME, hora_llegada TIME, ensalida TINYINT DEFAULT 1, cancelado TINYINT DEFAULT 0, FOREIGN KEY (idalumno) REFERENCES alumnos(idAlumno), FOREIGN KEY (idvehiculo) REFERENCES vehiculos(idVehiculo), FOREIGN KEY (idProfesor) REFERENCES profesores(idProfesor));

-- 4. Sincronización Inicial (Extract SIGAFI -> Load ISTPET)
-- Nota: En la nube, este paso 'jala' datos de la DB mock creada arriba.
INSERT INTO istpet_vehiculos.tipo_licencia (codigo, descripcion) 
VALUES 
('C', 'CONDUCCIÓN NO PROFESIONAL TIPO C'),
('D', 'CONDUCCIÓN PROFESIONAL TIPO D'),
('E', 'CONDUCCIÓN PROFESIONAL TIPO E');

INSERT IGNORE INTO istpet_vehiculos.cursos (idNivel, idCarrera, Nivel) SELECT idNivel, idCarrera, Nivel FROM sigafi_es.niveles;
INSERT IGNORE INTO istpet_vehiculos.profesores (idProfesor, primerNombre, primerApellido) SELECT idProfesor, primerNombre, primerApellido FROM sigafi_es.profesores;
INSERT IGNORE INTO istpet_vehiculos.alumnos (idAlumno, primerNombre, apellidoPaterno) SELECT idAlumno, primerNombre, apellidoPaterno FROM sigafi_es.alumnos;
INSERT IGNORE INTO istpet_vehiculos.usuarios_web (usuario, password, salida, ingreso, activo, asistencia, esRrhh) SELECT usuario, password, salida, ingreso, activo, asistencia, esRrhh FROM sigafi_es.usuarios_web;
INSERT IGNORE INTO istpet_vehiculos.vehiculos (idVehiculo, numero_vehiculo, placa, marca, modelo, id_tipo_licencia) SELECT idVehiculo, numero_vehiculo, placa, Marca, Modelo, 1 FROM sigafi_es.vehiculos;
INSERT IGNORE INTO istpet_vehiculos.matriculas (idMatricula, idAlumno, idNivel, idPeriodo, paralelo) SELECT idMatricula, idAlumno, idNivel, idPeriodo, paralelo FROM sigafi_es.matriculas WHERE valida = 1;

-- Vistas Operativas
CREATE OR REPLACE VIEW istpet_vehiculos.v_clases_activas AS SELECT p.idPractica AS id_registro, p.idalumno AS idAlumno, e.primerNombre AS primer_nombre, e.apellidoPaterno AS apellido_paterno, CONCAT(e.apellidoPaterno, ' ', e.primerNombre) AS estudiante, v.idVehiculo AS id_vehiculo, v.numero_vehiculo AS numero_vehiculo, v.placa AS placa, CONCAT(i.primerApellido, ' ', i.primerNombre) AS instructor, p.hora_salida AS salida FROM istpet_vehiculos.cond_alumnos_practicas p JOIN istpet_vehiculos.alumnos e ON p.idalumno = e.idAlumno JOIN istpet_vehiculos.vehiculos v ON p.idvehiculo = v.idVehiculo JOIN istpet_vehiculos.profesores i ON p.idProfesor = i.idProfesor WHERE p.ensalida = 1 AND p.cancelado = 0;

SELECT 'DEPLOYMENT UNIFICADO (NUBE) COMPLETADO' AS Status;
