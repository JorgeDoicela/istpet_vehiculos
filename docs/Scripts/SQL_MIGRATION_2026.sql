-- ============================================================
--  SQL MIGRATION 2026: SIGAFI -> ISTPET ZENITH
--  Propósito: Importar datos operativos e históricos (3 años).
--  Mapeo de Licencias: 3->1 (C), 4->2 (D), 5->3 (E)
-- ============================================================

USE istpet_vehiculos;

-- 1. IMPORTAR INSTRUCTORES DESDE PROFESORES (Si no existen)
INSERT IGNORE INTO instructores (cedula, nombres, apellidos, telefono, email, activo)
SELECT 
    idProfesor, 
    CONCAT_WS(' ', primerNombre, segundoNombre), 
    CONCAT_WS(' ', apellidoPaterno, apellidoMaterno), 
    celular, 
    email, 
    COALESCE(activo, 1)
FROM sigafi_es.profesores;

-- 2. IMPORTAR VEHÍCULOS OPERATIVOS (Mapeo de Licencias)
INSERT IGNORE INTO vehiculos (id_vehiculo, numero_vehiculo, placa, marca, modelo, id_tipo_licencia, id_instructor_fijo, estado_mecanico)
SELECT 
    v.IdVehiculo,
    v.NumeroVehiculo,
    v.Placa,
    v.Marca,
    v.Modelo,
    CASE 
        WHEN v.IdTipoVehiculo = 1 THEN 1 -- Sedan -> C
        WHEN v.IdTipoVehiculo IN (4, 5) THEN 2 -- Pesado/Bus -> D/E (Ajuste según dump)
        ELSE 1 
    END,
    1, -- Default Instructor (Admin) - Se puede ajustar manualmente después
    CASE 
        WHEN v.Estado = 0 THEN 'OPERATIVO'
        WHEN v.Estado = 1 THEN 'MANTENIMIENTO'
        ELSE 'FUERA_SERVICIO'
    END
FROM sigafi_es.vehiculo v;

-- 3. IMPORTAR USUARIOS Y CLAVES (BCRYPT BRIDGE)
INSERT IGNORE INTO usuarios (usuario, password_hash, rol, nombre_completo, activo)
SELECT 
    p.idProfesor, -- Usamos cédula como login inicial
    u.Contrasenia, -- Preservamos el hash BCrypt
    'guardia',
    CONCAT_WS(' ', p.apellidoPaterno, p.apellidoMaterno, p.primerNombre, p.segundoNombre),
    1
FROM sigafi_es.usuario u
JOIN sigafi_es.profesores p ON u.IdUsuario = CAST(p.idProfesor AS UNSIGNED) -- Mapeo idPersona a idProfesor
WHERE u.Activo = 1;

-- 4. IMPORTAR ESTUDIANTES Y MATRÍCULAS (Solo últimos 3 años de actividad)
INSERT IGNORE INTO estudiantes (cedula, nombres, apellidos, telefono, email, activo)
SELECT 
    idAlumno, 
    CONCAT_WS(' ', primerNombre, segundoNombre), 
    CONCAT_WS(' ', apellidoPaterno, apellidoMaterno), 
    celular, 
    email, 
    1
FROM sigafi_es.alumnos
WHERE idAlumno IN (SELECT idalumno FROM sigafi_es.cond_alumnos_practicas WHERE fecha >= DATE_SUB(CURDATE(), INTERVAL 3 YEAR));

INSERT IGNORE INTO matriculas (id_matricula, cedula_estudiante, id_curso, fecha_matricula, horas_completadas, estado)
SELECT 
    m.idMatricula,
    m.idAlumno,
    1, -- Default Curso - Se asocia al primer curso disponible por ahora
    COALESCE(m.fechaMatricula, CURDATE()),
    0,
    'ACTIVO'
FROM sigafi_es.matriculas m
WHERE m.valida = 1 AND m.idAlumno COLLATE utf8mb4_spanish_ci IN (SELECT cedula FROM estudiantes);

-- 5. IMPORTAR HISTORIAL DE PRÁCTICAS (Últimos 3 años)
-- Salidas
INSERT IGNORE INTO registros_salida (id_registro, id_matricula, id_vehiculo, id_instructor, fecha_hora_salida, observaciones_salida, registrado_por)
SELECT 
    p.idPractica,
    m.id_matricula,
    v.id_vehiculo,
    ins.id_instructor,
    CONCAT(p.fecha, ' ', COALESCE(p.hora_salida, '00:00:00')),
    CONCAT('IMPORTADO SIGAFI - User:', p.user_asigna),
    1
FROM sigafi_es.cond_alumnos_practicas p
JOIN matriculas m ON p.idalumno = m.cedula_estudiante
JOIN vehiculos v ON p.idvehiculo = v.id_vehiculo
JOIN instructores ins ON p.idProfesor = ins.cedula
WHERE p.fecha >= DATE_SUB(CURDATE(), INTERVAL 3 YEAR);

-- Llegadas (Solo si tienen hora de llegada y no están canceladas)
INSERT IGNORE INTO registros_llegada (id_registro, fecha_hora_llegada, observaciones_llegada, registrado_por)
SELECT 
    p.idPractica,
    CONCAT(p.fecha, ' ', p.hora_llegada),
    CONCAT('IMPORTADO SIGAFI - User:', p.user_llegada),
    1
FROM sigafi_es.cond_alumnos_practicas p
WHERE p.fecha >= DATE_SUB(CURDATE(), INTERVAL 3 YEAR) 
AND p.hora_llegada IS NOT NULL 
AND p.cancelado = 0
AND p.idPractica IN (SELECT id_registro FROM registros_salida);

-- Fin de Migración
SELECT 'MIGRACIÓN COMPLETADA EXITOSAMENTE' AS Status;
