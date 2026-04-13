# Integración de Datos Maestros: Núcleo SIGAFI

Este documento académico-técnico detalla la arquitectura de integración entre el sistema de Logística y el núcleo institucional SIGAFI, basada en un modelo de **Puente Híbrido Universal**.

---

## 1. Arquitectura del Puente (Hybrid Universal Bridge)

La integración no es un simple enlace de datos, sino un sistema orquestado que garantiza la **Autonomía Operativa** mediante tres niveles de consulta:

| Nivel | Componente | Acción |
| :--- | :--- | :--- |
| **L1: Cache Local** | Mirror DB | Respuesta instantánea (<5ms) para datos previamente sincronizados. |
| **L2: JIT Fetching** | SIGAFI Bridge | Búsqueda profunda en `sigafi_es` con inyección automática en el espejo. |
| **L3: Resilience Hub** | Polly Pipeline | Gestión de fallos y latencia mediante Circuit Breaker. |

---

## 2. El Pipeline de Resiliencia (`SigafiResiliencePipeline`)

Para mitigar la volatilidad de la red institucional, el sistema implementa un disyuntor lógico:
*   **Detección de Caída**: Si una consulta cruzada falla o excede el umbral de tiempo (2s), el puente se "abre".
*   **Operación Degradada**: La aplicación redirige todas las peticiones al Espejo Local, permitiendo el despacho de vehículos basado en la última sincronización válida.
*   **Auto-Reconexión**: El sistema realiza pruebas de "pulso" periódicas para re-establecer el puente automáticamente.

---

## 3. Matriz de Consultas Transversales (Cross-Schema)

El sistema utiliza SQL de alto rendimiento para extraer la "Verdad Central":

### 3.1. Extracción con Enriquecimiento JIT
```sql
SELECT
    a.idAlumno, a.primerNombre, a.apellidoPaterno,
    m.paralelo, s.nombre AS Jornada,
    TO_BASE64(a.foto) AS FotoBase64
FROM sigafi_es.alumnos a
JOIN sigafi_es.matriculas m ON m.idAlumno = a.idAlumno
JOIN sigafi_es.periodos p ON p.idPeriodo = m.idPeriodo
LEFT JOIN sigafi_es.secciones s ON s.idSeccion = m.idSeccion
WHERE a.idAlumno = @cedula AND p.activo = 1;
```

### 3.2. Orquestación de Agenda Diaria
Detecta las prácticas programadas en SIGAFI, otorgando prioridad sobre las asignaciones fijas. Esto alimenta el selector predictivo del **Control Hub**.

---

## 4. El Motor de Sincronización (Master Sync)

El `DataSyncService` ejecuta una secuencia de **20 pasos de integridad** para alinear el ecosistema local:
1.  Sincronización de Catálogos (Periodos, Carreras, Modalidades).
2.  Sincronización de Unidades (Vehículos y sus categorías).
3.  Sincronización de Recursos (Instructores y Personal Admin).
4.  Carga de Matrículas Vigentes.
5.  Actualización de Agendas y Horarios.

---

## 5. Protocolo de Acceso y Blindaje

Para garantizar que el sistema de logística sea un ente **no intrusivo**, se aplican estas restricciones:
*   **Read-Only Strict**: El backend carece de permisos `WRITE` en el esquema de SIGAFI.
*   **Sanitización de Carga**: Cada registro extraído pasa por el **Escudo de Datos** para corregir inconsistencias del sistema central (mayúsculas, espacios, truncamiento de campos largos).
*   **Desacoplamiento de Identidad**: Las contraseñas en SIGAFI se validan mediante el **Puente de Seguridad Híbrida**, soportando hashing BCrypt heredado.
