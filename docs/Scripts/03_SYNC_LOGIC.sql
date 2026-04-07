-- ============================================================
-- 03_SYNC_LOGIC.sql
-- Purpose: Automatic Synchronization Bridge (Central -> Logistics)
-- Logic: Cross-Database Data Migration
-- ============================================================

USE istpet_vehiculos;

-- 1. Sync Base License Types
INSERT IGNORE INTO tipos_licencia (nombre, descripcion) VALUES 
('C', 'Liviana'), 
('D', 'Pesada'), 
('E', 'Extra Pesada');

-- 2. Sync Instructors (sigafi_es.profesores -> istpet_vehiculos.instructores)
INSERT IGNORE INTO instructores (cedula, nombres, apellidos, telefono, email, activo)
SELECT 
    idProfesor, 
    CONCAT_WS(' ', UPPER(primerNombre), UPPER(segundoNombre)), 
    CONCAT_WS(' ', UPPER(primerApellido), UPPER(segundoApellido)), 
    celular, 
    email, 
    COALESCE(activo, 1)
FROM sigafi_es.profesores;

-- 3. Sync Vehicles (sigafi_es.vehiculos -> istpet_vehiculos.vehiculos)
INSERT IGNORE INTO vehiculos (id_vehiculo, numero_vehiculo, placa, marca, modelo, id_tipo_licencia, id_instructor_fijo, estado_mecanico)
SELECT 
    v.IdVehiculo, 
    v.numero_vehiculo, 
    v.placa, 
    v.Marca, 
    v.Modelo, 
    1, -- Default License (C)
    1, -- Default Instructor (Admin)
    'OPERATIVO'
FROM sigafi_es.vehiculos v;

-- 4. Sync Security (sigafi_es.usuario -> istpet_vehiculos.usuarios)
INSERT IGNORE INTO usuarios (usuario, password_hash, rol, nombre_completo, activo)
SELECT 
    p.idProfesor, 
    u.Contrasenia, 
    'admin', 
    CONCAT_WS(' ', p.primerApellido, p.segundoApellido, p.primerNombre, p.segundoNombre), 
    1
FROM sigafi_es.usuario u
JOIN sigafi_es.profesores p ON u.IdUsuario = CAST(p.idProfesor AS UNSIGNED);

-- 5. Sync Students (sigafi_es.alumnos -> istpet_vehiculos.estudiantes)
INSERT IGNORE INTO estudiantes (cedula, nombres, apellidos, telefono, email, activo)
SELECT 
    idAlumno, 
    CONCAT_WS(' ', UPPER(primerNombre), UPPER(segundoNombre)), 
    CONCAT_WS(' ', UPPER(apellidoPaterno), UPPER(apellidoMaterno)), 
    celular, 
    email, 
    1
FROM sigafi_es.alumnos;

-- 6. Sync Enrollments (sigafi_es.matriculas -> istpet_vehiculos.matriculas)
INSERT IGNORE INTO matriculas (id_matricula, cedula_estudiante, id_curso, fecha_matricula, horas_completadas, estado)
SELECT 
    idMatricula, 
    idAlumno, 
    1, -- Default Course
    COALESCE(fechaMatricula, CURDATE()), 
    0, 
    'ACTIVO'
FROM sigafi_es.matriculas 
WHERE valida = 1;

-- 7. Sync Historical Schedules (Last 3 Years)
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
JOIN matriculas m ON p.idAlumno = m.cedula_estudiante
JOIN vehiculos v ON p.idVehiculo = v.id_vehiculo
JOIN instructores ins ON p.idProfesor = ins.cedula
WHERE p.fecha >= DATE_SUB(CURDATE(), INTERVAL 3 YEAR);

-- Final Execution Log
SELECT 'SYNCHRONIZATION COMPLETED SUCCESSFULLY' AS Status;
