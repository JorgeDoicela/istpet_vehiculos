# Seguridad y Protección de Datos — ISTPET

## Modelo de Seguridad

El sistema implementa tres capas de protección independientes:

```
[Capa 1: Autenticación Híbrida]  →  [Capa 2: Middleware de Errores]  →  [Capa 3: Data Shield]
     AuthController                   ErrorHandlingMiddleware              DataValidator + DataSyncService
```

---

## Capa 1: Autenticación — Puente Híbrido de Seguridad

El sistema debe coexistir con usuarios provenientes del sistema SIGAFI (que usan contraseñas hasheadas con **BCrypt**) y usuarios nativos nuevos (que usan **SHA-256**). El `AuthController` detecta automáticamente el algoritmo correcto.

```mermaid
flowchart TD
    A["POST /api/auth/login"] --> B{¿Usuario existe y activo?}
    B -- No --> C["401: Usuario no encontrado"]
    B -- Sí --> D{¿Hash empieza con '$2a$' o '$2b$'?}
    D -- Sí --> E["Verificar con BCrypt.Verify()"]
    D -- No --> F["Calcular SHA-256 del password\ny comparar"]
    E --> G{¿Válido?}
    F --> G
    G -- No --> H["401: Contraseña incorrecta"]
    G -- Sí --> I["200 OK: {Token, usuario, nombre, rol}"]
```

### Hash SHA-256 (Usuarios Nativos)
```sql
-- Así se crea el hash en el SQL inicial:
INSERT INTO usuarios_web (usuario, password, ...)
VALUES ('admin_istpet', 'istpet2026', ...);
```

### Hash BCrypt (Usuarios Migrados de SIGAFI)
Los usuarios con contraseñas almacenadas como BCrypt en SIGAFI pueden autenticarse directamente sin necesidad de restablecer su contraseña. El sistema detecta el hash por su prefijo `$2a$` o `$2b$`.

### Autenticación con JWT
El sistema utiliza **JSON Web Tokens (JWT)** para proteger los endpoints. El login retorna un token `Bearer` que debe ser incluido en el header `Authorization` de las peticiones subsiguientes.

---

## Capa 2: Middleware Global de Errores

`ErrorHandlingMiddleware.cs` actúa como el último recurso ante cualquier excepción no controlada. Garantiza que el cliente nunca reciba:
- Mensajes de error de .NET crudos (stack traces)
- Información del servidor o de la base de datos

**Toda excepción no capturada resulta en:**
```json
{
  "success": false,
  "message": "Error interno del servidor. Consulte soporte técnico.",
  "data": null
}
```

El error real se registra en los logs del servidor (`ILogger`) sin exponerlo al cliente.

---

## Capa 3: Data Shield — Sanitización de Datos Externos

Implementado en `DataValidator.cs` y `DataSyncService.cs`. Protege la base de datos ante la ingesta de datos externos de calidad variable (provenientes de sistemas terceros vía `POST /api/sync/students`).

### Reglas de Validación

| Regla | Implementación | Acción si falla |
| :--- | :--- | :--- |
| **Cédula válida** | `Regex: ^\d{10,15}$` | Registro rechazado, `registros_fallidos++` |
| **Email válido** | `Regex: ^[^@\s]+@[^@\s]+\.[^@\s]+$` | Email guardado como `NULL` (campo opcional) |
| **Nombre limpio** | `Regex.Replace(name, @"[^a-zA-Z\s]", "")` | Caracteres especiales eliminados antes de persistir |

### Flujo de Ingesta Protegida

```mermaid
flowchart LR
    A[JSON Externo] --> B{Validar Cédula}
    B -- Inválida --> C[Registrar fallo\nContinuar siguiente]
    B -- Válida --> D[Limpiar nombre\nValidar email]
    D --> E{¿Ya existe en DB?}
    E -- Sí --> F[Omitir - No duplicar]
    E -- No --> G[INSERT en #alumnos]
    G --> H[registros_procesados++]
    C --> I[Guardar SyncLog]
    F --> I
    H --> I
    I --> J["Retornar SyncLog\n{procesados, fallidos, estado}"]
```

---

## Protección de la BD Central SIGAFI

El acceso a `sigafi_es` es estrictamente de **solo lectura**. El usuario de MySQL configurado solo tiene permisos `SELECT` sobre esa base de datos. Nunca se realizan `INSERT`, `UPDATE` o `DELETE` sobre SIGAFI desde este sistema.

---

## Configuración CORS

En el entorno actual (desarrollo), CORS permite cualquier origen:

```csharp
policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
```

> **Para producción:** Restringir a los dominios específicos del frontend.

```csharp
// Ejemplo para producción:
policy.WithOrigins("https://istpet.edu.ec")
      .AllowAnyMethod()
      .AllowAnyHeader();
```

---

## Consideraciones de Producción

| Aspecto | Estado Actual | Recomendación |
| :--- | :--- | :--- |
| Autenticación | JWT Bearer Tokens | Configurar rotación de llaves |
| Autorización por rol | Implementada en backend | Auditoría periódica de roles |
| CORS | `AllowAll` (desarrollo) | Restringir a dominio de producción |
| HTTPS | Opcional | Habilitar `app.UseHttpsRedirection()` |
| Contraseña en config | `appsettings.json` o ENV | Usar Azure Key Vault para producción |
