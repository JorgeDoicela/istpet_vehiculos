-- ============================================================
-- 02_SYNC_SIGAFI_DATA.sql
-- Propósito: Extraer datos de sigafi_es (Solo Lectura) e 
--            insertar en istpet_vehiculos (DB Local).
-- ============================================================

USE istpet_vehiculos;

-- 1. Sincronización de Seguridad (Mirror de usuarios_web)
INSERT IGNORE INTO usuarios_web (usuario, password, salida, ingreso, activo, asistencia, esRrhh)
SELECT 
    usuario, 
    password, 
    COALESCE(salida, 0),
    COALESCE(ingreso, 0),
    COALESCE(activo, 1),
    COALESCE(asistencia, 0),
    COALESCE(esRrhh, 0)
FROM sigafi_es.usuarios_web;

-- 2. Sincronización de Cursos
INSERT IGNORE INTO cursos (idNivel, idCarrera, Nivel)
SELECT idNivel, idCarrera, Nivel
FROM sigafi_es.niveles;

-- 3. Sincronización de Profesores (Instructores)
INSERT IGNORE INTO profesores (idProfesor, primerNombre, segundoNombre, primerApellido, segundoApellido, activo)
SELECT 
    idProfesor, 
    UPPER(primerNombre), 
    UPPER(segundoNombre), 
    UPPER(primerApellido), 
    UPPER(segundoApellido), 
    COALESCE(activo, 1)
FROM sigafi_es.profesores;

UPDATE profesores 
SET nombres = CONCAT_WS(' ', primerNombre, segundoNombre),
    apellidos = CONCAT_WS(' ', primerApellido, segundoApellido);

-- 4. Sincronización de Alumnos (Estudiantes)
INSERT IGNORE INTO alumnos (idAlumno, primerNombre, segundoNombre, apellidoPaterno, apellidoMaterno, activo)
SELECT 
    idAlumno, 
    UPPER(primerNombre), 
    UPPER(segundoNombre), 
    UPPER(apellidoPaterno), 
    UPPER(apellidoMaterno), 
    1
FROM sigafi_es.alumnos;

-- 5. Sincronización de Vehículos
INSERT IGNORE INTO vehiculos (idVehiculo, numero_vehiculo, placa, marca, modelo, id_tipo_licencia, estado_mecanico)
SELECT 
    v.idVehiculo, 
    v.numero_vehiculo, 
    v.placa, 
    v.Marca, 
    v.Modelo, 
    1, -- Default Mapping
    'OPERATIVO'
FROM sigafi_es.vehiculos v;

-- 6. Sincronización de Matrículas
INSERT IGNORE INTO matriculas (idMatricula, idAlumno, idNivel, idPeriodo, paralelo)
SELECT 
    idMatricula, 
    idAlumno, 
    idNivel, 
    idPeriodo, 
    paralelo
FROM sigafi_es.matriculas 
WHERE valida = 1;

-- 7. Historial de Prácticas
INSERT IGNORE INTO cond_alumnos_practicas (idPractica, idalumno, idvehiculo, idProfesor, fecha, hora_salida, hora_llegada, cancelado, user_asigna, ensalida)
SELECT 
    idPractica, 
    idalumno, 
    idvehiculo, 
    idProfesor, 
    fecha, 
    hora_salida, 
    hora_llegada, 
    COALESCE(cancelado, 0),
    user_asigna,
    CASE WHEN hora_llegada IS NULL THEN 1 ELSE 0 END
FROM sigafi_es.cond_alumnos_practicas
WHERE fecha >= DATE_SUB(CURDATE(), INTERVAL 2 YEAR);

SELECT 'DATOS DE SIGAFI SINCRONIZADOS EXITOSAMENTE' AS Status;
