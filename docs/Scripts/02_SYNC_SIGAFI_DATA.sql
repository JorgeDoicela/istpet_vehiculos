-- ============================================================
-- 02_SYNC_SIGAFI_DATA.sql
-- Modelo: SIGAFI (servidor remoto, sigafi_es) = fuente de verdad con todos los datos.
--         istpet_vehiculos (local) = espejo alimentado desde SIGAFI.
-- Propósito de este script: INSERT…SELECT desde sigafi_es hacia local (solo referencia / manual).
-- Nota 2026: la ingesta operativa vive en el API → POST /api/Sync/master (Master Sync C#).
-- No incluye matriculas_examen_conduccion (lo cubre el Master Sync). Índice: docs/Scripts/README.md
-- ============================================================

USE istpet_vehiculos;

-- Desactivar modo seguro para actualizaciones masivas de limpieza
SET SQL_SAFE_UPDATES = 0;
-- Desactivar llaves foráneas para permitir ingesta masiva sin errores de orden
SET FOREIGN_KEY_CHECKS = 0;

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
FROM sigafi_es.cursos;

-- 2.1 Sincronización de Categorías de Vehículos
INSERT IGNORE INTO categoria_vehiculos (idCategoria, categoria)
SELECT idCategoria, categoria
FROM sigafi_es.categoria_vehiculos;

-- 2.2 Sincronización de Categorías de Exámenes
INSERT IGNORE INTO categorias_examenes_conduccion (IdCategoria, categoria, tieneNota, activa)
SELECT IdCategoria, categoria, tieneNota, activa
FROM sigafi_es.categorias_examenes_conduccion;

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

-- 5. Sincronización de Vehículos y Asignaciones
-- Primero traemos las asignaciones de instructores (Fuente de verdad)
INSERT IGNORE INTO asignacion_instructores_vehiculos (idAsignacion, idVehiculo, idProfesor, fecha_asignacion, activo, observacion)
SELECT idAsignacion, idVehiculo, idProfesor, fecha_asignacion, activo, observacion
FROM sigafi_es.asignacion_instructores_vehiculos;

-- Luego sincronizamos vehículos vinculando el instructor sugerido por SIGAFI
INSERT INTO vehiculos (idVehiculo, idSubcategoria, numero_vehiculo, placa, marca, anio, idCategoria, activo, observacion, chasis, motor, modelo, id_tipo_licencia, id_instructor_fijo, estado_mecanico)
SELECT 
    v.idVehiculo, 
    v.idSubcategoria,
    v.numero_vehiculo, 
    v.placa, 
    v.marca, 
    v.anio,
    v.idCategoria,
    v.activo,
    v.observacion,
    v.chasis,
    v.motor,
    v.modelo,
    1, -- Default Mapping
    (SELECT a.idProfesor FROM sigafi_es.asignacion_instructores_vehiculos a WHERE a.idVehiculo = v.idVehiculo AND a.activo = 1 LIMIT 1),
    'OPERATIVO'
FROM sigafi_es.vehiculos v
ON DUPLICATE KEY UPDATE
    id_instructor_fijo = VALUES(id_instructor_fijo),
    idSubcategoria = VALUES(idSubcategoria),
    idCategoria = VALUES(idCategoria);

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

-- 8. Sincronización de Horarios (Agenda Granular)
INSERT IGNORE INTO cond_alumnos_horarios (idAsignacionHorario, idAsignacion, idFecha, idHora, asiste, activo, observacion)
SELECT 
    h.idAsignacionHorario, 
    h.idAsignacion, 
    h.idFecha, 
    h.idHora, 
    h.asiste, 
    h.activo, 
    h.observacion
FROM sigafi_es.cond_alumnos_horarios h
JOIN sigafi_es.cond_alumnos_vehiculos v ON v.idAsignacion = h.idAsignacion
WHERE h.activo = 1;

-- 9. Sincronización de Vínculos Práctica-Horario (The Bridge)
INSERT IGNORE INTO cond_practicas_horarios_alumnos (idPractica, idAsignacionHorario)
SELECT 
    l.idPractica, 
    l.idAsignacionHorario
FROM sigafi_es.cond_practicas_horarios_alumnos l
JOIN sigafi_es.cond_alumnos_practicas p ON p.idPractica = l.idPractica
WHERE p.fecha >= DATE_SUB(CURDATE(), INTERVAL 1 YEAR);

SELECT 'DATOS DE SIGAFI SINCRONIZADOS EXITOSAMENTE' AS Status;

-- Restaurar configuraciones de seguridad
SET FOREIGN_KEY_CHECKS = 1;
SET SQL_SAFE_UPDATES = 1;
