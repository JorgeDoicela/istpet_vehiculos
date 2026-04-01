# ISTPET Management System: Estado del Arte 2026

Este documento detalla la arquitectura, las capacidades actuales y la visión futurista del sistema modernizado para la Escuela de Conducción Profesional ISTPET.

## 1. Arquitectura de Blindaje Zenith
Hemos implementado una infraestructura desacoplada de alto rendimiento:

- **Backend (.NET 8 Enterprise)**:
    - **Clean Architecture**: Capas separadas para asegurar que la lógica de negocio sea independiente de la base de datos.
    - **Grand Mapping 2026**: Uso de Fluent API en AppDbContext para mapear de forma explícita propiedades de C# a columnas de MySQL (snake_case), eliminando errores de sincronización.
    - **Standard Response Layer**: Implementación de ApiResponse, un envoltorio que estandariza todas las salidas del servidor, facilitando la telemetría en el frontend.
- **Frontend (React 19 + Vite + Tailwind)**:
    - **Diseño Apple Light 2026**: Estética de vanguardia con Glassmorphism, gradientes de malla y tipografía de precisión.
    - **Capa de Servicios Resiliente**: Servicios de red con desempaquetado automático de datos y blindaje contra valores nulos o indefinidos.

## 2. Capacidades Implementadas (Hitos Logrados)
Actualmente, el sistema ya es capaz de:

### A. Gestión de Flota Inteligente
- **Inventario Maestro**: Control total de vehículos, placas, marcas y kilometraje activo.
- **Monitor de Salud Mecánica**: Seguimiento en tiempo real del estado (OPERATIVO, MANTENIMIENTO, FUERA_SERVICIO).
- **Alertas de Taller**: Sistema de predicción que detecta automáticamente unidades a menos de 500 km del próximo mantenimiento.

### B. Dashboard de Control Logístico
- **Monitor En Ruta**: Visualización dinámica de clases activas mediante la vista v_clases_activas, integrando datos de estudiante, instructor, vehículo y hora de salida.
- **Telemetría de Red**: Logs inyectados en la consola del navegador para diagnóstico inmediato de la salud del sistema.

### C. Ecosistema de Datos (SQL Integrity)
- **Esquema de 11 Tablas**: Sincronización absoluta entre modelos e identidad de base de datos.
- **Lógica en Base de Datos**: Triggers para control de cupos y Procedimientos Almacenados (sp_registrar_salida, sp_registrar_llegada) para asegurar la consistencia del kilometraje.
- **Tooling de Recuperación**: Existencia de SQL_HEALER y SQL_SEEDER para mantenimiento preventivo y carga de datos de prueba.

## 3. Hoja de Ruta: Próximos Pasos (Visión 2026)
Basándonos en la solidez actual, el sistema evolucionará en las siguientes fases:

### Fase 1: Seguridad y Acceso (JWT Zenith)
- **Autenticación**: Implementación de Tokens JWT para proteger los endpoints.
- **RBAC (Role Based Access Control)**: Definición estricta de permisos para Administradores (acceso total) y Guardias (solo registro de salidas/llegadas).

### Fase 2: Registro de Actividad Operativa
- **Formularios de Flujo**: Creación de la interfaz para que el guardia registre la salida de vehículos con un solo clic, validando que el instructor y el estudiante estén libres.
- **Control de Retorno**: Interfaz de llegada con actualización automática del kilometraje del vehículo y horas prácticas del estudiante.

### Fase 3: Portal Académico
- **Ficha del Estudiante**: Vista detallada de progreso con barra de porcentaje de horas completadas.
- **Gestión de Matrículas**: Proceso digital para asignar estudiantes a cursos y tipos de licencia específicos.

### Fase 4: Reportes y Analítica
- **Generación de PDFs**: Reportes de mantenimiento y certificados de asistencia.
- **Gráficos de Uso**: Visualización de la eficiencia de la flota y horas pico de entrenamiento.

---
> [!IMPORTANT]
> El sistema ha pasado de ser una aplicación básica a un ecosistema de gestión robusto, estético e indestructible. La base de datos es ahora el espejo exacto del código fuente.
