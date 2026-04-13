# Guía Maestra del Usuario: Control Logístico Vehicular

Bienvenido al sistema de gestión logística industrial del ISTPET. Este manual detalla la operación de los tres núcleos operativos del sistema: **Control Hub**, **Mission Control** y el **Puente de Sincronización**.

---

## 1. El Control Hub (Registro de Salida)

Es la interfaz primaria para el despacho de unidades. El sistema utiliza **Inteligencia Predictiva** basada en la agenda de SIGAFI.

### Flujo de Despacho Profesional:
1.  **Identificación JIT (Just-In-Time)**: Ingrese los últimos 3-4 dígitos de la cédula del estudiante. El sistema buscará en el espejo local; si no existe, activará automáticamente el **Puente Híbrido** hacia SIGAFI.
2.  **Sugerencia de Agenda**: Si el estudiante tiene una práctica programada o un tutor asignado, el Hub pre-seleccionará el vehículo e instructor idóneos, minimizando el error humano.
3.  **Filtrado por Categoría**: Los vehículos se bloquean visualmente si no coinciden con la categoría de licencia del estudiante (C, D, E).
4.  **Confirmación de Salida**: Al presionar "Confirmar Salida", el sistema valida en nanosegundos el "Triángulo de Disponibilidad" (Estudiante, Vehículo, Instructor).

---

## 2. Mission Control (Registro de Llegada y Dashboard)

Visible en la pestaña "Llegada" y en el módulo de "Mantenimiento". Permite el monitoreo táctico de la flota en pista.

### Operación de Retorno:
*   **Grid de Tiempo Real**: Muestra tarjetas con efecto *Glassmorphism* de cada unidad fuera de garita.
*   **Reloj de Paridad**: Indica el tiempo transcurrido desde la salida.
*   **Cierre de Transacción**: Al confirmar la llegada, el sistema calcula automáticamente las horas de práctica y las asocia al expediente operativo del alumno.

---

## 3. Gestión de la Flota y Catálogos

### Gestión de Unidades (Vehículos)
*   **Estado Mecánico**: Un vehículo marcado en "Mantenimiento" en este módulo aparecerá bloqueado para despacho en el Control Hub.
*   **Mapeo de Instructor**: Permite asignar un docente fijo a una unidad física.

### Expedientes Estudiantiles
*   **Historial de Paridad**: Permite verificar si los datos locales coinciden con SIGAFI. Si hay una discrepancia, el administrador puede forzar una **Inspección de Paridad**.

---

## 4. El Dashboard de Monitoreo

Ubicado en el menú lateral, ofrece una visión analítica:
*   **Indicador de Carga**: Porcentaje de la flota actualmente en uso.
*   **Panel de Alertas**: Notificaciones críticas sobre vehículos que han excedido el tiempo promedio de práctica o unidades con fallos reportados.

---

## 5. Resiliencia en Caso de Desconexión

> [!IMPORTANT]
> El sistema está diseñado para el **Modo de Operación Local Aislada**.

Si la conexión con el servidor central SIGAFI se interrumpe (indicado por un aviso en la barra de búsqueda):
1.  Usted podrá seguir registrando salidas y llegadas de alumnos que ya hayan sido sincronizados previamente.
2.  La búsqueda de nuevos alumnos desde SIGAFI se reactivará automáticamente cuando se reestablezca el enlace.
3.  **No es necesario reiniciar el sistema**; el *Resilience Pipeline* detectará la reconexión.

---

## 6. Iconografía y Atajos Rápidos
*   **Luna/Sol**: Cambio de ambiente visual (Modo Oscuro recomendado para turnos nocturnos).
*   **Badge de Categoría**: Indica el tipo de licencia (Verde para C, Amarillo para D, Rojo para E).
*   **Log de Auditoría**: Visible solo para administradores para rastrear quién autorizó cada salida.
