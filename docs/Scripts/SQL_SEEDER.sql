-- ============================================================
--  SQL SEEDER 2026
--  Propósito: Poblar el sistema para pruebas y demostración.
--  Uso: Ejecutar DESPUÉS de SQL_SCHEMA.sql
-- ============================================================

USE istpet_vehiculos;

-- 1. INSTRUCTORES (Elite Team)
INSERT IGNORE INTO instructores (cedula, nombres, apellidos, telefono, email) VALUES
('1712345678', 'Marco Antonio', 'Pérez Salazar', '0991234567', 'marco.perez@istpet.edu.ec'),
('1712345679', 'Elena Sofía', 'Ramírez Cueva', '0991234568', 'elena.ramirez@istpet.edu.ec'),
('1804561239', 'Richard Mauricio', 'Trujillo Redroban', '0987654321', 'richard.trujillo@istpet.edu.ec'),
('1712345681', 'Lucía Fernanda', 'Ortega Rojas', '0981234570', 'lucia.ortega@istpet.edu.ec');

-- 2. ASIGNACIÓN DE LICENCIAS A INSTRUCTORES (Ignorar si ya existen)
INSERT IGNORE INTO instructor_licencias (id_instructor, id_tipo_licencia, fecha_obtencion) VALUES
(1, 1, '2020-05-15'), (1, 2, '2022-08-10'), (2, 1, '2019-11-20'), (3, 3, '2018-03-05'), (4, 2, '2021-06-12');

-- 3. FLOTA DE VEHÍCULOS (Zenith Fleet)
INSERT IGNORE INTO vehiculos (id_vehiculo, numero_vehiculo, placa, marca, modelo, id_tipo_licencia, id_instructor_fijo, km_actual, km_proximo_mantenimiento, estado_mecanico) VALUES
(35, 35, 'PBA-1035', 'Chevrolet', 'Joy 2025', 1, 3, 500, 5000, 'OPERATIVO'),
(101, 101, 'PBX-1234', 'Chevrolet', 'Sail 2024', 1, 1, 4850, 5000, 'OPERATIVO'),
(102, 102, 'PBA-5678', 'Hyundai', 'Accent 2025', 1, 2, 12000, 15000, 'OPERATIVO'),
(201, 201, 'PCX-9012', 'Hino', 'Bus City 2023', 2, 4, 45200, 45500, 'OPERATIVO'),
(301, 301, 'PTX-3456', 'Mercedes', 'Actros 2024', 3, 3, 25000, 30000, 'OPERATIVO');

-- 4. CURSOS ACADÉMICOS
INSERT IGNORE INTO cursos (id_tipo_licencia, nombre, nivel, paralelo, periodo, fecha_inicio, fecha_fin, cupos_disponibles) VALUES
(1, 'Curso Conducción Liviana - Nocturno', 'Inicial', 'A', '2026-I', '2026-04-01', '2026-06-30', 18),
(2, 'Curso Profesional de Buses', 'Avanzado', 'B', '2026-I', '2026-05-15', '2026-08-15', 15),
(3, 'Maestría en Carga Pesada', 'Especialidad', 'C', '2026-I', '2026-04-10', '2026-07-10', 10);

-- 5. ESTUDIANTES
INSERT IGNORE INTO estudiantes (cedula, nombres, apellidos, telefono, email) VALUES
('1799887766', 'Carlos Andrés', 'Domínguez Paz', '0999887766', 'carlos.dp@gmail.com'),
('1799887767', 'María José', 'Guerra Santos', '0999887767', 'maria.gs@gmail.com'),
('1799887768', 'Roberto Carlos', 'Vaca Luna', '0999887768', 'roberto.vl@gmail.com'),
('1799887769', 'Anita Belén', 'Torres Vega', '0999887769', 'anita.tv@gmail.com');

-- 6. MATRÍCULAS
INSERT IGNORE INTO matriculas (cedula_estudiante, id_curso, fecha_matricula, horas_completadas) VALUES
('1799887766', 1, '2026-03-20', 5.5), ('1799887767', 1, '2026-03-21', 12.0), ('1799887768', 2, '2026-03-22', 0.0), ('1799887769', 3, '2026-03-23', 20.0);

-- 7. SESIONES ACTIVAS (Dashboard Vivo)
-- Usamos 'WHERE NOT EXISTS' para evitar duplicados en tablas sin claves únicas simples
INSERT INTO registros_salida (id_matricula, id_vehiculo, id_instructor, km_salida, observaciones_salida, registrado_por)
SELECT 1, 1, 1, 4850, 'Clase de parqueo en reversa', 1
WHERE NOT EXISTS (SELECT 1 FROM registros_salida WHERE id_matricula=1 AND id_vehiculo=1 AND id_instructor=1);

INSERT INTO registros_salida (id_matricula, id_vehiculo, id_instructor, km_salida, observaciones_salida, registrado_por)
SELECT 2, 2, 2, 12000, 'Recorrido en vía perimetral', 1
WHERE NOT EXISTS (SELECT 1 FROM registros_salida WHERE id_matricula=2 AND id_vehiculo=2 AND id_instructor=2);

-- 8. HISTORIAL DE LLEGADAS
INSERT IGNORE INTO registros_salida (id_registro, id_matricula, id_vehiculo, id_instructor, km_salida, registrado_por) VALUES
(3, 3, 3, 3, 25000, 1);
INSERT IGNORE INTO registros_llegada (id_registro, km_llegada, observaciones_llegada, registrado_por) VALUES
(3, 25100, 'Práctica de frenos exitosa', 1);
