# Arquitectura y Patrones de Diseño: ISTPET System

Este documento detalla la estructura técnica y las decisiones de diseño que hacen que el sistema sea altamente escalable.

## Arquitectura General
El sistema sigue los principios de **Arquitectura Limpia (Clean Architecture)** y **Desacoplamiento de Capas**, separando estrictamente la responsabilidad de cada componente.

### 1. Backend (.NET 8 Web API)
- **Modelos de Dominio**: Representación directa de las 11 tablas del script SQL original.
- **DTO Layer (Data Transfer Objects)**: Capa de abstracción que asegura que los detalles internos de la base de datos nunca se expongan directamente.
- **AutoMapper Automation**: Implementación del patrón de mapeo automático para permitir la## Sincronización e Integridad de Datos

### Sincronización Inteligente de Estudiantes
El sistema implementa un mecanismo de "succión" de datos desde la **Base de Datos Central del ISTPET** para maximizar la eficiencia en las garitas:
1.  **Búsqueda Local**: Se prioriza la consulta en las tablas locales para máxima velocidad.
2.  **Puente Central (Real-time Bridge)**: Si el alumno no existe localmente, el sistema consulta la base académica central mediante SQL directo.
3.  **Persistencia Automática**: Una vez localizado el alumno en la central, sus datos se **guardan permanentemente** en las tablas locales (`estudiantes` y `matriculas`). Esto asegura que el sistema siga funcionando incluso si la conexión con la base central se interrumpe en el futuro.

## Evolución y Migraciones
tema sin cambios manuales en los controladores.
- **Service Layer**: Lógica de negocio encapsulada que consume el `AppDbContext` de Entity Framework Core.
- **Integration Layer (Smart Sync)**: Motor de sincronización que conecta la base de datos de logística con la **Base de Datos Central del ISTPET**. Utiliza el patrón **Adapter** para succionar datos academicos y persistirlos localmente.

### 2. Frontend (React + Vite + Tailwind CSS)
- **Component-Based Architecture**: Interfaz modular y reutilizable.
- **Dynamic Rendering**: Patrón de renderizado dinámico en las vistas para soportar campos nuevos desde el backend automáticamente.
- **Axios Interceptors**: Manejo global de comunicación y errores, simplificando la lógica de los servicios individuales.

## Patrones de Diseño Implementados

| Patrón | Propósito en ISTPET |
| :--- | :--- |
| **Dependency Injection** | Facilita las pruebas y el intercambio de componentes (ej: cambiar de Mock a SQL). |
| **Adapter Pattern** | El `DataSyncService` permite que el sistema "beba" datos de fuentes desconocidas y los adapte a tus tablas locales. |
| **Data Transfer Object** | Protege la integridad de los datos y permite versionar la API sin romper la interfaz de usuario. |
| **Global Error Handling** | Middleware que garantiza que el sistema siempre responda con éxito o error controlado (`ApiResponse`). |
| **Sanitizer / Validator** | Capapa de "Escudo de Datos" que limpia e inspecciona datos externos antes de la persistencia. |

## Estándares de Código
- **Naming**: PascalCase para C# y camelCase para Javascript.
- **Iconografía**: 100% SVG Vector Icons (Heroicons) para evitar dependencias pesadas y emojis.
- **Responsividad**: Mobile-first design usando Tailwind CSS.
