/* ============================================================================
   SCRIPT DE INTEGRACIÓN DIRECTA: LOGÍSTICA -> SIGAFI_ES (ESCENARIO B)
   ============================================================================
   Ejecutar este script DENTRO de la base de datos 'sigafi_es' solo si NO se
   permite la creación de la base de datos independiente 'istpet_vehiculos'.

   Este script añade los 3 elementos mínimos que SIGAFI no tiene pero que el
   sistema de logística necesita para operar.
   ============================================================================ */

-- 1. Bitácora de Auditoría (Para saber qué usuario web hizo cada acción)
CREATE TABLE IF NOT EXISTS `audit_logs` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `usuario` varchar(50) NOT NULL,
  `accion` varchar(50) NOT NULL,
  `entidad_id` varchar(100) DEFAULT NULL,
  `detalles` text,
  `ip_origen` varchar(45) DEFAULT NULL,
  `fecha_hora` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci;

-- 2. Extensión de Vehículos (Para estado mecánico e instructores fijos)
CREATE TABLE IF NOT EXISTS `vehiculos_operacion` (
  `idVehiculo` int(11) NOT NULL,
  `id_instructor_fijo` varchar(14) DEFAULT NULL,
  `estado_mecanico` varchar(30) DEFAULT 'OPERATIVO',
  PRIMARY KEY (`idVehiculo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci;

-- 3. Vista del Radar de Pista (Monitor de Clases Activas)
CREATE OR REPLACE VIEW `v_clases_activas` AS
SELECT
    p.idPractica AS id_registro,
    p.idalumno AS idAlumno,
    CONCAT(a.apellidoPaterno, ' ', a.primerNombre) AS estudiante,
    v.numero_vehiculo,
    v.placa,
    CONCAT(pr.apellidos, ' ', pr.nombres) AS instructor,
    p.hora_salida AS salida
FROM cond_alumnos_practicas p
JOIN alumnos a ON a.idAlumno = p.idalumno
JOIN vehiculos v ON v.idVehiculo = p.idvehiculo
JOIN profesores pr ON pr.idProfesor = p.idProfesor
WHERE p.ensalida = 1 AND p.hora_llegada IS NULL;
