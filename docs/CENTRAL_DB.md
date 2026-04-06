# Guía de Conexión: Base de Datos Central ISTPET

Has activado la arquitectura de **Puente de Datos Real**. El sistema de logística de vehículos ahora está preparado para buscar alumnos directamente en la base de datos centralizada del instituto.

## Cómo activar la conexión real

Sigue estos pasos cuando tengas el nombre de la base de datos central:

1.  Abre el archivo: `backend/Services/Implementations/SqlCentralStudentProvider.cs`
2.  Busca la línea número **15**:
    ```csharp
    private const string CENTRAL_DB_NAME = "ISTPET_CENTRAL_DB";
    ```
3.  Reemplaza `"ISTPET_CENTRAL_DB"` por el nombre real (ejemplo: `"academico_central"`).
4.  Guarda el archivo y reinicia el servidor.

## Requisitos de Base de Datos

Para que la "Succión de Datos" funcione, la base de datos central debe tener:
*   Una tabla llamada `estudiantes`.
*   Columnas llamadas: `cedula`, `nombres`, `apellidos`.
*   El usuario de MySQL (`root` en desarrollo) debe tener permisos de lectura en ambas bases de datos.

## Qué hace el sistema automáticamente (Smart Sync)

Cuando un guardia ingresa una cédula:
1.  **Busca Localmente**: Prioriza la velocidad usando los datos ya guardados.
2.  **Succión desde la Central**: Si es la primera vez del alumno, lo busca en la BD Académica mediante el puente SQL real.
3.  **Persistencia y Auto-Matrícula**: Al encontrarlo, **guarda una copia local permanente** del alumno y le asigna un curso automáticamente. Esto crea un respaldo de datos propio del sistema de vehículos.

> [!IMPORTANT]
> **Autonomía y Respaldo**: Este proceso garantiza que, una vez que un alumno ha sido "succionado" por primera vez, el sistema de vehículos ya no depende de la base central para ese alumno específico, asegurando que el registro de entradas y salidas nunca se detenga por problemas de red externa.

> [!TIP]
> Esta arquitectura evita que tengas que registrar al mismo alumno en dos sistemas diferentes. ¡La potencia de la integración de datos!
