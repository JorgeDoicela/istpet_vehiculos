# Especificación Técnica de la API ISTPET

Este documento detalla los endpoints principales, formatos de datos y protocolos de comunicación del backend desarrollado en .NET 8.

## Estándar de Respuesta (ApiResponse)
Todas las respuestas siguen un formato estándar para facilitar el consumo en el frontend:

```json
{
  "sucess": true,
  "data": { ... },
  "message": "Operación exitosa"
}
```

## Endpoints Principales

### 1. Vehículos
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/Vehiculos` | Lista completa de la flota con autotransformación DTO. |
| **GET** | `/api/Vehiculos/{placa}` | Detalle de una unidad específica por placa. |

### 2. Estudiantes
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/Estudiantes/{cedula}` | Consulta de perfil de alumno con mapeo de Nombre Completo. |

### 3. Sincronización (Data Sync Hub)
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **POST** | `/api/Sync/students` | Ingesta de datos externos masivos con validación de "Aduana". |

**Ejemplo de Payload para Sincronización:**
```json
[
  {
    "id_externo": "17xxxxxxxx",
    "nombre_completo": "Juan Perez",
    "correo_universidad": "juan@istpet.edu"
  }
]
```

## Códigos de Estado HTTP
- **200 OK**: Operación exitosa.
- **404 Not Found**: El recurso solicitado (cedula/placa) no existe.
- **500 Internal Server Error**: Error capturado por el Global Exception Middleware.

## Pruebas (Swagger)
Puedes probar interactivamente estos endpoints accediendo a:
`http://localhost:5112/swagger/index.html`
