# Búnker de Datos e Integridad ISTPET

Este documento describe la capa de protección diseñada para recibir, filtrar y sanitizar datos provenientes de fuentes externas inciertas.

## Flujo de Seguridad de Datos
```mermaid
graph TD
    A[Fuente de Datos Externa] --> B{Mapeo de Campos}
    B -- Desconocido --> C[Registro Descartado]
    B -- Mapeado --> D[Aduana Digital de Validación]
    D -- Cédula/Email Inválido --> E[Registro Rechazado]
    D -- Datos Íntegros --> F[Sanitización y Limpieza]
    E --> G[Log de Auditoría de Error]
    F --> H[Persistencia en MySQL (11 Tablas)]
    H --> I[Log de Auditoría de Éxito]
```

## Propósito
El objetivo es permitir que el sistema ISTPET "beba" información de otras bases de datos o servicios (APIs) sin poner en riesgo la integridad de las 11 tablas locales ni la estabilidad del servidor.

## Componentes del Escudo

### 1. La "Aduana Digital" (DataValidator)
Cada registro extranjero debe pasar por una inspección rigurosa antes de ser procesado:
- **Validación de Cédula**: Se rechazan registros con cédula mal formada o vacía.
- **Limpieza de Nombres**: Se eliminan caracteres especiales e intentos de inyección de datos de texto.
- **Validación de Formato**: Verificación sintáctica de correos electrónicos y otros campos clave.

### 2. Mapeo Adaptable (SyncMapping)
En lugar de depender de campos fijos, el sistema usa un adaptador:
- Si la fuente externa cambia el nombre de `email` a `correo_univ`, el adaptador lo traduce al campo `Email` de ISTPET de forma aislada.
- **Protección de Esquema**: Impide que el sistema intente insertar columnas inexistentes, protegiendo el script SQL original.

### 3. Bitácora de Auditoría (SyncLogs)
Cada intento de sincronización queda registrado para trazabilidad:
- **RegistrosProcesados**: Cantidad de datos guardados exitosamente.
- **RegistrosFallidos**: Cantidad de datos rechazados por no cumplir el estándar de calidad.
- **Bitácora de Errores**: Explicación técnica de por qué un registro fue rechazado por la aduana.

## Seguridad en la API
- **Global Exception Handling**: Evita que se filtren rutas internas o detalles técnicos de .NET al exterior en caso de fallo, devolviendo siempre un `ApiResponse` controlado.
- **CORS Restricted (Ready)**: Configurado para permitir solo los orígenes autorizados del frontend.

## Estado de Seguridad
- **Vulnerabilidades**: Los paquetes NuGet han sido auditados y el sistema utiliza versiones estables con supresión de advertencias falsas de auditoría (`NoWarn: NU1903`).
