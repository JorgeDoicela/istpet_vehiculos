import React, { useState, useEffect, useCallback, useRef } from 'react';
import dashboardService from '../../services/dashboardService';
import { logisticaService } from '../../services/logisticaService';

const hoy = () => new Date().toISOString().split('T')[0];

const ESTADOS = [
  { value: '', label: 'Todos los estados' },
  { value: 'en_pista', label: 'En pista' },
  { value: 'completada', label: 'Completada' },
  { value: 'cancelada', label: 'Cancelada' },
];

function estadoChip(item) {
  if (item.cancelado) return { label: 'Cancelada', cls: 'bg-[var(--apple-border)]/60 text-[var(--apple-text-sub)]' };
  if (item.horaLlegada) return { label: 'Completada', cls: 'bg-emerald-500/15 text-emerald-500' };
  return { label: 'En pista', cls: 'bg-rose-500/15 text-rose-500 animate-pulse' };
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

export default function HistorialPanel() {
  const [filtros, setFiltros] = useState({
    fechaInicio: hoy(),
    fechaFin: hoy(),
    instructorId: '',
    busqueda: '',
    estado: '',
  });
  const [data, setData] = useState([]);
  const [instructores, setInstructores] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [cargado, setCargado] = useState(false);
  const busquedaRef = useRef(null);

  useEffect(() => {
    logisticaService.getInstructores().then(setInstructores).catch(() => {});
  }, []);

  const ejecutar = useCallback(async (f) => {
    setLoading(true);
    setError('');
    try {
      const res = await dashboardService.getHistorialPracticas(f);
      setData(res);
      setCargado(true);
    } catch (e) {
      setError(e?.message || 'Error al cargar el historial');
      setData([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFiltros((prev) => ({ ...prev, [name]: value }));
  };

  const handleBusquedaChange = (e) => {
    const value = e.target.value;
    setFiltros((prev) => ({ ...prev, busqueda: value }));
  };

  const buscar = () => {
    ejecutar(filtros);
  };

  const limpiar = () => {
    const next = { fechaInicio: hoy(), fechaFin: hoy(), instructorId: '', busqueda: '', estado: '' };
    setFiltros(next);
    setData([]);
    setCargado(false);
    setError('');
  };

  const hayFiltrosActivos = filtros.busqueda || filtros.instructorId || filtros.estado ||
    filtros.fechaInicio !== hoy() || filtros.fechaFin !== hoy();

  return (
    <div className="space-y-4 animate-apple-in max-w-full overflow-hidden">
      {/* Barra de filtros */}
      <div className="apple-card p-4 bg-[var(--apple-card)] border border-[var(--apple-border)] space-y-3">

        {/* Fila 1: fechas */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-1">
          <div>
            <label className={labelCls}>Desde</label>
            <input type="date" name="fechaInicio" value={filtros.fechaInicio} onChange={handleChange} className={inputCls} />
          </div>
          <div>
            <label className={labelCls}>Hasta</label>
            <input type="date" name="fechaFin" value={filtros.fechaFin} onChange={handleChange} className={inputCls} />
          </div>
        </div>

        {/* Fila 2: selects */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-1">
          <div>
            <label className={labelCls}>Instructor</label>
            <select name="instructorId" value={filtros.instructorId} onChange={handleChange} className={`${inputCls} appearance-none`}>
              <option value="">Todos</option>
              {instructores.map(i => (
                <option key={i.idInstructor} value={i.idInstructor}>{i.fullName}</option>
              ))}
            </select>
          </div>
          <div>
            <label className={labelCls}>Estado</label>
            <select name="estado" value={filtros.estado} onChange={handleChange} className={`${inputCls} appearance-none`}>
              {ESTADOS.map(e => <option key={e.value} value={e.value}>{e.label}</option>)}
            </select>
          </div>
        </div>

        {/* Fila 3: búsqueda + limpiar */}
        <div className="flex flex-col sm:flex-row sm:items-end gap-2">
          <div className="flex-1 min-w-0">
            <label className={labelCls}>Buscar alumno / instructor</label>
            <div className="relative">
              <input
                ref={busquedaRef}
                type="text"
                name="busqueda"
                value={filtros.busqueda}
                onChange={handleBusquedaChange}
                placeholder="Nombre, cédula..."
                className={`${inputCls} pr-10`}
              />
              {loading ? (
                <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)]">
                  <SpinnerIcon />
                </span>
              ) : (
                <svg className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--apple-text-sub)] opacity-40" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z" />
                </svg>
              )}
            </div>
          </div>
          {(hayFiltrosActivos || cargado) && (
            <button
              type="button"
              onClick={limpiar}
              className="w-full sm:w-auto shrink-0 text-[10px] font-black uppercase tracking-widest px-3 py-2.5 rounded-2xl border border-[var(--apple-border)] text-[var(--apple-text-sub)] hover:border-[var(--apple-primary)] hover:text-[var(--apple-primary)] transition-all"
            >
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
        <div className="rounded-2xl border border-rose-500/20 bg-rose-500/10 px-5 py-4 text-sm font-bold text-rose-500">
          {error}
        </div>
      )}

      {/* Tabla desktop */}
      {!error && (
        <div className="hidden md:block overflow-x-auto rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-card)]">
          <table className="w-full min-w-[1000px] text-left border-collapse">
            <thead>
              <tr className="border-b border-[var(--apple-border)] bg-[var(--apple-bg)]/30">
                {['# Veh.', 'Instructor', 'Día', 'Fecha', 'H. Salida', 'H. Llegada', 'Tiempo', 'En práctica', 'User salida', 'User llegada'].map(h => (
                  <th key={h} className="px-4 py-3 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--apple-border)]">
              {loading && data.length === 0 && (
                <tr>
                  <td colSpan={10} className="py-16 text-center">
                    <div className="flex flex-col items-center gap-3">
                      <div className="w-8 h-8 rounded-full border-2 border-t-[var(--apple-primary)] border-transparent animate-spin" />
                      <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40">Cargando historial...</p>
                    </div>
                  </td>
                </tr>
              )}
              {!loading && !cargado && (
                <tr>
                  <td colSpan={10} className="py-16 text-center">
                    <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-50 max-w-sm mx-auto px-4">
                      Ajusta los filtros y pulsa <span className="text-[var(--apple-primary)]">Buscar</span> para cargar el historial.
                    </p>
                  </td>
                </tr>
              )}
              {!loading && data.length === 0 && cargado && (
                <tr>
                  <td colSpan={10} className="py-16 text-center">
                    <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40">Sin registros para los filtros seleccionados</p>
                  </td>
                </tr>
              )}
              {data.map((item, index) => (
                <tr
                  key={item.idPractica}
                  className="hover:bg-[var(--apple-bg)]/40 transition-colors group animate-apple-in"
                  style={{ animationDelay: `${Math.min(index * 0.03, 0.6)}s`, animationFillMode: 'both' }}
                >
                  {/* # Veh */}
                  <td className="px-4 py-3 text-center">
                    <span className="inline-flex w-8 h-8 rounded-lg bg-[var(--apple-text-main)] text-[var(--apple-bg)] items-center justify-center font-black text-[11px]">
                      {item.numeroVehiculo}
                    </span>
                  </td>
                  {/* Instructor + alumno (subtexto) */}
                  <td className="px-4 py-3 max-w-[200px]">
                    <p className="text-[11px] font-black text-[var(--apple-text-main)] uppercase truncate">{item.profesor}</p>
                    <p className="text-[9px] font-bold text-[var(--apple-text-sub)] opacity-50 truncate mt-0.5">{item.nomina}</p>
                  </td>
                  {/* Día */}
                  <td className="px-4 py-3 text-[10px] font-bold text-[var(--apple-text-sub)] uppercase italic whitespace-nowrap">{item.dia}</td>
                  {/* Fecha */}
                  <td className="px-4 py-3 text-[11px] font-bold tabular-nums whitespace-nowrap">{item.fecha}</td>
                  {/* H. Salida */}
                  <td className="px-4 py-3 text-[11px] font-black text-emerald-500 tabular-nums whitespace-nowrap">{item.horaSalida}</td>
                  {/* H. Llegada */}
                  <td className="px-4 py-3 text-[11px] font-black text-rose-500 tabular-nums whitespace-nowrap">{item.horaLlegada || '—'}</td>
                  {/* Tiempo */}
                  <td className="px-4 py-3 text-[11px] font-bold tabular-nums text-[var(--apple-text-sub)] whitespace-nowrap">{item.horaLlegada ? item.tiempo : '—'}</td>
                  {/* En práctica */}
                  <td className="px-4 py-3 text-center">
                    {item.enSalida ? (
                      <span className="inline-flex w-5 h-5 rounded border-2 border-[var(--apple-primary)] bg-[var(--apple-primary)] items-center justify-center">
                        <svg className="w-3 h-3 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                        </svg>
                      </span>
                    ) : (
                      <span className="inline-flex w-5 h-5 rounded border-2 border-[var(--apple-border)]" />
                    )}
                  </td>
                  {/* User salida */}
                  <td className="px-4 py-3 text-[10px] font-bold text-[var(--apple-text-sub)] whitespace-nowrap">{item.userSalida || '—'}</td>
                  {/* User llegada */}
                  <td className="px-4 py-3 text-[10px] font-bold text-[var(--apple-text-sub)] whitespace-nowrap">{item.userLlegada || '—'}</td>
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
              Ajusta los filtros y pulsa <span className="text-[var(--apple-primary)]">Buscar</span> para cargar el historial.
            </p>
          )}
          {!loading && data.length === 0 && cargado && (
            <p className="text-center text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40 py-12">
              Sin registros para los filtros seleccionados
            </p>
          )}
          {data.map((item, index) => {
            const chip = estadoChip(item);
            return (
              <article
                key={item.idPractica}
                className="rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-bg)]/30 p-4 space-y-3 animate-apple-in"
                style={{ animationDelay: `${Math.min(index * 0.03, 0.6)}s`, animationFillMode: 'both' }}
              >
                {/* Fila 1: alumno + estado */}
                <div className="flex items-start justify-between gap-2 overflow-hidden">
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-black text-[var(--apple-text-main)] uppercase truncate break-words">{item.nomina}</p>
                    <p className="text-[10px] font-bold text-[var(--apple-text-sub)] opacity-60 truncate mt-0.5">{item.profesor}</p>
                  </div>
                  <span className={`shrink-0 text-[8px] font-black px-2 py-0.5 rounded-full uppercase tracking-wider ${chip.cls}`}>
                    {chip.label}
                  </span>
                </div>
                {/* Fila 2: veh, fecha, día */}
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-y-3 gap-x-2 text-xs">
                  <div>
                    <p className={labelCls}>Vehículo</p>
                    <span className="inline-flex w-7 h-7 rounded-lg bg-[var(--apple-text-main)] text-[var(--apple-bg)] items-center justify-center font-black text-[10px] mt-0.5">
                      {item.numeroVehiculo}
                    </span>
                  </div>
                  <div>
                    <p className={labelCls}>Fecha</p>
                    <p className="font-bold tabular-nums mt-0.5 whitespace-nowrap">{item.fecha}</p>
                  </div>
                  <div className="col-span-2 sm:col-span-1">
                    <p className={labelCls}>Día</p>
                    <p className="font-bold uppercase italic mt-0.5 truncate">{item.dia}</p>
                  </div>
                </div>
                {/* Fila 3: horas + tiempo */}
                <div className="flex flex-wrap gap-4 text-xs border-t border-[var(--apple-border)] pt-3">
                  <div>
                    <span className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-50">Salida </span>
                    <span className="font-black text-emerald-500 tabular-nums">{item.horaSalida}</span>
                  </div>
                  <div>
                    <span className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-50">Llegada </span>
                    <span className="font-black text-rose-500 tabular-nums">{item.horaLlegada || '—'}</span>
                  </div>
                  {item.horaLlegada && (
                    <div>
                      <span className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-50">Tiempo </span>
                      <span className="font-bold tabular-nums">{item.tiempo}</span>
                    </div>
                  )}
                </div>
                {/* Fila 4: usuarios */}
                {(item.userSalida || item.userLlegada) && (
                  <div className="grid grid-cols-1 gap-2 text-xs border-t border-[var(--apple-border)] pt-3">
                    {item.userSalida && (
                      <div className="min-w-0">
                        <span className={labelCls}>User salida </span>
                        <p className="font-bold text-[var(--apple-text-sub)] break-words leading-tight">{item.userSalida}</p>
                      </div>
                    )}
                    {item.userLlegada && (
                      <div className="min-w-0">
                        <span className={labelCls}>User llegada </span>
                        <p className="font-bold text-[var(--apple-text-sub)] break-words leading-tight">{item.userLlegada}</p>
                      </div>
                    )}
                  </div>
                )}
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}

