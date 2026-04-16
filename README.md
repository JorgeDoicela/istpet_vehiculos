# ISTPET Logistics: SIGAFI Parity Edition 2026

![ISTPET Zenith Header](https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/25.png) <!-- Reemplazar con imagen real si existe -->

**ISTPET Logistics** es la plataforma de control operativo de flota y monitoreo académico definitiva del Instituto Superior Tecnológico "Mayor Pedro Traversari" (ISTPET). Diseñada para garantizar una **paridad absoluta** con la base de datos central de SIGAFI, esta edición 2026 introduce una arquitectura híbrida de alta disponibilidad y una experiencia de usuario de grado industrial.

> [!IMPORTANT]
> **Estado de Producción:** El sistema opera actualmente en **Modo Directo**, consultando SIGAFI en tiempo real. El **Modo Espejo (Sincronización Masiva)** se mantiene como un respaldo de alta disponibilidad (Standby HA) reactivable por configuración.

---

## 🚀 Innovaciones Clave (Version Final)

- **Hybrid Universal Bridge**: Arquitectura que permite conmutar entre lectura directa de SIGAFI y un espejo local sincronizado cada 15 minutos.
- **Resiliencia Industrial**: Implementación de *Circuit Breakers* (Polly) para proteger el sistema ante caídas de la base de datos central.
- **Apple Aesthetic UI**: Interfaz fluida basada en Glassmorphism, diseñada para visión nocturna y fatiga visual reducida en turnos de guardia.
- **Módulo de Reportes Pro**: Generación de reportes operativos con paridad de campos SIGAFI y exportación nativa a Excel.
- **PWA Ready**: Capacidad de instalación como aplicación de escritorio o móvil con soporte de Service Workers para resiliencia de red.

## 🏗️ Resumen Arquitectónico

El sistema utiliza un **Backend .NET 8** con C# que actúa como puente inteligente hacia SIGAFI (MySQL), exponiendo una API REST robusta al **Frontend React 19**.

- **Direct Path**: Consultas JIT (Just-In-Time) que garantizan que los datos del estudiante siempre estén frescos.
- **Standby Path**: Motor de sincronización de 23 módulos que mantiene una copia operativa total en la base de datos local `istpet_vehiculos`.
- **Audit Engine**: Registro transaccional de cada salida y llegada para trazabilidad completa.

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

## 🛠️ Despliegue Rápido

### Requisitos
- .NET 8 SDK & Node.js 20+
- MySQL Server (Local) & Acceso a SIGAFI Central

### Modo Directo (Recomendado)
1. Configure su `.env` con `DATABASE_MODE=Direct`.
2. Ejecute `infrastructure/deploy-direct-server.ps1` desde PowerShell como Administrador.
3. El sistema se auto-reparará y levantará el esquema necesario.

## 📄 Documentación Técnica Completa

Para profundizar en el funcionamiento interno, consulte los siguientes manuales:

- 📐 [Arquitectura del Sistema](file:///c:/Users/DESARROLLADOR/Desktop/Proyectos/istpet_vehiculos/docs/ARCHITECTURE.md)
- 🔌 [Especificación de API](file:///c:/Users/DESARROLLADOR/Desktop/Proyectos/istpet_vehiculos/docs/API_SPEC.md)
- 🎨 [Guía de Desarrollo Frontend](file:///c:/Users/DESARROLLADOR/Desktop/Proyectos/istpet_vehiculos/docs/FRONTEND_GUIDE.md)
- 📋 [Manual de Operaciones y HA](file:///c:/Users/DESARROLLADOR/Desktop/Proyectos/istpet_vehiculos/docs/OPERATIONS_MANUAL.md)

---
© 2026 ISTPET Zenith - Advanced Engineering Team.

