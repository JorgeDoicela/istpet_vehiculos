/** Utilidades compartidas para filas de agenda (SIGAFI / logística). */

/**
 * El API (.NET) serializa en camelCase (`alumnoNombre`); componentes viejos esperan PascalCase.
 */
export function normalizeAgendaPractica(ag) {
  if (!ag || typeof ag !== 'object') return ag;
  return {
    ...ag,
    AlumnoNombre: ag.alumnoNombre ?? ag.AlumnoNombre ?? '',
    VehiculoDetalle: ag.vehiculoDetalle ?? ag.VehiculoDetalle ?? '',
    ProfesorNombre: ag.profesorNombre ?? ag.ProfesorNombre ?? '',
    estadoOperativo: ag.estadoOperativo ?? ag.EstadoOperativo ?? 'sin_sincronizar',
    HoraPlanificadaInicio: ag.horaPlanificadaInicio ?? ag.HoraPlanificadaInicio,
    HoraPlanificadaFin: ag.horaPlanificadaFin ?? ag.HoraPlanificadaFin,
    EsPlanificado: ag.esPlanificado ?? ag.EsPlanificado ?? false
  };
}

/**
 * Práctica que aún aplica para “tiene algo agendado” en sugerencias de salida:
 * fecha hoy o futura y no cerrada en el espejo local (completada/cancelada).
 * Quien no tiene cita, ya cerró o solo figura en el pasado → false.
 */
export function agendaPracticaVigenteParaSugerencia(ag) {
  if (!ag || typeof ag !== 'object') return false;
  const op = String(ag.estadoOperativo ?? ag.EstadoOperativo ?? '').toLowerCase();
  if (op === 'completada' || op === 'cancelada') return false;
  const ymd = agendaYmdFromApi(ag.fecha);
  if (!ymd) return false;
  return ymd >= ymdLocalHoy();
}

export function agendaYmdFromApi(fecha) {
  if (!fecha) return '';
  if (fecha instanceof Date && !Number.isNaN(fecha.getTime())) {
    const y = fecha.getFullYear();
    const mo = String(fecha.getMonth() + 1).padStart(2, '0');
    const d = String(fecha.getDate()).padStart(2, '0');
    return `${y}-${mo}-${d}`;
  }
  const m = String(fecha).match(/^(\d{4})-(\d{2})-(\d{2})/);
  return m ? `${m[1]}-${m[2]}-${m[3]}` : '';
}

export function ymdLocalHoy() {
  const t = new Date();
  return `${t.getFullYear()}-${String(t.getMonth() + 1).padStart(2, '0')}-${String(t.getDate()).padStart(2, '0')}`;
}

export function fmtFechaAgenda(fecha) {
  if (!fecha) return '';
  const s = String(fecha);
  const m = s.match(/^(\d{4})-(\d{2})-(\d{2})/);
  if (m) {
    const d = new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]));
    return d.toLocaleDateString('es-EC', { weekday: 'short', day: 'numeric', month: 'short' });
  }
  const d = new Date(fecha);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleDateString('es-EC', { weekday: 'short', day: 'numeric', month: 'short' });
}

/**
 * Formatea un TimeSpan (HH:mm:ss o HH:mm) que viene como string del Backend (.NET).
 * Evita usar 'new Date()' que falla con strings de solo tiempo.
 */
export function fmtTimeSpan(val) {
  if (!val) return '--:--';
  const s = String(val);
  // Si parece un ISO completo, extraer tiempo
  if (s.includes('T')) {
      const parts = s.split('T')[1].split(':');
      return `${parts[0]}:${parts[1]}`;
  }
  // Si es HH:mm:ss o HH:mm
  const matches = s.match(/^(\d{1,2}):(\d{2})/);
  if (matches) {
      return `${matches[1].padStart(2, '0')}:${matches[2]}`;
  }
  return s.substring(0, 5);
}

export function fmtUltimaCargaAgenda(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleString('es-EC', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
}

export function estadoAgendaChip(estado) {
  const e = String(estado || 'sin_sincronizar').toLowerCase();
  const map = {
    pendiente: { label: 'Pendiente', cls: 'bg-amber-500/20 text-amber-900' },
    en_pista: { label: 'En pista', cls: 'bg-rose-500 text-white' },
    completada: { label: 'Regresó', cls: 'bg-emerald-600 text-white' },
    cancelada: { label: 'Cancelada', cls: 'bg-[var(--apple-border)] text-[var(--apple-text-sub)]' },
    sin_sincronizar: { label: 'Sin sync local', cls: 'bg-[var(--apple-primary)]/12 text-[var(--apple-primary)]' }
  };
  return map[e] || map.sin_sincronizar;
}
