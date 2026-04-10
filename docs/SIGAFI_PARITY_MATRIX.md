# Matriz de Paridad SIGAFI -> istpet_vehiculos

Objetivo: mantener un espejo casi total de SIGAFI en la base local, excluyendo fotos/BLOB y preservando campos operativos locales.

## Politica de paridad

- **Fuente de verdad**: `sigafi_es` (solo lectura desde la app).
- **Espejo local**: `istpet_vehiculos` (lectura/escritura para operacion y enriquecimiento local).
- **Fotos**: excluidas del espejo (`alumnos.foto` no se replica).
- **Passwords**: se conservan en espejo (`alumnos.password`, `usuarios_web.password`).
- **Master Sync**: mecanismo operativo principal de paridad.

## Clasificacion por tabla

| Tabla | Tipo | Nota de paridad |
| :--- | :--- | :--- |
| `alumnos` | mirror | Se replica set completo excepto BLOB `foto`. |
| `profesores` | mirror | Replica completa de columnas remotas usadas en SIGAFI. |
| `vehiculos` | mirror + local | Mirror remoto + extensiones locales (`id_tipo_licencia`, `id_instructor_fijo`, `estado_mecanico`). |
| `periodos` | mirror | Replica completa. |
| `carreras` | mirror | Replica completa. |
| `cursos` | mirror | Replica completa incluyendo `activo`. |
| `secciones` | mirror | Replica completa. |
| `modalidades` | mirror | Replica completa. |
| `instituciones` | mirror | Replica completa. |
| `categoria_vehiculos` | mirror | Replica completa. |
| `categorias_examenes_conduccion` | mirror | Replica completa. |
| `matriculas` | mirror + local | Replica remota completa + locales (`horas_completadas`, `estado`). |
| `cond_alumnos_practicas` | mirror + local | Mirror remoto; `observaciones` es campo local opcional y no se pisa con `NULL` remoto. |
| `cond_alumnos_vehiculos` | mirror | Replica completa. |
| `asignacion_instructores_vehiculos` | mirror | Replica completa. |
| `cond_alumnos_horarios` | mirror | Replica completa, incluyendo activos e inactivos. |
| `cond_practicas_horarios_alumnos` | mirror | Replica completa de enlaces existentes en origen. |
| `matriculas_examen_conduccion` | mirror | Replica completa. |
| `usuarios_web` | mirror | Replica completa incluyendo `esRrhh`; `creado_en` se conserva en local. |
| `tipo_licencia` | derivada local | Catalogo local derivado desde `categoria_vehiculos` mientras SIGAFI no publique tabla propia. |

## Reglas para evitar perdida de datos locales

- No sobrescribir campos `local-only` en upserts de sync.
- Cuando SIGAFI no entrega una columna (ej. `cond_alumnos_practicas.observaciones`), no reemplazar valor local existente con `NULL`.
- Filtros funcionales (activos/validos) deben vivir en consumo de negocio, no en el espejo.
