import React, { useState, useCallback } from 'react';
import dashboardService from '../../services/dashboardService';

const hoy = () => new Date().toISOString().split('T')[0];

const ACCIONES = [
  { value: '', label: 'Todas las acciones' },
  { value: 'LOGIN', label: 'LOGIN' },
  { value: 'LOGIN_FAIL', label: 'LOGIN_FAIL' },
  { value: 'SALIDA', label: 'SALIDA' },
  { value: 'LLEGADA', label: 'LLEGADA' },
  { value: 'ELIMINAR_SALIDA', label: 'ELIMINAR_SALIDA' },
  { value: 'SYNC', label: 'SYNC' },
  { value: 'SYNC_FAIL', label: 'SYNC_FAIL' },
];

const ACTION_STYLE = {
  LOGIN: 'bg-blue-500/15 text-blue-600',
  LOGIN_FAIL: 'bg-rose-500/15 text-rose-500',
  SALIDA: 'bg-[var(--istpet-gold)]/15 text-[var(--istpet-gold)]',
  LLEGADA: 'bg-emerald-500/15 text-emerald-600',
  ELIMINAR_SALIDA: 'bg-rose-500/20 text-rose-600',
  SYNC: 'bg-purple-500/15 text-purple-600',
  SYNC_FAIL: 'bg-rose-500/15 text-rose-500',
};

function accionChip(accion) {
  return ACTION_STYLE[accion] ?? 'bg-[var(--apple-border)]/60 text-[var(--apple-text-sub)]';
}

function fmtFechaHora(iso) {
  if (!iso) return '—';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString('es-EC', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
    hour12: false,
  });
}

function SpinnerIcon() {
  return (
    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
    </svg>
  );
}

const inputCls =
  'w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-2xl px-4 py-2.5 text-sm font-bold text-[var(--apple-text-main)] placeholder:text-[var(--apple-text-sub)]/45 focus:border-[var(--apple-primary)] shadow-inner transition-all outline-none';
const labelCls = 'text-[9px] font-black uppercase tracking-[0.2em] text-[var(--apple-text-sub)] opacity-60 block mb-1.5';

