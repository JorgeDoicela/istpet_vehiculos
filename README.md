# ISTPET Logistics: SIGAFI Parity Edition 2026

![ISTPET Zenith Header](https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/25.png) <!-- Reemplazar con imagen real si existe -->

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19.0-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://react.dev/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0+-4479A1?style=for-the-badge&logo=mysql&logoColor=white)](https://www.mysql.com/)
[![Vite](https://img.shields.io/badge/Vite-5.0+-646CFF?style=for-the-badge&logo=vite&logoColor=white)](https://vitejs.dev/)

**ISTPET Logistics** es la plataforma de control operativo de flota y monitoreo académico definitiva del Instituto Superior Tecnológico "Mayor Pedro Traversari" (ISTPET). Diseñada para garantizar una **paridad absoluta** con la base de datos central de SIGAFI, esta edición 2026 introduce una arquitectura híbrida de alta disponibilidad y una experiencia de usuario de grado industrial.

> [!IMPORTANT]
> **Estado de Producción:** El sistema opera actualmente en **Modo Directo**, consultando SIGAFI en tiempo real. El **Modo Espejo (Sincronización Masiva)** se mantiene como un respaldo de alta disponibilidad (Standby HA) reactivable por configuración.

---

## 🚀 Innovaciones Clave (Version Final)

- **Hybrid Universal Bridge**: Arquitectura que permite conmutar entre lectura directa de SIGAFI y un espejo local sincronizado.
- **Resiliencia Industrial**: Implementación de *Circuit Breakers* (Polly) para proteger el sistema ante caídas de la base de datos central.
- **Apple Aesthetic UI**: Interfaz fluida basada en Glassmorphism, diseñada para visión nocturna y fatiga visual reducida en turnos de guardia.
- **Módulo de Reportes Pro**: Generación de reportes operativos con paridad de campos SIGAFI y exportación nativa a Excel vía SheetJS.
- **PWA Ready**: Capacidad de instalación como aplicación de escritorio o móvil con soporte de Service Workers para resiliencia de red local.

---

## 🏗️ Ecosistema Técnico

El sistema utiliza un **Backend .NET 8** con C# que actúa como puente inteligente hacia SIGAFI (MySQL), exponiendo una API REST robusta al **Frontend React 19**.

| Módulo | Base Tecnológica | Responsabilidad Principal |
| :--- | :--- | :--- |
| **API Core** | ASP.NET Core 8 Web API | Enrutamiento, validación y exposición RESTful. |
| **Data Layer** | EF Core 8 + Pomelo MySQL | Mapeo Objeto-Relacional y ejecución JIT. |
| **Sync Engine** | Background Services | Replicación masiva de 23 tablas desde SIGAFI (Modo HA). |
| **UI Framework** | React 19 + Tailwind CSS | Renderizado concurrente y sistema de diseño atómico. |

---

## 📁 Estructura del Proyecto

```bash
istpet_vehiculos/
├── backend/                # API .NET 8 (Paridad SIGAFI)
│   ├── Controllers/        # Endpoints de Lógica y Sync
│   ├── Hosting/            # Servicios en Background (Standby Mode)
│   ├── Models/             # 30+ Entidades de Dominio
│   ├── Services/           # Puentes, Resiliencia y Auditoría
│   └── backend.Tests/      # Pruebas Unitarias de Seguridad
├── frontend/               # SPA React 19 (Apple Aesthetic)
│   ├── src/
│   │   ├── components/     # UI Atómica y Funcional
│   │   ├── context/        # Estado Global (Alertas, Auth)
│   │   ├── pages/          # Control Operativo, Reportes, Home
│   │   └── utils/          # Normalización JIT
│   └── public/             # PWA Manifest & Icons
├── infrastructure/         # Orquestación Docker & Env
└── scripts/                # Automatización de Release & Bundle
```

---

## 🛠️ Despliegue Rápido

### Requisitos Previos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- MySQL 8.0+ o MariaDB 11.0+ (Local)
- VPN o red interna para acceso a SIGAFI Central

### Modo Directo (Recomendado para Producción)
1. Configure su archivo `.env` en la raíz asegurando `DATABASE_MODE=Direct`.
2. Ejecute el script de despliegue desde PowerShell (como Administrador):
   ```powershell
   .\infrastructure\deploy-direct-server.ps1
   ```
3. El *Schema Healer* (Auto-reparación) se encargará de levantar la estructura de Base de Datos necesaria automáticamente.

---

## 📄 Documentación Técnica Completa

Para una comprensión profunda de las decisiones de ingeniería y el funcionamiento interno, consulte los siguientes manuales alojados en el directorio `/docs`:

- 📐 **[Arquitectura del Sistema](file:///c:/Users/DESARROLLADOR/Desktop/Proyectos/istpet_vehiculos/docs/ARCHITECTURE.md)**: Análisis del "Hybrid Bridge" y resiliencia.
- 🔌 **[Especificación de API](file:///c:/Users/DESARROLLADOR/Desktop/Proyectos/istpet_vehiculos/docs/API_SPEC.md)**: Contratos detallados y flujos de paridad.
- 🎨 **[Guía Frontend](file:///c:/Users/DESARROLLADOR/Desktop/Proyectos/istpet_vehiculos/docs/FRONTEND_GUIDE.md)**: Implementación de la estética Glassmorphism y React 19.
- 📋 **[Manual de Operaciones](file:///c:/Users/DESARROLLADOR/Desktop/Proyectos/istpet_vehiculos/docs/OPERATIONS_MANUAL.md)**: Guía crítica para administración de Alta Disponibilidad (HA).

---
© 2026 ISTPET Zenith - Advanced Engineering Team.
