-- 🛡️ ZENITH 2026: RBAC TEST USERS SEED
-- Run this script in your MySQL 'istpet_vehiculos' database to test roles.

USE istpet_vehiculos;

-- 1. ADMIN USER (Acceso Total)
-- psw: admin123 (Hash simulado, será migrado a BCrypt al primer login si es SHA256, o usa el hash BCrypt directamente)
INSERT IGNORE INTO usuarios (usuario, password_hash, rol, nombre_completo, activo)
VALUES ('admin', 'admin123_hash_placeholder', 'admin', 'Administrador Maestro', 1);

-- 2. LOGISTICA USER (Acceso Operativo + Monitoreo)
-- psw: logistica123
INSERT IGNORE INTO usuarios (usuario, password_hash, rol, nombre_completo, activo)
VALUES ('logistica', 'logistica123_hash_placeholder', 'logistica', 'Coordinador de Logística', 1);

-- 3. GUARDIA USER (Acceso Limitado a Salida/Llegada)
-- psw: guardia123
INSERT IGNORE INTO usuarios (usuario, password_hash, rol, nombre_completo, activo)
VALUES ('guardia', 'guardia123_hash_placeholder', 'guardia', 'Agente de Seguridad', 1);

-- Nota: Para que el AuthController los valide, asegúrate de que el hash coincida 
-- con lo esperado o que el sistema de migración proactiva esté activo.
