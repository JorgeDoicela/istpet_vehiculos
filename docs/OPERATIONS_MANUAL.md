# Manual de Operaciones y Alta Disponibilidad (HA)

Este manual está dirigido a administradores de sistemas y personal técnico encargado del mantenimiento de ISTPET Logística.

---

## 1. Gestión de Modos de Base de Datos

ISTPET Logística es un sistema híbrido que puede operar en dos modos principales definidos por la variable de entorno `DATABASE_MODE`.

### A. Modo Directo (Default / Producción)
- **Configuración**: `DATABASE_MODE=Direct`
- **Comportamiento**: 
  - Las consultas de estudiantes y catálogo se realizan en tiempo real contra SIGAFI Central.
  - La base de datos local `istpet_vehiculos` se usa solo para persistir estados operativos ("En Pista") y auditoría.
  - El motor de sincronización masiva entra en **Standby**.

### B. Modo Espejo (Standby HA)
- **Configuración**: `DATABASE_MODE=Mirror`
- **Comportamiento**:
  - El sistema utiliza la base de datos local como fuente primaria.
  - Útil cuando la conexión con SIGAFI es extremadamente lenta o inestable.
  - Requiere ejecuciones periódicas del **Master Sync**.

---

## 2. Procedimiento de Sincronización Manual

Si por alguna razón los datos locales han divergido (por ejemplo, un cambio masivo en SIGAFI que no se refleja), siga estos pasos:

1. Inicie sesión como **Administrador**.
2. Diríjase a la ruta `/configuracion` (o similar, dependiendo de la UI).
3. Localice el botón **"Sincronización Maestra (Full Sync)"**.
4. **IMPORTANTE**: Este proceso bloquea la tabla de alumnos por unos segundos. No se recomienda ejecutarlo durante horas pico de salida de vehículos.

---

## 3. Resolución de Problemas (Troubleshooting)

### El sistema dice "SIGAFI No Disponible"
1. Verifique el estado del Circuit Breaker en los logs del backend.
2. Si el circuito está `OPEN`, el sistema fallará rápido por protección.
3. Valide la conectividad VPN o de red hacia la IP de SIGAFI.

### Error: VEHICULO_EN_USO fantasma
Si un vehículo aparece como "En Pista" pero ya regresó físicamente:
1. Revise el panel de "Garita de Retorno".
2. Si no aparece allí, un administrador puede forzar el cierre del registro en la tabla `practicas` (poner `ensalida=0`).

---

## 4. Despliegue y Actualización

### Actualización de Esquema (Schema Healer)
El sistema auto-repara su esquema en cada reinicio. Si agrega una columna nueva en el código (`AppDbContext`), simplemente reinicie el servicio de .NET y el `SchemaHealer` se encargará de crearla.

### Generación de Release
Utilice el script oficial:
```powershell
.\scripts\create-release-bundle.ps1
```
Este script genera un `.zip` con todos los binarios y configuraciones optimizadas para el entorno ISTPET.
