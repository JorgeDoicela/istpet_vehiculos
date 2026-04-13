# Hoja de Ruta (Roadmap) Evolutivo: ISTPET Logística

Estado actual del sistema: **Producción / Grado Industrial**
Nivel de Madurez: **9/10 (Core Logic & Infrastructure Complete)**

---

## 1. Logros Consolidados (Fase 1: Estabilización)

A la fecha, el sistema ha completado sus pilares fundamentales de arquitectura:

*   [x] **Puente Híbrido Universal**: Motor de búsqueda paralelo SIGAFI/Local con inyección JIT.
*   [x] **Resilience Pipeline**: Implementación de Circuit Breaker (Polly) para operaciones aisladas.
*   [x] **Seguridad de Grado Industrial**: Autenticación dual (BCrypt/Native) y Hardening de JWT de 256-bits.
*   [x] **Schema Healer Protocol**: Auto-gestión del esquema de base de datos en tiempo de ejecución.
*   [x] **Escudo de Datos (Data Shield)**: Ingesta protegida con sanitización y truncamiento preventivo.
*   [x] **Audit Ledger**: Sistema forense de registro de transacciones con captura de metadatos de red.

---

## 2. Próximos Horizontes (Fase 2: Inteligencia y Movilidad)

### Prioridad Alta (Q2 2026)
*   **Business Intelligence (Power BI)**: Exposición de las vistas `v_clases_activas` e históricos mediante un conector dedicado para análisis de KPIs.
*   **Gestión de Mantenimiento Avanzada**: UI para el registro de partes mecánicas y alertas predictivas basadas en horas de rodaje acumuladas.
*   **Webhooks de Notificación**: Integración con bots de mensajería para alertas de "Vehículo Retornando" o "Cambio de Turno".

### Prioridad Media (Q3 2026)
*   **Mobile-First Sync PWA**: Optimización del frontend para tablets de baja gama en garita, con capacidad de buffer offline.
*   **Geolocalización Pasiva**: Integración de coordenadas GPS registradas en el momento del CHECK-IN y CHECK-OUT para verificación de zona.

---

## 3. Visión Long-Term (Fase 3: Ecosistema Autónomo)
*   **IA de Asignación Predictiva**: Recomendador automático de vehículos basado en el consumo de combustible y la carga de trabajo equitativa entre instructores.
*   **Integración con Sensores IoT**: Telemetría básica (nivel de batería/combustible) sincronizada directamente al Dashboard del Guardia.

---

## 4. Gestión de la Deuda Técnica

Para mantener la excelencia técnica, se propone el siguiente ciclo de mantenimiento:
1.  **Refactorización de AutoMapper**: Migrar las proyecciones LINQ directas en controladores a perfiles de mapeo puros.
2.  **Expansión de Cobertura de Tests**: Alcanzar un 70% de cobertura en los servicios de lógica de negocio (`SqlLogisticaService`).
3.  **Auditoría de Índices**: Optimizar los índices en las tablas locales (`alumnos`, `matriculas`) dado el volumen masivo (47k+ registros) del espejo central.
