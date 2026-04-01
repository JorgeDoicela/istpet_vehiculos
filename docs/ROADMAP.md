# Hoja de Ruta ISTPET: Próximos Pasos

Este documento describe la visión futura y las funcionalidades recomendadas para el escalamiento del sistema de gestión de la escuela de conducción.

## Fase 1: Seguridad Avanzada
- **Login con JWT**: Implementación de autenticación segura utilizando la tabla `usuarios` del script original.
- **Roles y Permisos Dinámicos**: Control de acceso a nivel de componente para administradores vs guardias.
- **Refresh Tokens**: Sesiones persistentes de larga duración.

## Fase 2: Automatización e IA
- **Integración GPS Real**: Conexión con trackers en los vehículos para actualizar la vista `v_clases_activas` mediante coordenadas reales.
- **Predicción de Mantenimiento**: Algoritmo que analice la frecuencia de uso para avisar de mantenimientos antes de que se cumpla el kilometraje crítico.
- **Reportes Inteligentes**: Exportación automática de logs de sincronización a formatos Excel/PDF.

## Fase 3: Ecosistema y Escalabilidad
- **API Externa de Consulta**: Abrir el "Sync Hub" para que otras instituciones puedan consultar el estado académico de sus alumnos en ISTPET.
- **Notificaciones Push**: Alertas automáticas al instructor cuando su vehículo asignado requiera mantenimiento.
- **App Móvil**: Cliente nativo para reporte de salida/llegada directamente desde el vehículo.

## Mejoras Técnicas Continuas
- **Unit Testing**: Incremento de cobertura de pruebas en la capa de servicios.
- **Dockerization**: Empaquetado del sistema en contenedores para despliegue en cualquier proveedor de nube (AWS, Azure, DigitalOcean).
- **Caché Distribuida**: Implementación de Redis para acelerar las consultas a las vistas de monitoreo más pesadas.