export default function LogsPanel() {
  const [filtros, setFiltros] = useState({
    fechaInicio: hoy(),
    fechaFin: hoy(),
    usuario: '',
    accion: '',
    busqueda: '',
  });
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [cargado, setCargado] = useState(false);

  const ejecutar = useCallback(async (f) => {
    setLoading(true);
    setError('');
    try {
      const res = await dashboardService.getAuditLogs(f);
      setData(res);
      setCargado(true);
    } catch (e) {
      setError(e?.message || 'Error al cargar los logs');
      setData([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFiltros((prev) => ({ ...prev, [name]: value }));
  };

  const handleBusqueda = (e) => {
    setFiltros((prev) => ({ ...prev, busqueda: e.target.value }));
  };

  const buscar = () => {
    ejecutar(filtros);
  };

  const limpiar = () => {
    const next = { fechaInicio: hoy(), fechaFin: hoy(), usuario: '', accion: '', busqueda: '' };
    setFiltros(next);
    setData([]);
    setCargado(false);
    setError('');
  };

  const hayFiltrosActivos = filtros.busqueda || filtros.usuario || filtros.accion ||
    filtros.fechaInicio !== hoy() || filtros.fechaFin !== hoy();

  return (
    <div className="space-y-4 animate-apple-in">

      {/* Filtros */}
      <div className="apple-card p-4 bg-[var(--apple-card)] border border-[var(--apple-border)] space-y-3">
        {/* Fechas */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className={labelCls}>Desde</label>
            <input type="date" name="fechaInicio" value={filtros.fechaInicio} onChange={handleChange} className={inputCls} />
          </div>
          <div>
            <label className={labelCls}>Hasta</label>
            <input type="date" name="fechaFin" value={filtros.fechaFin} onChange={handleChange} className={inputCls} />
          </div>
        </div>

        {/* Acción + Usuario */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className={labelCls}>Acción</label>
            <select name="accion" value={filtros.accion} onChange={handleChange} className={`${inputCls} appearance-none`}>
              {ACCIONES.map(a => <option key={a.value} value={a.value}>{a.label}</option>)}
            </select>
          </div>
          <div>
            <label className={labelCls}>Usuario</label>
            <input type="text" name="usuario" value={filtros.usuario} onChange={handleChange} placeholder="Cédula o login…" className={inputCls} />
          </div>
        </div>

        {/* Búsqueda + limpiar */}
        <div className="flex items-end gap-2">
          <div className="flex-1 min-w-0">
            <label className={labelCls}>Buscar en detalles / entidad</label>
            <div className="relative">
              <input
                type="text"
                name="busqueda"
                value={filtros.busqueda}
                onChange={handleBusqueda}
                placeholder="Texto libre…"
                className={`${inputCls} pr-10`}
              />
              {loading ? (
                <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)]"><SpinnerIcon /></span>
              ) : (
                <svg className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--apple-text-sub)] opacity-40" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z" />
                </svg>
              )}
            </div>
          </div>
          {(hayFiltrosActivos || cargado) && (
            <button type="button" onClick={limpiar} className="shrink-0 text-[10px] font-black uppercase tracking-widest px-3 py-2.5 rounded-2xl border border-[var(--apple-border)] text-[var(--apple-text-sub)] hover:border-[var(--apple-primary)] hover:text-[var(--apple-primary)] transition-all">
              Limpiar
            </button>
          )}
        </div>

        <div className="flex flex-col sm:flex-row gap-2 pt-1">
          <button
            type="button"
            onClick={buscar}
            disabled={loading}
            className="btn-apple-primary w-full sm:flex-1 py-3 rounded-2xl text-xs font-black uppercase tracking-widest disabled:opacity-50 disabled:pointer-events-none"
          >
            {loading ? 'Buscando…' : 'Buscar'}
          </button>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div className="rounded-2xl border border-rose-500/20 bg-rose-500/10 px-5 py-4 text-sm font-bold text-rose-500">{error}</div>
      )}

      {/* Tabla desktop */}
      {!error && (
        <div className="hidden md:block overflow-x-auto rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-card)]">
          <table className="w-full min-w-[900px] text-left border-collapse">
            <thead>
              <tr className="border-b border-[var(--apple-border)] bg-[var(--apple-bg)]/30">
                {['Fecha / Hora', 'Usuario', 'Acción', 'Entidad', 'IP Origen', 'Detalles'].map(h => (
                  <th key={h} className="px-4 py-3 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--apple-border)]">
              {loading && data.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-16 text-center">
                    <div className="flex flex-col items-center gap-3">
                      <div className="w-8 h-8 rounded-full border-2 border-t-[var(--apple-primary)] border-transparent animate-spin" />
                      <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40">Cargando logs…</p>
                    </div>
                  </td>
                </tr>
              )}
              {!loading && !cargado && (
                <tr>
                  <td colSpan={6} className="py-16 text-center">
                    <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-50 max-w-sm mx-auto px-4">
                      Ajusta los filtros y pulsa <span className="text-[var(--apple-primary)]">Buscar</span> para cargar los logs.
                    </p>
                  </td>
                </tr>
              )}
              {!loading && data.length === 0 && cargado && (
                <tr>
                  <td colSpan={6} className="py-16 text-center">
                    <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40">Sin registros para los filtros seleccionados</p>
                  </td>
                </tr>
              )}
              {data.map((item, index) => (
                <tr
                  key={item.id}
                  className="hover:bg-[var(--apple-bg)]/40 transition-colors animate-apple-in"
                  style={{ animationDelay: `${Math.min(index * 0.03, 0.6)}s`, animationFillMode: 'both' }}
                >
                  <td className="px-4 py-3 text-[10px] font-bold tabular-nums text-[var(--apple-text-sub)] whitespace-nowrap">{fmtFechaHora(item.fecha_hora)}</td>
                  <td className="px-4 py-3 text-[11px] font-black text-[var(--apple-text-main)] whitespace-nowrap">{item.usuario}</td>
                  <td className="px-4 py-3">
                    <span className={`text-[9px] font-black px-2.5 py-1 rounded-full uppercase tracking-wider ${accionChip(item.accion)}`}>
                      {item.accion}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-[10px] font-bold text-[var(--apple-text-sub)] whitespace-nowrap tabular-nums">{item.entidad_id || '—'}</td>
                  <td className="px-4 py-3 text-[10px] font-bold text-[var(--apple-text-sub)] whitespace-nowrap tabular-nums">{item.ip_origen || '—'}</td>
                  <td className="px-4 py-3 text-[10px] font-bold text-[var(--apple-text-sub)] max-w-[300px] truncate" title={item.detalles ?? ''}>
                    {item.detalles || '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Cards mobile */}
      {!error && (
        <div className="md:hidden space-y-2.5">
          {loading && data.length === 0 && (
            <div className="flex justify-center py-12">
              <div className="w-8 h-8 rounded-full border-2 border-t-[var(--apple-primary)] border-transparent animate-spin" />
            </div>
          )}
          {!loading && !cargado && (
            <p className="text-center text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-50 py-12 px-4">
              Ajusta los filtros y pulsa <span className="text-[var(--apple-primary)]">Buscar</span> para cargar los logs.
            </p>
          )}
          {!loading && data.length === 0 && cargado && (
            <p className="text-center text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40 py-12">Sin registros</p>
          )}
          {data.map((item, index) => (
            <article
              key={item.id}
              className="rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-card)] p-4 space-y-3 animate-apple-in"
              style={{ animationDelay: `${Math.min(index * 0.03, 0.6)}s`, animationFillMode: 'both' }}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-black text-[var(--apple-text-main)] truncate">{item.usuario}</p>
                  <p className="text-[9px] font-bold text-[var(--apple-text-sub)] opacity-60 tabular-nums mt-0.5">{fmtFechaHora(item.fecha_hora)}</p>
                </div>
                <span className={`shrink-0 text-[9px] font-black px-2.5 py-1 rounded-full uppercase tracking-wider ${accionChip(item.accion)}`}>
                  {item.accion}
                </span>
              </div>
              <div className="flex flex-wrap gap-x-4 gap-y-1 text-[9px] font-bold border-t border-[var(--apple-border)] pt-3">
                {item.entidad_id && (
                  <div>
                    <span className="uppercase tracking-wider text-[var(--apple-text-sub)] opacity-50">Entidad </span>
                    <span className="text-[var(--apple-text-main)] tabular-nums">{item.entidad_id}</span>
                  </div>
                )}
                {item.ip_origen && (
                  <div>
                    <span className="uppercase tracking-wider text-[var(--apple-text-sub)] opacity-50">IP </span>
                    <span className="text-[var(--apple-text-main)] tabular-nums">{item.ip_origen}</span>
                  </div>
                )}
              </div>
              {item.detalles && (
                <p className="text-[9px] font-bold text-[var(--apple-text-sub)] border-t border-[var(--apple-border)] pt-3 break-words">
                  {item.detalles}
                </p>
              )}
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
