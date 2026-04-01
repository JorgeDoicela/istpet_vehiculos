-- ============================================================
--  SQL HEALER
--  Uso: Ejecutar si hay errores 500 o fallos de mapeo.
--  Este script repara la estructura SIN borrar los datos.
-- ============================================================

USE istpet_vehiculos;

-- 1. REPARACIÓN DE SEGURIDAD
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS nombre_completo VARCHAR(100) AFTER rol;
ALTER TABLE usuarios MODIFY COLUMN activo TINYINT(1) DEFAULT 1;

-- 2. REPARACIÓN DE PLURALIZACIÓN (Estudiantes e Instructores)
-- Si las columnas ya son plurales, MySQL simplemente dará una advertencia.
ALTER TABLE estudiantes CHANGE COLUMN IF EXISTS nombre nombres VARCHAR(100);
ALTER TABLE estudiantes CHANGE COLUMN IF EXISTS apellido apellidos VARCHAR(100);

ALTER TABLE instructores CHANGE COLUMN IF EXISTS nombre nombres VARCHAR(100);
ALTER TABLE instructores CHANGE COLUMN IF EXISTS apellido apellidos VARCHAR(100);

-- 3. REPARACIÓN DE FLOTA (Vehículos)
ALTER TABLE vehiculos ADD COLUMN IF NOT EXISTS activo TINYINT(1) DEFAULT 1;
ALTER TABLE vehiculos MODIFY COLUMN estado_mecanico ENUM('OPERATIVO', 'MANTENIMIENTO', 'FUERA_SERVICIO') DEFAULT 'OPERATIVO';

-- 4. RE-ESTABLECIMIENTO DE VISTAS MAESTRAS
-- Estas vistas son el "motor" del Dashboard Apple Light.
CREATE OR REPLACE VIEW v_clases_activas AS
SELECT
    rs.id_registro,
    e.cedula,
    CONCAT(e.nombres, ' ', e.apellidos) AS estudiante,
    v.placa,
    v.numero_vehiculo,
    CONCAT(ins.nombres, ' ', ins.apellidos) AS instructor,
    rs.fecha_hora_salida AS salida
FROM registros_salida rs
JOIN matriculas m ON rs.id_matricula = m.id_matricula
JOIN estudiantes e ON m.cedula_estudiante = e.cedula
JOIN vehiculos v ON rs.id_vehiculo = v.id_vehiculo
JOIN instructores ins ON rs.id_instructor = ins.id_instructor
WHERE rs.id_registro NOT IN (SELECT id_registro FROM registros_llegada);

CREATE OR REPLACE VIEW v_alerta_mantenimiento AS
SELECT
    v.id_vehiculo,
    v.numero_vehiculo,
    v.placa,
    v.km_actual,
    v.km_proximo_mantenimiento,
    (v.km_proximo_mantenimiento - v.km_actual) AS km_restantes
FROM vehiculos v
WHERE (v.km_proximo_mantenimiento - v.km_actual) <= 500
OR v.estado_mecanico = 'MANTENIMIENTO';

-- 5. AUDITORÍA DE CONSTRAINTS
-- Asegura que no falten los FKs básicos si la tabla fue creada manualmente.
-- Nota: Si fallan, es porque ya existen, no hay problema.
SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='fk_veh_tipo_h' AND TABLE_NAME='vehiculos' AND TABLE_SCHEMA='istpet_vehiculos');
SET @sqlstmt := IF(@exist > 0, 'SELECT "FK_EXISTS"', 'ALTER TABLE vehiculos ADD CONSTRAINT fk_veh_tipo_h FOREIGN KEY (id_tipo_licencia) REFERENCES tipo_licencia(id_tipo)');
PREPARE stmt FROM @sqlstmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;
