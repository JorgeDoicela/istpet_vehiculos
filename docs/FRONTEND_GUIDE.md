# Guía de Ingeniería Frontend: React 19 — Zenith Edition

Este documento detalla la arquitectura, el sistema de diseño y los patrones de desarrollo utilizados en la interfaz de usuario de ISTPET Vehículos 2026.

---

## 1. Stack Tecnológico Industrial

| Tecnología | Versión | Rol Crítico |
| :--- | :--- | :--- |
| **React** | 19.0 | Soporte para `useTransition` y optimizaciones de renderizado. |
| **Vite** | 5.2+ | Empaquetado de alta velocidad y soporte PWA. |
| **Tailwind CSS** | 3.4+ | Estilización atómica y tokens de Glassmorphism. |
| **Lucide React** | 0.350+ | Set de iconografía consistente. |
| **XLSX (SheetJS)** | 0.19+ | Generación nativa de reportes Excel en el cliente. |

---

## 2. Sistema de Diseño: Apple Aesthetic

La interfaz utiliza una interpretación moderna de los principios de Apple, priorizando la claridad y la responsividad física.

### Características Visuales:
*   **Glassmorphism**: Capas de transparencia con `backdrop-filter` para mantener el contexto del dashboard.
*   **Micro-animaciones**: Transiciones de entrada tipo "slide-in" y efectos de escala en tarjetas de vehículos.
*   **Respuesta Háptica Visual**: Feedback inmediato en botones de salida y llegada para confirmar la acción del usuario.

---

## 3. Módulos de Misión Crítica

### 3.1. Control Operativo (`ControlOperativo.jsx`)
Gestiona el flujo central de logística. Implementa una búsqueda híbrida (Local + Central) y sugiere automáticamente datos de la agenda.

### 3.2. Centro de Reportes (`Reports.jsx`)
Módulo avanzado que permite auditar el historial de prácticas por fecha e instructor.
*   **Normalización**: Une los campos de SIGAFI con los registros locales para mostrar la "Versión Final" de la práctica.
*   **Exportación**: Genera archivos `.xlsx` con metadatos específicos para la administración del ISTPET.

### 3.3. Historial Rápido (`History.jsx` / `Home.jsx`)
Visualización de las últimas 10 prácticas completadas para una verificación rápida de retornos sin entrar al módulo de reportes.

---

## 4. Utilidades y Normalización (`agendaUi.js`)

Debido a que el backend retorna una mezcla de PascalCase (Legacy) y camelCase (Modern), el frontend utiliza una capa de normalización en `utils/agendaUi.js`.
*   **`normalizeAgendaRow`**: Asegura que el componente `ActiveClasses` reciba datos consistentes independientemente de si provienen del espejo local o de SIGAFI.
*   **Cálculo de Duración**: Funciones puras para determinar el tiempo en pista basado en `hora_salida` y `llegada`.

---

## 5. Capacidades PWA (Progressive Web App)

El sistema está configurado para comportarse como una aplicación nativa:
- **Offline Ready**: Cacheo de activos estáticos para carga instantánea.
- **Installable**: Puede añadirse a la pantalla de inicio en Windows, Android e iOS.
- **Manifest**: Iconos institucionales y colores de marca definidos en `manifest.json`.

---

## 6. Pipeline de Producción

```bash
# Desarrollo
npm run dev

# Construcción de Producción
npm run build

# Previsualización del Bundle Final
npm run preview
```
