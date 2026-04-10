-- ============================================================
-- ISTPET Logística — Migración 03: Tabla de Auditoría
-- Ejecutar UNA VEZ sobre la BD istpet_vehiculos
-- ============================================================

CREATE TABLE IF NOT EXISTS `audit_logs` (
    `id`          INT          NOT NULL AUTO_INCREMENT,
    `usuario`     VARCHAR(50)  NOT NULL COMMENT 'Cédula o login del operador',
    `accion`      VARCHAR(50)  NOT NULL COMMENT 'LOGIN | LOGIN_FAIL | SALIDA | LLEGADA | SYNC | SYNC_FAIL',
    `entidad_id`  VARCHAR(100) NULL     COMMENT 'PK de la entidad afectada (idPractica, idAlumno, etc.)',
    `detalles`    TEXT         NULL     COMMENT 'Información adicional en texto libre',
    `ip_origen`   VARCHAR(45)  NULL     COMMENT 'IPv4 o IPv6 del cliente',
    `fecha_hora`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_audit_usuario` (`usuario`),
    INDEX `idx_audit_accion`  (`accion`),
    INDEX `idx_audit_fecha`   (`fecha_hora`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish_ci
  COMMENT='Registro de acciones relevantes para auditoría del sistema';
