# Guía Frontend — React + Vite + Tailwind CSS

## Stack Tecnológico

| Tecnología | Versión | Uso |
| :--- | :--- | :--- |
| React | 19.2 | Framework de UI basado en componentes |
| Vite | 8.0 | Bundler y servidor de desarrollo |
| Tailwind CSS | 3.4 | Utilidades CSS (configuración personalizada) |
| React Router DOM | 7 | Navegación SPA |
| Axios | 1.14 | Cliente HTTP para la API REST |

---

## Estructura del Proyecto

```
frontend/src/
├── App.jsx                  # Raíz de la app y definición de rutas
├── main.jsx                 # Punto de entrada de React (createRoot)
├── index.css                # Variables CSS globales (design tokens)
├── App.css                  # Animaciones y estilos globales (apple-*)
│
├── pages/
│   ├── ControlOperativo.jsx # Página principal (Salida y Llegada de vehículos)
│   ├── Home.jsx             # Dashboard de monitoreo
│   ├── Students.jsx         # Catálogo de estudiantes
│   └── Vehicles.jsx         # Catálogo de vehículos
│
├── components/
│   ├── layout/
│   │   ├── Layout.jsx       # Contenedor principal con Sidebar
│   │   └── Sidebar.jsx      # Navegación lateral con links y modo oscuro
│   ├── common/
│   │   ├── StatusBadge.jsx  # Badge de estado reutilizable ("En Pista", etc.)
│   │   └── ThemeContext.jsx  # Context API para modo claro/oscuro
│   ├── features/
│   │   ├── ActiveClasses.jsx  # Tarjetas de vehículos en pista
│   │   ├── VehicleList.jsx    # Lista de vehículos del catálogo
│   │   ├── StudentSearch.jsx  # Búsqueda de estudiantes
│   │   └── SkeletonLoader.jsx # Placeholders de carga animados
│   └── logistica/
│       ├── VehicleCard.jsx    # Tarjeta de vehículo disponible (seleccionable)
│       └── LogisticaHeader.jsx # Encabezado del módulo con tabs Salida/Llegada
│
└── services/
    ├── api.js               # Instancia base de Axios (baseURL configurada aquí)
    ├── logisticaService.js  # Métodos para el módulo de Control Operativo
    ├── dashboardService.js  # Métodos para el módulo de monitoreo
    ├── studentService.js    # Métodos para el catálogo de estudiantes
    └── vehicleService.js    # Métodos para el catálogo de vehículos
```

---

## Rutas de la Aplicación

Definidas en `App.jsx` con React Router DOM v7:

| Ruta | Componente | Descripción |
| :--- | :--- | :--- |
| `/` | `ControlOperativo` | Panel de despacho. Ruta principal |
| `/monitoreo` | `Home` | Dashboard con clases activas y alertas |
| `/estudiantes` | `Students` | Catálogo de estudiantes |
| `/vehiculos` | `Vehicles` | Catálogo de flota |
| `/*` (cualquier otra) | `ControlOperativo` | Redirige al panel principal |

---

## Sistema de Diseño (Design Tokens)

El diseño utiliza variables CSS definidas en `index.css`. Están disponibles en toda la aplicación:

```css
:root {
  --apple-bg: #f5f5f7;          /* Fondo principal */
  --apple-card: #ffffff;         /* Fondo de tarjetas */
  --apple-border: #e2e8f0;       /* Color de bordes */
  --apple-primary: #007AFF;      /* Azul de acción principal */
  --apple-text-main: #1a2544;    /* Texto principal oscuro */
  --apple-text-sub: #8e8e93;     /* Texto secundario gris */
  --istpet-navy: #1a2544;        /* Azul corporativo ISTPET */
  --istpet-gold: #f0a500;        /* Dorado corporativo ISTPET */
}
```

---

## Módulo Principal: ControlOperativo.jsx

Es el componente más complejo del sistema (627 líneas). Gestiona dos pestañas: **Salida** y **Llegada**.

### Estado del Componente

```javascript
// --- Pestaña Salida ---
activeTab           // 'salida' | 'llegada'
salidaCedula        // Texto del campo de búsqueda
sugerencias         // Lista de autocompletado (máx. 5)
estudianteData      // Datos del estudiante localizado
vehiculos           // Lista de unidades disponibles
vehiculoSeleccionado
instructores
instructorSeleccionado

// --- Pestaña Llegada ---
clasesActivas       // Vehículos actualmente en pista
claseSeleccionada   // Vehículo a registrar retorno
horaRetorno         // Reloj en tiempo real (actualiza cada 1s)
agendadosHoy        // Lista de prácticas de SIGAFI para hoy
```

### Lógica de Autocompletado

Implementa un **debounce de 300ms** sobre el campo de cédula. Al escribir:
1. Filtra localmente la lista `agendadosHoy` (sin llamada al servidor).
2. Simultáneamente, consulta `GET /api/logistica/buscar?query=...` para alumnos históricos.
3. Combina ambas listas eliminando duplicados. Muestra máximo 5 sugerencias.

### Auto-búsqueda Completa

Cuando la cédula tiene exactamente **10 dígitos** y hay un **debounce de 800ms** sin cambios, dispara automáticamente `ejecutarBusquedaEstudiante()` — el mismo que activa el botón de búsqueda manual.

### Filtrado de Vehículos por Licencia

Al seleccionar un estudiante, el selector de vehículos solo muestra las unidades cuyo `idTipoLicencia` sea menor o igual al tipo de licencia del estudiante (permite que un estudiante tipo C conduzca vehículos tipo C; un tipo E puede conducir cualquiera).

---

## Capa de Servicios (Axios)

### `api.js` — Configuración Base

```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  headers: { 'Content-Type': 'application/json' }
});

export default api;
```

### `logisticaService.js` — Métodos

| Método | HTTP | Endpoint | Descripción |
| :--- | :--- | :--- | :--- |
| `buscarEstudiante(cedula)` | GET | `/logistica/estudiante/{cedula}` | Busca estudiante (local + SIGAFI) |
| `buscarSugerencias(query)` | GET | `/logistica/buscar?query=...` | Autocompletado (min 3 chars) |
| `getVehiculosDisponibles()` | GET | `/logistica/vehiculos-disponibles` | Flota disponible |
| `getInstructores()` | GET | `/logistica/instructores` | Lista de instructores activos |
| `registrarSalida(idMat, idVeh, idInst)` | POST | `/logistica/salida` | Registra salida |
| `registrarLlegada(idRegistro)` | POST | `/logistica/llegada` | Registra llegada |
| `getAgendadosHoy()` | GET | `/logistica/agendados-hoy` | Agenda SIGAFI del día |

---

## Comandos de Desarrollo

```bash
npm run dev      # Servidor de desarrollo (http://localhost:5173)
npm run build    # Compilar para producción (output: /dist)
npm run preview  # Previsualizar el build de producción
npm run lint     # Analizar el código con ESLint
```
