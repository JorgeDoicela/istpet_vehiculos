# Guía de Operación Funcional ISTPET

Este manual instruye al usuario final sobre cómo operar el Dashboard de modernización de ISTPET.

## Dashboard de Monitoreo

### 1. Panel de "Escudo de Datos" (Sincronización)
- **Propósito**: Ingerir datos de alumnos o vehículos desde fuentes externas.
- **Cómo usar**: Haz clic en el botón "Ejecutar Sincronización Segura". El sistema disparará una validación en la "Aduana Digital" y te mostrará un reporte instantáneo de:
  - **Aceptados**: Registros que pasaron el filtro de calidad.
  - **Rechazados**: Datos corruptos o incompletos bloqueados para proteger tu base de datos.

### 2. Monitoreo de Clases Activas
- **Vista**: Panel derecho del Dashboard.
- **Función**: Muestra en tiempo real qué alumnos e instructores están en ruta sincronizando los datos de las tablas `registros_salida` y `registros_llegada` (vía Vistas SQL).

### 3. Alertas de Mantenimiento
- **Ubicación**: Banner superior (si existen alertas).
- **Lógica**: Se activa automáticamente cuando un vehículo se marca como en mantenimiento o fuera de servicio en la tabla `vehiculos`.

## Gestión Académica y de Flota

### Búsqueda de Estudiantes
- **Cómo usar**: Ingresa la cédula del alumno en el buscador central.
- **Resultado**: El sistema consultará el perfil a través de la API y presentará los datos mapeados (Nombre Completo, Cédula, Estado).

### Listado Dinámico de Unidades
- **Función**: Presenta las tarjetas de los vehículos operativos.
- **Nota técnica**: Estas tarjetas son adaptativas. Si el administrador agrega cualquier campo adicional al vehículo en el backend, éste aparecerá automáticamente en la tarjeta sin cambios en el sistema.

## Resolución de Problemas (FAQ)
- **Problema**: El buscador no devuelve resultados.
  - **Solución**: Asegúrate de que el Backend esté corriendo en el puerto 5112 y que el alumno esté registrado en la tabla `estudiantes`.
- **Problema**: La sincronización arroja muchos rechazos.
  - **Solución**: Verifica el formato de los datos de la fuente externa (ej: longitud de cédula o formato de email).
