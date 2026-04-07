# Sistema de Gestión de Flota y Logística de Prácticas - ISTPET

Sistema web para la gestión operativa de la escuela de conducción del Tecnológico Traversari (ISTPET). Automatiza el control de salida y llegada de vehículos, la sincronización de estudiantes desde el sistema académico central (SIGAFI) y el monitoreo en tiempo real de la flota.

---

## Ecosistema Tecnológico

| Capa | Tecnología | Versión |
| :--- | :--- | :--- |
| **Backend API** | .NET 8 Web API + Entity Framework Core | 8.0 |
| **ORM / Datos** | Pomelo EF Core for MySQL | 8.0 |
| **Base de Datos** | MySQL / MariaDB | 11+ |
| **Frontend** | React + Vite + Tailwind CSS | 19 / 8 / 3.4 |
| **Comunicación** | Axios (REST JSON) | 1.14 |
| **Mapeo Automático** | AutoMapper | 12.0 |
| **Hash de Contraseñas** | BCrypt.Net + SHA-256 | 4.1 |
| **CI/CD** | GitHub Actions | v4 |

---

## Módulos del Sistema

| Módulo | Ruta | Descripción |
| :--- | :--- | :--- |
| **Control Operativo** | `/` | Panel principal: registro de salida y llegada de vehículos |
| **Monitoreo** | `/monitoreo` | Dashboard con clases activas y alertas de mantenimiento |
| **Estudiantes** | `/estudiantes` | Catálogo y búsqueda de estudiantes |
| **Vehículos** | `/vehiculos` | Catálogo y estado de la flota |

---

## Documentación Técnica

### Arquitectura e Ingeniería
- **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** — Capas, patrones de diseño y flujo de datos
- **[API_SPEC.md](docs/API_SPEC.md)** — Especificación completa de endpoints REST

### Base de Datos
- **[DATABASE.md](docs/DATABASE.md)** — Esquema ERD, 11 tablas y vistas SQL
- **[SQL_SCHEMA.md](docs/SQL_SCHEMA.md)** — Script de creación de base de datos comentado
- **[CENTRAL_DB.md](docs/CENTRAL_DB.md)** — Integración con la BD Central SIGAFI del ISTPET

### Operaciones y Seguridad
- **[INSTALLATION.md](docs/INSTALLATION.md)** — Guía de configuración y puesta en marcha
- **[SECURITY_DATA_SHIELD.md](docs/SECURITY_DATA_SHIELD.md)** — Autenticación híbrida y protección de datos
- **[PIPELINES.md](docs/PIPELINES.md)** — Pipelines de CI/CD con GitHub Actions

### Usuario y Hoja de Ruta
- **[USER_GUIDE.md](docs/USER_GUIDE.md)** — Manual de operación para el guardia/administrador
- **[ROADMAP.md](docs/ROADMAP.md)** — Funcionalidades pendientes y mejoras futuras

---

## Inicio Rápido

### Requisitos Previos
- .NET 8 SDK
- Node.js 20+
- MySQL 8+ (o MariaDB 11+)

### 1. Base de Datos
```sql
-- Ejecutar en MySQL:
SOURCE docs/Scripts/SQL_SCHEMA.sql;

-- (Opcional) Para pruebas con SIGAFI simulado:
SOURCE docs/Scripts/MOCK_SIGAFI_ES.sql;
```

### 2. Backend
```bash
cd backend
# Ajustar la cadena de conexión en appsettings.json
dotnet restore
dotnet run
# API disponible en: http://localhost:5000
# Swagger UI en: http://localhost:5000/swagger
```

### 3. Frontend
```bash
cd frontend
npm install
npm run dev
# UI disponible en: http://localhost:5173
```

---

*Proyecto desarrollado para la Escuela de Conducción del ISTPET. Sistema de logística de prácticas de manejo.*
