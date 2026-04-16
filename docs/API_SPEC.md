# Especificación de la API REST — ISTPET Zenith Edición 2026

Este documento define el contrato de integración formal para el Sistema de Control Operativo Vehicular. Todos los endpoints operan bajo la convención RESTful y requieren autenticación `Bearer Token (JWT)`, asegurando transporte vía TLS/HTTPS en producción.

---

## 1. Patrón Global de Respuesta Envelope

El backend estandariza las respuestas utilizando el envoltorio `ApiResponse<T>`.

**Estructura Base (`200 OK` / `400 Bad Request`):**
```json
{
  "success": true,               // Boolean indicando estado
  "message": "Operación exitosa", // Mensaje Human-Readable
  "data": { ... },               // Payload genérico dinámico
  "timestamp": "2026-04-16T14:40:00Z"
}
```

---

## 2. Core Logístico (Garita de Control)

Gestiona la asignación dinámica de flujo vehicular mediante un puente JIT (Just-In-Time) con SIGAFI.

### `GET /api/logistica/estudiante/{cedula}`
Extrae el perfil completo del estudiante resolviendo su agenda y cruzando datos locales.

**Respuesta de Éxito (`200 OK`):**
```json
{
  "success": true,
  "data": {
    "idAlumno": "1712345678",
    "nombreCompleto": "TRAVERSARI GOMEZ PEDRO LUIS",
    "carrera": "CONDUCCIÓN PROFESIONAL TIPO C",
    "nivel": "PRIMERO",
    "esAgendado": true,
    "horaAgenda": "14:00",
    "isBusy": false
  }
}
```

### `POST /api/logistica/salida`
Ejecuta la transacción operativa de salida en pista. Falla rápidamente de presentarse asimetría de condiciones.

**Payload (`application/json`):**
```json
{
  "idMatricula": 18274,
  "idVehiculo": 15,
  "idInstructor": "1787654321",
  "registradoPor": "guardia.noche"
}
```

### `POST /api/logistica/llegada`
Revierte el estado del vehículo en base de datos y computa estadísticos de demora para consolidación de reportes.

**Payload (`application/json`):**
```json
{
  "idPractica": 54200,
  "registradoPor": "guardia.noche"
}
```

---

## 3. Data Intelligence y Reportes (Admin Only)

Módulos restringidos al rol `admin` para consolidación tabular y auditoría.

### `GET /api/reports/practicas`
Genera el consolidado JIT cruzando el histórico SIGAFI con las trazas de tiempo recientes locales.

**Query Parameters:**
| Parámetro | Tipo | Descripción |
| :--- | :--- | :--- |
| `fechaInicio` | YYYY-MM-DD | Límite inferior de la ventana de búsqueda. |
| `fechaFin` | YYYY-MM-DD | Límite superior. |
| `instructorId` | String | Cédula exacta para filtrado atómico. |

---

## 4. Códigos de Error de Negocio

Cuando la API retorna `success: false`, el `message` podrá contener lógicas estructuradas para que el front-end actúe correspondientemente:

| Código de Negocio | Condición Desencadenante | Resolución Esperada |
| :--- | :--- | :--- |
| `VEHICULO_EN_USO` | El coche seleccionado tiene `ensalida=1` en otra tabla pivotante. | Sugerir vehículo alternativo. |
| `ESTUDIANTE_EN_PISTA`| El alumno tiene un registro de práctica abierto sin completarse. | Inspeccionar Garita de Retorno. |
| `INVALID_MATRICULA` | Falló validación contra tabla central de pagos SIGAFI. | Redirigir a secretaría. |
| `CIRCUIT_OPEN` | El enlace con la base de datos central en la nube ha colapsado. | Reintentar en 30 segundos; o usar Fallback Mode. |
