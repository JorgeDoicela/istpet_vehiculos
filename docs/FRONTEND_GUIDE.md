# Guía de Ingeniería Frontend: React 19 — Zenith Edition

Este documento aborda los patrones arquitectónicos bajo los cuales el frontend ha sido consolidado. La premisa principal es evitar la saturación del Main Thread, favoreciendo micro-transacciones asíncronas y retroalimentaciones efímeras.

---

## 1. Stack Tecnológico Industrial

| Dominios de Renderizado | Tecnología / Librería | Naturaleza de Implementación |
| :--- | :--- | :--- |
| **Núcleo de Ejecución** | **React 19.0** | Implementamos concurrencia para evitar bloqueos del UI (`useTransition`). |
| **Gestión de Paquetes** | **Vite 5.x** | Hot-Module Replacement en < 100ms. Minimización automática de CSS/JS. |
| **Estilización** | **Tailwind CSS 3.4+** | Variables atómicas con un fuerte énfasis en la aceleración por hardware (Transformaciones). |
| **Reportes Excel** | **SheetJS (XLSX)** | Generación binaria nativa directa en el navegador, descargando carga de la API. |

---

## 2. Paradigma Apple "Glassmorphism"

En el diseño para logística o monitoreo (Garitas, Supervisión), los fondos puros causan fatiga visual nocturna. ISTPET implementa el modelo Glassmorphism con un enfoque performante:

- **Efecto de Capas (`backdrop-blur-md`)**: Solo se aplica en paneles de alto nivel, permitiendo visualizar contextualmente movimientos tras las ventanas logísticas sin opacidad del 100%.
- **Prevención Térmica**: Evita el pintado constante en el DOM (re-paints) mediante el acelerado hardware sobre elementos translúcidos de fondo, delegando animaciones exclusivamente a variables `transform` y `opacity`.

---

## 3. Topología de Componentes Core

### El Motor de Logística (`ControlOperativo.jsx`)
No es simplemente un formulario. Este componente es un agrupador del `Business Logic` para gestionar salidas vehiculares.
- **Búsqueda Debounced Híbrida**: Retrasa en 300ms la petición a SIGAFI hasta que el ingreso del dígito esté finalizado.
- **Auto-Enriquecimiento**: Utiliza el utilitario `agendaUi.js` para parsear el modelo PascalCase de DB central y renderizarlo en camelCase local.

### El Exportador Masivo (`Reports.jsx`)
Mapeo asíncrono de más de cientos de transacciones históricas. 
- Implementa una tabla virtualizada de previsualización que delega el renderizado pesado hacia **XLSX**, comprimiendo los vectores para generar la descarga nativa en el dispositivo del usuario.

---

## 4. Progressive Web App (PWA) e Instalabilidad

Configuradas mediante el manifiesto, las interfaces están diseñadas para prescindir de la barra de direcciones del navegador en el dispositivo final:
- Cache Activo de `JS/CSS`, impidiendo fallas catastróficas del cliente si la red parpadea temporalmente. 
- Se provee una experiencia similar a native app al abrir directo en iPads y dispositivos en garita. 

---

## 5. Deployment Scripts de Front-End

Para liberar un nuevo reléase visual del cliente:
```bash
npm install     # Asegurar paridad de árbol de dependencias
npm run build   # Compilación AOT para Vercel o NGINX
npm run preview # Test loopback en dist/
```
