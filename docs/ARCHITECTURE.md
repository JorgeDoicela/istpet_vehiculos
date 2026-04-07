# Arquitectura del Sistema — ISTPET Logística

## Visión General

El sistema sigue una **arquitectura desacoplada por capas** (Layered Architecture) con separación estricta de responsabilidades entre el frontend, la API REST y la base de datos. Adicionalmente, incorpora un **puente de integración** con la base de datos académica central del ISTPET (sistema SIGAFI).

---

## Diagrama de Capas

```mermaid
graph TD
    subgraph "Capa de Presentación (React + Vite)"
        A[ControlOperativo.jsx<br/>Salida / Llegada] 
        B[Home.jsx<br/>Dashboard]
        C[Students.jsx / Vehicles.jsx<br/>Catálogos]
    end

    subgraph "Servicios Frontend (Axios)"
        D[logisticaService.js]
        E[dashboardService.js]
        F[studentService.js / vehicleService.js]
    end

    subgraph "API REST - .NET 8"
        G[LogisticaController]
        H[DashboardController]
        I[AuthController]
        J[SyncController]
    end

    subgraph "Capa de Servicios"
        K[SqlLogisticaService<br/>Lógica de Negocio]
        L[SqlCentralStudentProvider<br/>Puente SIGAFI]
        M[DataSyncService<br/>Ingesta Externa]
    end

    subgraph "Capa de Datos"
        N[AppDbContext<br/>EF Core + Fluent API]
        O[(MySQL<br/>istpet_vehiculos)]
        P[(MySQL<br/>sigafi_es<br/>BD Central)]
    end

    A --> D --> G --> K --> N --> O
    B --> E --> H --> N
    G --> L --> P
    J --> M --> N
```

---

## Estructura de Directorios

```
istpet_vehiculos/
├── backend/
│   ├── Controllers/        # Controladores REST (7 archivos)
│   ├── DTOs/               # Objetos de Transferencia de Datos
│   ├── Data/               # AppDbContext (EF Core)
│   ├── Mappings/           # Perfiles AutoMapper
│   ├── Middleware/         # Manejo global de errores
│   ├── Models/             # Entidades del dominio (13 modelos)
│   └── Services/
│       ├── Helpers/        # DataValidator (Sanitización)
│       ├── Interfaces/     # Contratos de servicio
│       └── Implementations/  # Implementaciones SQL reales
├── frontend/
│   └── src/
│       ├── components/
│       │   ├── common/     # StatusBadge, ThemeContext
│       │   ├── features/   # ActiveClasses, VehicleList, etc.
│       │   ├── layout/     # Layout, Sidebar
│       │   └── logistica/  # LogisticaHeader, VehicleCard
│       ├── pages/          # ControlOperativo, Home, Students, Vehicles
│       └── services/       # Clientes Axios por módulo
└── docs/
    └── Scripts/            # SQL_SCHEMA.sql, MOCK_SIGAFI_ES.sql
```

---

## Patrones de Diseño Implementados

| Patrón | Ubicación | Propósito |
| :--- | :--- | :--- |
| **Dependency Injection** | `Program.cs` | Registra servicios como `ILogisticaService`, `ICentralStudentProvider`, etc. Permite intercambiar implementaciones (ej: Mock vs SQL real) sin modificar los controladores. |
| **Repository / Service Layer** | `Services/Implementations/` | Encapsula toda la lógica de negocio. Los controladores delegan en servicios, no acceden directamente a la BD. |
| **DTO Pattern** | `DTOs/` | `ApiResponse<T>` estandariza todas las respuestas. Los DTOs de Logística (`EstudianteLogisticaResponse`, `VehiculoLogisticaResponse`) ocultan los detalles de las entidades del dominio. |
| **Adapter Pattern** | `SqlCentralStudentProvider.cs` | Traduce el esquema SIGAFI (nombres de tablas y columnas en camelCase) al modelo de dominio de ISTPET. |
| **Global Error Handler** | `ErrorHandlingMiddleware.cs` | Un único punto de captura para todas las excepciones no controladas, devolviendo siempre un `ApiResponse` coherente. |
| **AutoMapper** | `Mappings/MappingProfile.cs` | Transforma entidades de dominio a DTOs de forma automática y centralizada. |
| **Hybrid Auth Bridge** | `AuthController.cs` | Soporta dos algoritmos de hash (BCrypt legacy de SIGAFI y SHA-256 nativo) para garantizar compatibilidad al migrar usuarios. |

---

## Flujo Principal: Registro de Salida de Vehículo

```mermaid
sequenceDiagram
    participant G as Guardia (UI)
    participant API as LogisticaController
    participant S as SqlLogisticaService
    participant DB as MySQL (local)
    participant C as SqlCentralStudentProvider
    participant SDB as sigafi_es (central)

    G->>API: GET /api/logistica/estudiante/{cedula}
    API->>DB: Buscar en tablas locales (matriculas + estudiantes)
    alt Encontrado localmente
        DB-->>API: Datos del estudiante
        API->>C: GetScheduledPracticeAsync(cedula)
        C->>SDB: Query cond_alumnos_practicas WHERE fecha=CURDATE()
        SDB-->>C: Práctica agendada (si existe)
        API-->>G: EstudianteLogisticaResponse + datos de agenda
    else No encontrado localmente
        API->>C: GetFromCentralAsync(cedula)
        C->>SDB: Query cross-database a sigafi_es.alumnos + matriculas
        SDB-->>C: Datos crudos SIGAFI
        C-->>API: CentralStudentDto
        API->>DB: Auto-registra Estudiante + Matrícula local
        API-->>G: EstudianteLogisticaResponse (origen: SIGAFI Bridge)
    end

    G->>API: POST /api/logistica/salida
    API->>S: RegistrarSalidaAsync(idMatricula, idVehiculo, idInstructor)
    S->>DB: Valida: vehículo operativo, no en uso, instructor libre, estudiante no en pista
    S->>DB: INSERT registros_salida (dentro de transacción)
    DB-->>S: OK
    S-->>API: "EXITO"
    API-->>G: ApiResponse<string> {success: true}
```

---

## Estandarización de Respuestas API

Todos los endpoints retornan el siguiente envelope genérico:

```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": { ... },
  "timestamp": "2026-04-07T03:00:00Z"
}
```

En caso de error:
```json
{
  "success": false,
  "message": "Descripción del problema",
  "data": null,
  "timestamp": "2026-04-07T03:00:00Z"
}
```

---

## Estándares de Código

- **Backend (C#)**: Nomenclatura `PascalCase` para clases, métodos y propiedades.
- **Frontend (JavaScript)**: Nomenclatura `camelCase` para variables y funciones.
- **Base de Datos (MySQL)**: Nomenclatura `snake_case` para tablas y columnas.
- **Mapeo de nombres**: Fluent API de EF Core resuelve la discrepancia entre `snake_case` (SQL) y `PascalCase` (C#).
- **Iconografía**: SVG inline (Heroicons) en el frontend. Sin dependencias de icon fonts.
