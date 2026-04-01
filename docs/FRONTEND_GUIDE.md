# Arquitectura del Frontend ISTPET (React + Vite)

Este documento detalla la estructura y patrones de diseño utilizados para crear una interfaz de usuario responsiva, moderna y dinámica.

## Pilares Tecnológicos y Estéticos (Apple 2026)
- **Framework**: React 18 con Vite.
- **Estándar Visual**: **Apple Light 2026** (Vitreous Design).
- **Glassmorphism**: Uso de `backdrop-blur-2xl` y `apple-glass` para paneles flotantes.
- **Mesh Gradients**: Fondo dinámico de "Malla Líquida" para profundidad visual.
- **Iconografía**: Heroicons (100% SVG) con trazo minimalista.

## Estándar de Comunicación (Axios Interceptors)
El archivo `services/api.js` centraliza la lógica de comunicación:
1.  **Unwrapping Automatico**: Desempaqueta el objeto `ApiResponse` para que los servicios solo reciban la data real.
2.  **Manejo de Errores Global**: Captura fallos de red o errores 500 y los transforma en promesas rechazadas con mensajes amigables.

## Componentes Dinámicos (Evolución Fácil)
El sistema está diseñado para el cambio:
- **`VehicleList.jsx`**: Utiliza el patrón **Dynamic Object Mapping**. Si el backend agrega un campo nuevo al DTO, el componente lo reconocerá y lo renderizará en la tarjeta del vehículo sin necesidad de tocar el código de React.

## Estructura de Carpetas
```text
src/
├── components/
│   ├── layout/       # Estructura base (Sidebar, Navbar)
│   └── features/     # Componentes de negocio (Busqueda, Listas)
├── services/         # Clientes API (VehicleService, StudentService)
└── pages/            # Vistas principales (Dashboard)
```

## Estilo Premium e Industrial
- **Color de Marca**: Slate-900 y Primary-600 (Azul institucional).
- **Tipografía**: Inter (Estándar de legibilidad).
- **Iconografía**: 100% SVG Vectorial para evitar distorsiones y carga de fuentes pesadas.
