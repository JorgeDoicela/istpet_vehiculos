# Especificación Técnica del Script de Base de Datos

Este documento detalla la estructura, lógica y reglas de negocio contenidas en el script original `istpet_vehiculos`.

## Información General
- **Base de Datos**: `istpet_vehiculos`
- **Codificación**: `utf8mb4_spanish_ci` (Soporte completo para tildes y caracteres especiales)
- **Sujeto**: Gestión de Escuela de Conducción ISTPET
- **Licencias Soportadas**: C (Profesional - Taxis), D (Buses), E (Camiones)

## 1. Seguridad y Acceso
- **Tabla `usuarios`**: Control de acceso centralizado.
- **Roles**: `admin` (Gestión total) y `guardia` (Control de entrada/salida).
- **Seguridad**: Las contraseñas se gestionan mediante `password_hash` (en el script se inicializan con SHA2).

## 2. Gestión de Recursos (RRHH y Flota)
- **Capa Maestros**: Tablas `tipo_licencia` e `instructores`.
- **Relaciones N:M**: `instructor_licencias` permite que un instructor maneje múltiples categorías de licencias.
- **Flota de Vehículos**: Tabla `vehiculos` vincula cada unidad con un tipo de licencia necesaria y un instructor fijo. Incluye control de `km_actual` y `estado_mecanico`.
- **Mantenimientos**: Registro detallado de intervenciones mecánicas y costos asociados.

## 3. Estructura Académica
- **cursos**: Gestión de paralelos, jornadas (Matutina, Vespertina, Nocturna, FDS) y cupos.
- **estudiantes**: Registro único de personas por cédula.
- **matriculas**: Vínculo académico que rastrea las `horas_completadas` de práctica.

## 4. Control Logístico y Operativo
Es el núcleo transaccional del sistema:
- **registros_salida**: Registra el inicio de una clase práctica, capturando el kilometraje inicial.
- **registros_llegada**: Documenta el retorno del vehículo y actualiza automáticamente el kilometraje de la unidad.

## 5. Lógica de Negocio Automatizada (Triggers y Procesos)

### Trigger: `tg_actualizar_cupos_after_matricula`
Automatiza el control de cupos. Al insertar una nueva matrícula, disminuye automáticamente los `cupos_disponibles` del curso correspondiente.

### Procedimiento: `sp_registrar_salida`
Incluye una lógica de validación experta antes de permitir que un vehículo salga a pista:
- Se verifica que el vehículo esté `OPERATIVO`.
- Se valida que el vehículo NO esté actualmente en uso.
- Se asegura que el instructor NO esté en otra clase simultánea.
- Se confirma que el estudiante NO tenga otra clase activa en ese momento.

### Procedimiento: `sp_registrar_llegada`
Gestiona el cierre de la jornada de práctica:
- Valida que el kilometraje de llegada sea coherente con el de salida.
- Calcula automáticamente las horas de práctica realizadas basándose en el tiempo transcurrido.
- Actualiza el `km_actual` del vehículo y suma las horas al registro del estudiante en `matriculas`.

## 6. Monitoreo Inteligente (Vistas)
- **`v_clases_activas`**: Dashboard en tiempo real de quién está en pista, con qué vehículo y qué instructor.
- **`v_alerta_mantenimiento`**: Sistema de alerta temprana para vehículos que estén a menos de 500km de su próximo mantenimiento o marcados en reparación.
