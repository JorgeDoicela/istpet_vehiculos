# Manual de Usuario — ISTPET Sistema de Logística

## Acceso al Sistema

Abrir el navegador web y acceder a la URL proporcionada por el administrador (en local: `http://localhost:5173`).

---

## Módulo 1: Control Operativo (Pantalla Principal)

Esta es la pantalla de trabajo diaria del guardia de garita. Permite registrar la salida y el retorno de vehículos con estudiantes.

### Pestaña "Salida" — Despachar un Vehículo

**Flujo paso a paso:**

#### Paso 1: Buscar al Estudiante

En el campo **"Cédula del Estudiante"**, ingresar el número de cédula.

- El sistema busca automáticamente al teclear (autocompletado desde 3 caracteres).
- Al completar los 10 dígitos, la búsqueda se ejecuta sola.
- Si el estudiante aparece en el listado de sugerencias, hacer clic sobre su nombre.

**Resultados posibles:**

| Situación | Lo que muestra el sistema |
| :--- | :--- |
| Estudiante encontrado localmente | Tarjeta con nombre, curso, licencia, jornada y paralelo |
| Estudiante encontrado en SIGAFI | Tarjeta con los datos + foto (si disponible) |
| Estudiante no encontrado en ningún lugar | Mensaje de error: "No localizado" |

**Sugerencia de Agenda e Inteligencia de Despacho:** Si el estudiante tiene una práctica agendada hoy o un tutor asignado en SIGAFI, el sistema **seleccionará automáticamente** al vehículo y al instructor.

#### Paso 2: Seleccionar el Vehículo

Si el sistema no detectó una sugerencia automática, se muestra la flota de vehículos disponibles:

- Las tarjetas se pueden filtrar escribiendo la placa o número de unidad en el buscador.
- Los indicadores **C / D / E** muestran la categoría de licencia del estudiante. Solo aparecen los vehículos compatibles.
- Haga clic en una tarjeta para seleccionarla manualmente si es necesario.

#### Paso 3: Seleccionar el Instructor

En el menú desplegable **"Instructor Responsable"**, el sistema pre-seleccionará al docente asignado. Puede cambiarlo manualmente si el docente se encuentra ausente.

#### Paso 4: Confirmar la Salida

El reloj muestra la hora actual automáticamente. Cuando los tres datos están completos (estudiante, vehículo, instructor), el botón **"Confirmar Salida"** se activa.

Al hacer clic:
- Si todo es correcto: Notificación verde "¡Vehículo en pista registrado!" y la pantalla se reinicia.
- Si hay un problema: Notificación roja con el motivo (ej: "VEHICULO_EN_USO", "INSTRUCTOR_OCUPADO").

---

### Pestaña "Llegada" — Registrar el Retorno

Hacer clic en la pestaña **"Llegada"** (o acceder directamente con `/?tab=llegada`).

Se muestra una cuadrícula con todos los vehículos que están actualmente en pista. Cada tarjeta muestra:
- Número de unidad
- Nombre del instructor
- Nombre del estudiante
- Hora de salida

**Para registrar un retorno:**

1. Hacer clic en la tarjeta del vehículo que regresó (se selecciona con borde azul).
2. Verificar que la hora de **"Retorno Proyectado"** (reloj en tiempo real) es correcta.
3. Hacer clic en **"Confirmar Retorno"** (botón dorado).
4. Notificación verde "¡Llegada confirmada!" — las horas de práctica del estudiante se actualizan automáticamente.

---

## Módulo 2: Monitoreo (Dashboard)

Accesible desde el menú lateral en **"Monitoreo"**.

Muestra:
- **Clases Activas:** Los mismos vehículos en pista que la pestaña Llegada, en formato de tarjetas informativas.
- **Alertas de Mantenimiento:** Vehículos con estado "MANTENIMIENTO" que requieren atención.

Este módulo es de solo lectura — no permite realizar acciones.

---

## Módulo 3: Estudiantes

Accesible desde el menú lateral. Muestra el catálogo completo de estudiantes registrados en la base de datos local. Permite buscar por nombre o cédula.

---

## Módulo 4: Vehículos

Accesible desde el menú lateral. Muestra el catálogo completo de vehículos con su estado mecánico actual.

---

## Panel Lateral: Agenda SIGAFI Hoy

Visible siempre en la pestaña de Salida. Muestra las prácticas programadas para el día actual según el sistema académico central.

- Al pasar el cursor sobre una tarjeta, aparece el botón **"CARGAR CÉDULA"** que copia automáticamente la cédula del estudiante al campo de búsqueda.

---

## Modo Oscuro

El ícono de luna/sol en la barra lateral permite alternar entre modo claro y modo oscuro. La preferencia se mantiene mientras la sesión está activa.

---

## Consideraciones Operativas

- **Un vehículo solo puede salir una vez:** El sistema rechaza el despacho de un vehículo que ya está en pista.
- **Un instructor no puede estar en dos prácticas simultáneas:** El sistema lo valida.
- **Las horas de práctica se acumulan automáticamente:** Cada vez que se registra una llegada, el sistema calcula el tiempo transcurrido y lo suma al contador de horas del estudiante.
- **Si SIGAFI no está disponible:** El sistema de logística local sigue funcionando con normalidad. Solo la agenda y la búsqueda desde central estarán inactivas.
