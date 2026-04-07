# Estado del Sistema — ISTPET Logística (2026)

**Fecha de corte:** Abril 2026

---

## Estado General del Proyecto

| Área | Estado | Cobertura |
| :--- | :--- | :--- |
| Control Operativo (Salida/Llegada) | Funcional | 100% |
| Puente Híbrido SIGAFI | Funcional | 100% |
| Dashboard de Monitoreo | Funcional | 80% (sin auto-refresh) |
| Catálogos (Estudiantes, Vehículos) | Funcional (solo lectura) | 60% (sin CRUD completo) |
| Autenticación | Parcial | 40% (login sin JWT) |
| Gestión Administrativa (CRUD) | Pendiente | 10% |
| Pruebas Unitarias | Pendiente | 0% |

---

## Lo Que Está Implementado

### Backend (.NET 8)

- **7 Controladores REST:** Auth, Dashboard, Estudiantes, Logistica, Sync, TipoLicencia, Vehiculos.
- **Puente Híbrido Universal:** Búsqueda de estudiantes e instructores con fallback automático a la BD SIGAFI.
- **Lógica Transaccional:** `SqlLogisticaService` ejecuta validaciones de negocio dentro de transacciones MySQL atómicas.
- **Autenticación Dual:** Soporta BCrypt (usuarios SIGAFI) y SHA-256 (usuarios nativos) simultáneamente.
- **Data Shield:** `DataSyncService` con `DataValidator` para ingesta segura de datos externos.
- **Middleware Global de Errores:** Todo error no controlado devuelve un `ApiResponse` limpio.
- **AutoMapper:** Perfiles configurados para `Estudiante → EstudianteDto` y `Vehiculo → VehiculoDto`.
- **Swagger:** Disponible en desarrollo en `/swagger`.

### Base de Datos (MySQL)

- **11 tablas** + **1 tabla de auditoría** (`sync_logs`) = 12 tablas totales.
- **2 vistas SQL:** `v_clases_activas` y `v_alerta_mantenimiento`.
- **Integridad referencial completa** con constraints FK en todas las relaciones.
- **Collation español:** `utf8mb4_spanish_ci` para soporte nativo de tildes.
- **Script completo de creación:** `docs/Scripts/SQL_SCHEMA.sql`.
- **Script de simulación SIGAFI:** `docs/Scripts/MOCK_SIGAFI_ES.sql`.

### Frontend (React 19 + Vite)

- **4 páginas:** ControlOperativo, Home (Dashboard), Students, Vehicles.
- **11 componentes** organizados en `common/`, `features/`, `layout/`, `logistica/`.
- **Design System personalizado:** Variables CSS (`--apple-*`, `--istpet-*`) con modo claro/oscuro.
- **Autocompletado predictivo:** Debounce + fusión de resultados locales (SIGAFI) e históricos (BD local).
- **Detección de agenda:** Si el estudiante tiene práctica hoy en SIGAFI, se muestra y pre-selecciona el vehículo.
- **Reloj en tiempo real:** Actualización cada segundo para los formularios de salida/llegada.
- **Servicios Axios modularizados:** Un archivo `.js` por módulo de negocio.

### DevOps (GitHub Actions)

- **2 Pipelines:** Backend CI (build + publish en .NET 8), Frontend CI (npm install + Vite build).
- **Paths filtrados:** Los pipelines solo corren cuando hay cambios en su área (`backend/**` o `frontend/**`).
- **Ramas protegidas:** Pipelines activos en `main` y `develop`.

---

## Inventario de Archivos Clave

```
backend/
├── Controllers/          7 archivos
├── DTOs/                 4 archivos (ApiResponse, Auth, Domain, Logistica)
├── Data/                 AppDbContext.cs (182 líneas, 14 DbSets)
├── Mappings/             MappingProfile.cs
├── Middleware/           ErrorHandlingMiddleware.cs
├── Models/               13 modelos de dominio
└── Services/
    ├── Helpers/          DataValidator.cs
    ├── Interfaces/       4 contratos de servicio
    └── Implementations/  5 implementaciones

frontend/src/
├── pages/    4 páginas (ControlOperativo: 627 líneas)
├── components/ 10+ componentes
└── services/ 5 clientes Axios

docs/Scripts/
├── SQL_SCHEMA.sql        Script principal (213 líneas)
├── SQL_MIGRATION_2026.sql Script de migración
└── MOCK_SIGAFI_ES.sql    Simulador de BD Central (98 líneas)
```

---

## Dependencias NuGet Actuales

| Paquete | Versión | Uso |
| :--- | :--- | :--- |
| `AutoMapper` | 12.0.1 | Mapeo automático Entity → DTO |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 12.0.1 | Integración DI |
| `BCrypt.Net-Next` | 4.1.0 | Validación de hashes BCrypt (cuentas SIGAFI) |
| `Microsoft.AspNetCore.OpenApi` | 8.0.0 | Soporte OpenAPI / Swagger |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.0 | Herramientas de migraciones |
| `Microsoft.EntityFrameworkCore.Relational` | 8.0.0 | Soporte SQL relacional |
| `Pomelo.EntityFrameworkCore.MySql` | 8.0.0 | Provider MySQL para EF Core |
| `Swashbuckle.AspNetCore` | 8.0.0 | Interfaz Swagger UI |

## Dependencias npm Actuales

| Paquete | Versión | Uso |
| :--- | :--- | :--- |
| `react` + `react-dom` | 19.2 | Framework de UI |
| `react-router-dom` | 7.13 | Routing SPA |
| `axios` | 1.14 | Cliente HTTP |
| `vite` | 8.0 | Bundler y dev server |
| `tailwindcss` | 3.4 | Framework CSS |
| `@vitejs/plugin-react` | 6.0 | Plugin React para Vite |
