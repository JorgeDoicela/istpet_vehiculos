# Scripts SQL — ISTPET Vehículos

## Resumen de alineación con el backend (.NET 8 / EF Core)

| Script | Uso | Alineación con el API |
| :--- | :--- | :--- |
| **`01_ISTPET_LOGISTICS_SCHEMA.sql`** | Esquema **canónico** de `istpet_vehiculos` (DROP + CREATE BD, tablas, FKs, vista `v_clases_activas`, seeds `tipo_licencia` + `admin`). | **Referencia principal.** Coincide con `AppDbContext` y tablas que consume el Master Sync. |
| **`02_SYNC_SIGAFI_DATA.sql`** | Carga manual por `INSERT … SELECT` desde `sigafi_es` → `istpet_vehiculos` (mismo servidor MySQL). | Columnas alineadas con **`01`**. No incluye `matriculas_examen_conduccion` (eso lo hace **`POST /api/Sync/master`**). Requiere que `sigafi_es` tenga las tablas/columnas esperadas. |
| **`Cloud/99_MASTER_DEPLOYMENT.sql`** | Demo unificada (p. ej. TiDB / nube): mock mínimo de `sigafi_es` + espejo local + primer `INSERT` cruzado. | **Subconjunto** de `01`: sirve para smoke tests. Para **paridad completa** con producción, tras crear BDs ejecutar **`01`** sobre `istpet_vehiculos` (o mantener este script solo como laboratorio). Ver cabecera del propio `99`. El flujo Render + Vercel + TiDB está descrito en **[DEPLOYMENT_CLOUD.md](../DEPLOYMENT_CLOUD.md)**. |

## Flujos recomendados

### Entorno real (SIGAFI en red, p. ej. `192.168.x.x`)

1. Ejecutar **`01_ISTPET_LOGISTICS_SCHEMA.sql`** (crea `istpet_vehiculos`).
2. Configurar `DefaultConnection` + `SigafiConnection` en `appsettings.json`.
3. Poblar el espejo con **`POST /api/Sync/master`** (no depender solo de `02` si quieres exámenes y todos los módulos del C#).

### Solo SQL sin API (mismo servidor, ambas BDs)

1. Esquema local: **`01`**.
2. Opcional: **`02`** para volcar datos desde `sigafi_es` (legado / respaldo manual).

### Nube / CI / demo sin SIGAFI real

1. Ejecutar **`Cloud/99_MASTER_DEPLOYMENT.sql`** (o: mock SIGAFI a mano + **`01`** para `istpet_vehiculos`).
2. Probar API; para esquema idéntico al de producción, preferir **`01`** en `istpet_vehiculos`.

## Detalles que suelen preguntarse

- **`periodos` / `secciones`**: no están en **`01`**; el backend las consulta en **SIGAFI** vía `SqlCentralStudentProvider`, no como tablas obligatorias en el espejo local.
- **`v_alerta_mantenimiento`**: no está en **`01`**; el dashboard usa SIGAFI y, si existe la vista en local, hace merge. Su ausencia no rompe el arranque.
- **`02` vs Master Sync**: `02` documenta el modelo de datos; la ingesta **operativa** y completa está en el **Master Sync** (ver `docs/SYNC_VERIFICATION.md`).
