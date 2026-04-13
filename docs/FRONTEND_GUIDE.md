# Guía de Ingeniería Frontend: React 19 (Industrial Edition)

Este documento detalla la arquitectura, el sistema de diseño y los patrones de desarrollo utilizados en la interfaz de usuario de ISTPET Vehículos.

---

## 1. Stack Tecnológico de Vanguardia

| Tecnología | Versión | Rol Crítico |
| :--- | :--- | :--- |
| **React** | 19.x | Motor de UI con soporte para Concurrent Rendering. |
| **Vite** | 8.x | Herramienta de construcción ultrarrápida (HMR instantáneo). |
| **Tailwind CSS** | 3.4+ | Sistema de diseño atómico con tokens personalizados. |
| **Axios** | 1.1x | Cliente de red con interceptores para inyección de **JWT**. |
| **Framer Motion** | (Opcional) | Orquestación de micro-animaciones en `VehicleCard`. |

---

## 2. Sistema de Diseño: Apple Aesthetic (Glassmorphism)

La interfaz se basa en los principios de diseño de Apple (iOS/macOS), optimizados para la visibilidad en garitas con luz solar directa y entornos nocturnos.

### Tokens de Diseño (`index.css`):
*   **Backdrop Blur**: Uso de `backdrop-filter: blur(20px)` para profundidad visual.
*   **Color Palette**: Harmonía de `istpet-navy` (#1a2544) y `apple-primary` (#007AFF).
*   **Typography**: Familia de fuentes sin-serif de alta legibilidad (inter-ui/system-ui).

---

## 3. Arquitectura del Control Hub (`ControlOperativo.jsx`)

Este es el núcleo de despacho (600+ líneas de lógica reactiva).

### Patrones Implementados:
1.  **JIT (Just-In-Time) Refresh**: Al detectar que un instructor recién sincronizado no está en la lista local, el componente dispara un "Silent Refresh" para actualizar el catálogo de docentes sin interrumpir la experiencia del usuario.
2.  **Debouncing de Búsqueda**: Implementa un retardo de 300ms para el autocompletado y 800ms para la búsqueda profunda por cédula (10 dígitos).
3.  **Reloj de Paridad Local**: Reloj de alta precisión sincronizado con el servidor para el cálculo de tiempos de práctica proyectados.
4.  **Matriz de Compatibilidad**: Lógica de filtrado que deshabilita vehículos basándose en la jerarquía de licencias (C < D < E).

---

## 4. Orquestación de Servicios (Service Layer)

Los servicios en `frontend/src/services/` actúan como una capa de abstracción sobre la API REST.

### `api.js` (La Puerta de Enlace):
Implementa interceptores automáticos:
*   Si existe un token en `localStorage`, se inyecta en el header `Authorization: Bearer ...`.
*   Maneja globalmente estados 401 (Unauthorized) redirigiendo al usuario al login.

---

## 5. Componentes de Misión Crítica

*   **`ActiveClasses.jsx`**: Implementa el "Mission Control Grid", visualizando las unidades en pista con indicadores de tiempo transcurrido.
*   **`VehicleCard.jsx`**: Tarjeta reactiva que cambia de estado (Seleccionada, Bloqueada, Mantenimiento) con transiciones suaves de color.
*   **`ThemeContext.jsx`**: Motor de tematización que persiste la preferencia de Modo Oscuro en el navegador del guardia.

---

## 6. Pipeline de Construcción y Calidad

```bash
npm run dev      # Entorno de desarrollo con HMR
npm run build    # Genera bundle optimizado para despliegue en Vercel
npm run lint     # Garantiza que el código cumple con el estándar de Proyectos ISTPET
```
