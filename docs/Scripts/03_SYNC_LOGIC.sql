-- ============================================================
-- 03_SYNC_LOGIC.sql
-- Purpose: Automatic Synchronization Bridge (Central -> Logistics)
-- Absolute SIGAFI Parity Edition 2026.
-- Logic: Cross-Database Data Migration aligned with 1:1 Parity.
-- ============================================================

USE istpet_vehiculos;

-- 1. Sync Base License Types (tipo_licencia)
-- We insert initial data if missing
INSERT IGNORE INTO tipo_licencia (codigo, descripcion) VALUES 
('C', 'CONDUCCIÓN PROFESIONAL TIPO C (LIVIANA)'), 
('D', 'CONDUCCIÓN PROFESIONAL TIPO D (PESADA)'), 
('E', 'CONDUCCIÓN PROFESIONAL TIPO E (EXTRA PESADA)');

-- 2. Sync Levels (sigafi_es.niveles -> istpet_vehiculos.niveles)
INSERT IGNORE INTO niveles (idNivel, idCarrera, Nivel)
SELECT idNivel, idCarrera, Nivel
FROM sigafi_es.niveles;

-- 3. Sync Instructors (sigafi_es.profesores -> istpet_vehiculos.instructores)
-- Mirroring exact SIGAFI structure for 1:1 parity
INSERT IGNORE INTO instructores (idProfesor, primerNombre, segundoNombre, primerApellido, segundoApellido, activo)
SELECT 
    idProfesor, 
    UPPER(primerNombre), 
    UPPER(segundoNombre), 
    UPPER(primerApellido), 
    UPPER(segundoApellido), 
    COALESCE(activo, 1)
FROM sigafi_es.profesores;

-- Simple update for compatibility fields
UPDATE instructores 
SET nombres = CONCAT_WS(' ', primerNombre, segundoNombre),
    apellidos = CONCAT_WS(' ', primerApellido, segundoApellido);

-- 4. Sync Vehicles (sigafi_es.vehiculos -> istpet_vehiculos.vehiculos)
-- Direct 1:1 sync with augmentation for local logistics
INSERT IGNORE INTO vehiculos (idVehiculo, numero_vehiculo, placa, marca, modelo, anio, chasis, motor, id_tipo_licencia, estado_mecanico)
SELECT 
    v.idVehiculo, 
    v.numero_vehiculo, 
    v.placa, 
    v.Marca, 
    v.Modelo, 
    v.anio,
    v.chasis,
    v.motor,
    1, -- Default License Mapping (Tipo C)
    'OPERATIVO'
FROM sigafi_es.vehiculos v;

-- 5. Sync Students (sigafi_es.alumnos -> istpet_vehiculos.estudiantes)
INSERT IGNORE INTO estudiantes (idAlumno, primerNombre, segundoNombre, apellidoPaterno, apellidoMaterno, activo)
SELECT 
    idAlumno, 
    UPPER(primerNombre), 
    UPPER(segundoNombre), 
    UPPER(apellidoPaterno), 
    UPPER(apellidoMaterno), 
    1
FROM sigafi_es.alumnos;

-- 6. Sync Enrollments (sigafi_es.matriculas -> istpet_vehiculos.matriculas)
INSERT IGNORE INTO matriculas (idMatricula, idAlumno, idNivel, idSeccion, idPeriodo, paralelo, fecha_matricula, estado, valida)
SELECT 
    idMatricula, 
    idAlumno, 
    idNivel,
    idSeccion,
    idPeriodo,
    paralelo,
    COALESCE(fechaMatricula, CURDATE()), 
    'ACTIVO',
    valida
FROM sigafi_es.matriculas 
WHERE valida = 1;

-- 7. Sync Security System (sigafi_es.usuario -> istpet_vehiculos.usuarios)
-- Migrating central credentials to local logistics access
INSERT IGNORE INTO usuarios (usuario, password, rol, nombre_completo, activo)
SELECT 
    p.idProfesor, 
    u.Contrasenia, 
    CASE 
        WHEN u.salida = 1 AND u.ingreso = 1 THEN 'admin'
        WHEN u.salida = 1 THEN 'logistica'
        ELSE 'guardia' 
    END, 
    CONCAT_WS(' ', p.primerApellido, p.segundoApellido, p.primerNombre, p.segundoNombre), 
    1
FROM sigafi_es.usuario u
JOIN sigafi_es.profesores p ON u.IdUsuario = CAST(p.idProfesor AS UNSIGNED);

-- 8. Sync Historical Practices (sigafi_es.cond_alumnos_practicas -> istpet_vehiculos.registros_salida)
-- Mirroring history for analytics
INSERT IGNORE INTO registros_salida (idPractica, idalumno, idvehiculo, idProfesor, fecha, hora_salida, hora_llegada, cancelado, user_asigna, ensalida)
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
    CASE WHEN hora_llegada IS NULL THEN 1 ELSE 0 END -- Determine 'in track' status
FROM sigafi_es.cond_alumnos_practicas
WHERE fecha >= DATE_SUB(CURDATE(), INTERVAL 2 YEAR);

-- Create simple view for Active Classes (Real-time monitoring)
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
    CONCAT_WS(' ', i.primerApellido, i.primerNombre) AS instructor,
    p.hora_salida AS salida
FROM registros_salida p
JOIN estudiantes e ON p.idalumno = e.idAlumno
JOIN vehiculos v ON p.idvehiculo = v.idVehiculo
JOIN instructores i ON p.idProfesor = i.idProfesor
WHERE p.ensalida = 1 AND p.cancelado = 0;

-- Final Execution Log
SELECT 'SIGAFI ABSOLUTE PARITY SYNC COMPLETED SUCCESSFULLY' AS Status;
