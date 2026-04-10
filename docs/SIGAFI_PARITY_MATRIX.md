# Matriz de Paridad SIGAFI -> istpet_vehiculos

Objetivo: mantener un espejo estricto de SIGAFI en la base local para las tablas espejo, excluyendo solo fotos/BLOB.

## Politica de paridad

- **Fuente de verdad**: `sigafi_es` (solo lectura desde la app).
- **Espejo local**: `istpet_vehiculos` replica las tablas espejo con el mismo set de columnas que SIGAFI, salvo `foto`.
- **Fotos**: excluidas del espejo (`alumnos.foto` no se replica).
- **Passwords**: se conservan en espejo (`alumnos.password`, `usuarios_web.password`).
- **Master Sync**: mecanismo operativo principal de paridad.

## Clasificacion por tabla

| Tabla | Tipo | Nota de paridad |
| :--- | :--- | :--- |
| `alumnos` | mirror | Se replica set completo excepto BLOB `foto`. |
| `profesores` | mirror | Replica completa de columnas remotas usadas en SIGAFI. |
| `vehiculos` | mirror | Replica las columnas remotas; el estado operativo queda en `vehiculos_operacion`. |
| `periodos` | mirror | Replica completa. |
| `carreras` | mirror | Replica completa. |
| `cursos` | mirror | Replica exacta segun SIGAFI. |
| `secciones` | mirror | Replica completa. |
| `modalidades` | mirror | Replica completa. |
| `instituciones` | mirror | Replica completa. |
| `categoria_vehiculos` | mirror | Replica completa. |
| `categorias_examenes_conduccion` | mirror | Replica completa. |
| `matriculas` | mirror | Replica remota completa; estado operativo queda en `matriculas_operacion`. |
| `cond_alumnos_practicas` | mirror | Replica remota exacta; observaciones locales quedan en `practicas_operacion`. |
| `cond_alumnos_vehiculos` | mirror | Replica completa. |
| `asignacion_instructores_vehiculos` | mirror | Replica completa. |
| `cond_alumnos_horarios` | mirror | Replica completa, incluyendo activos e inactivos. |
| `cond_practicas_horarios_alumnos` | mirror | Replica completa de enlaces existentes en origen. |
| `matriculas_examen_conduccion` | mirror | Replica completa. |
| `usuarios_web` | mirror | Replica exacta incluyendo `esRrhh`. |
| `tipo_licencia` | derivada local | Catalogo local derivado desde `categoria_vehiculos` mientras SIGAFI no publique tabla propia. |

## Reglas para evitar perdida de datos locales

- No agregar columnas operativas dentro de tablas espejo.
- Los datos operativos locales viven en tablas auxiliares: `vehiculos_operacion`, `matriculas_operacion`, `practicas_operacion`.
- Filtros funcionales (activos/validos) deben vivir en consumo de negocio, no en el espejo.
