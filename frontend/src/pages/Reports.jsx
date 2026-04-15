import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import reportService from '../services/reportService';
import { logisticaService } from '../services/logisticaService';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import './Reports.css';

const inputCls =
    'w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-2xl px-4 py-2.5 text-sm font-bold text-[var(--apple-text-main)] placeholder:text-[var(--apple-text-sub)]/45 focus:border-[var(--apple-primary)] shadow-inner transition-all outline-none';

const labelCls = 'text-[9px] font-black uppercase tracking-[0.2em] text-[var(--apple-text-sub)] opacity-60 block mb-1.5';

/**
 * Reports: Absolute SIGAFI Parity Edition 2026.
 * All property naming aligned with SIGAFI / Backend Refactor (idAlumno, idProfesor, numeroVehiculo).
 */
const Reports = () => {
    const [loading, setLoading] = useState(false);
    const [data, setData] = useState([]);
    const [cargado, setCargado] = useState(false);
    const [error, setError] = useState('');
    const [instructores, setInstructores] = useState([]);
    const [filtros, setFiltros] = useState({
        fechaInicio: new Date().toISOString().split('T')[0],
        fechaFin: new Date().toISOString().split('T')[0],
        instructorId: ''
    });
    useEffect(() => {
        cargarInstructores();
    }, []);

    const cargarInstructores = async () => {
        try {
            const res = await logisticaService.getInstructores();
            setInstructores(res);
        } catch (error) {
            console.error('Error cargando instructores:', error);
        }
    };

    const ejecutarReporte = async () => {
        setLoading(true);
        setError('');
        try {
            const res = await reportService.getReportePracticas(filtros);
            const rawData = res?.Data || res?.data || (Array.isArray(res) ? res : []);
            const listaNormalizada = rawData.map(item => ({
                idPractica: item.idPractica || item.IdPractica,
                idProfesor: item.idProfesor || item.IdProfesor,
                profesor: item.profesor || item.Profesor,
                categoria: item.categoria || item.Categoria,
                numeroVehiculo: item.numeroVehiculo || item.NumeroVehiculo,
                idAlumno: item.idAlumno || item.IdAlumno,
                nomina: item.nomina || item.Nomina,
                dia: item.dia || item.Dia,
                fecha: item.fecha || item.Fecha,
                horaSalida: item.horaSalida || item.HoraSalida,
                horaLlegada: item.horaLlegada || item.HoraLlegada,
                tiempo: item.tiempo || item.Tiempo,
                observaciones: item.observaciones || item.Observaciones,
                cancelado: item.cancelado || item.Cancelado || 0
            }));
            setData(listaNormalizada);
            setCargado(true);
        } catch (err) {
            console.error('Error al generar reporte:', err);
            const msg = err?.message ?? err?.Message ?? err?.title ?? 'Error al generar el reporte';
            setError(typeof msg === 'string' ? msg : 'Error al generar el reporte');
            setData([]);
            setCargado(true);
        } finally {
            setLoading(false);
        }
    };

    const handleFiltroChange = (e) => {
        const { name, value } = e.target;
        setFiltros(prev => ({ ...prev, [name]: value }));
    };

    const exportToExcel = () => {
        const worksheet = XLSX.utils.json_to_sheet(data.map(item => ({
            'idprofesor': item.idProfesor,
            'profesor': item.profesor,
            'categoria': item.categoria,
            'numero_vehiculo': item.numeroVehiculo,
            'idalumno': item.idAlumno,
            'nomina': item.nomina,
            'dia': item.dia,
            'fecha': item.fecha,
            'hora_salida': item.horaSalida,
            'hora_llegada': item.horaLlegada || '',
            'tiempo': item.tiempo,
            'cancelado': item.cancelado
        })));
        const workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, worksheet, "Reporte_Practicas");
        const excelBuffer = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
        const dataBlob = new Blob([excelBuffer], { type: 'application/octet-stream' });
        saveAs(dataBlob, `Reporte_ISTPET_${filtros.fechaInicio}_${filtros.fechaFin}.xlsx`);
    };

    return (
        <Layout>
            <div className="reports-page space-y-5 animate-apple-in w-full max-w-full min-w-0">
                {/* Cabecera — misma jerarquía que /historial */}
                <div className="px-1 flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
                    <div>
                        <p className="text-[10px] lg:text-xs font-black text-[var(--istpet-gold)] uppercase tracking-[0.2em] mb-0">
                            Administración
                        </p>
                        <h1 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase leading-tight">
                            Reporte de prácticas
                        </h1>
                    </div>
                    <button
                        type="button"
                        onClick={exportToExcel}
                        disabled={data.length === 0}
                        className={`w-full sm:w-auto shrink-0 flex items-center justify-center gap-2 px-4 py-2.5 rounded-2xl text-xs font-black uppercase tracking-widest transition-all self-start sm:self-auto disabled:opacity-35 disabled:pointer-events-none
                            ${data.length > 0 
                                ? 'bg-emerald-600 text-white border border-emerald-600 shadow-lg shadow-emerald-600/20 hover:scale-[1.02] active:scale-95' 
                                : 'border border-[var(--apple-border)] text-[var(--apple-text-sub)] hover:border-[var(--apple-primary)] hover:text-[var(--apple-primary)]'
                            }
                        `}
                    >
                        <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                        Descargar Excel
                    </button>
                </div>

                <div className="space-y-4">
                    {/* Filtros — mismo bloque que HistorialPanel */}
                    <div className="apple-card p-4 bg-[var(--apple-card)] border border-[var(--apple-border)] space-y-3">
                        <div className="grid grid-cols-2 gap-3">
                            <div>
                                <label className={labelCls}>Desde</label>
                                <input type="date" name="fechaInicio" value={filtros.fechaInicio} onChange={handleFiltroChange} className={inputCls} />
                            </div>
                            <div>
                                <label className={labelCls}>Hasta</label>
                                <input type="date" name="fechaFin" value={filtros.fechaFin} onChange={handleFiltroChange} className={inputCls} />
                            </div>
                        </div>
                        <div>
                            <label className={labelCls}>Instructor</label>
                            <select name="instructorId" value={filtros.instructorId} onChange={handleFiltroChange} className={`${inputCls} appearance-none`}>
                                <option value="">Todos</option>
                                {instructores.map(i => (
                                    <option key={i.idInstructor} value={i.idInstructor}>{i.fullName}</option>
                                ))}
                            </select>
                        </div>
                        <div className="flex flex-col sm:flex-row gap-2 pt-1">
                            <button
                                type="button"
                                onClick={ejecutarReporte}
                                disabled={loading}
                                className="btn-apple-primary w-full sm:flex-1 py-3 rounded-2xl text-xs font-black uppercase tracking-widest disabled:opacity-50 disabled:pointer-events-none"
                            >
                                {loading ? 'Buscando…' : 'Buscar'}
                            </button>
                        </div>
                    </div>

                    {error && (
                        <div className="rounded-2xl border border-rose-500/20 bg-rose-500/10 px-5 py-4 text-sm font-bold text-rose-500">
                            {error}
                        </div>
                    )}

                    {!error && (
                        <>
                            <div className="md:hidden space-y-2.5">
                                {loading && data.length === 0 && (
                                    <div className="flex justify-center py-12">
                                        <div className="w-8 h-8 rounded-full border-2 border-t-[var(--apple-primary)] border-transparent animate-spin" />
                                    </div>
                                )}
                                {!loading && !cargado && (
                                    <p className="text-center text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-50 py-12 px-4">
                                        Ajusta los filtros y pulsa <span className="text-[var(--apple-primary)]">Buscar</span> para cargar el reporte.
                                    </p>
                                )}
                                {!loading && cargado && data.length === 0 && (
                                    <p className="text-center text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40 py-12">
                                        Sin registros para los filtros seleccionados
                                    </p>
                                )}
                                {cargado && data.length > 500 && (
                                    <div className="rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-card)] px-4 py-3">
                                        <p className="text-[10px] font-black text-[var(--apple-primary)] uppercase tracking-widest leading-relaxed">
                                            Limitado a los últimos 500 de {data.length} registros. Use Excel para el total.
                                        </p>
                                    </div>
                                )}
                                {cargado && !loading && data.length > 0 && data.slice(0, 500).map((item, index) => (
                                    <article
                                        key={item.idPractica}
                                        className="rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-bg)]/30 p-4 space-y-3 animate-apple-in"
                                        style={{ animationDelay: `${Math.min(index * 0.03, 0.6)}s`, animationFillMode: 'both' }}
                                    >
                                <div className="flex flex-wrap items-start justify-between gap-2">
                                    <div className="min-w-0 flex-1">
                                        <p className="text-[10px] font-black uppercase tracking-wider text-[var(--apple-text-sub)]">Profesor</p>
                                        <p className="text-sm font-black text-[var(--apple-text-main)] uppercase truncate">{item.profesor}</p>
                                    </div>
                                    <span className="text-[9px] font-black px-2 py-1 rounded-lg bg-[var(--apple-bg)] border border-[var(--apple-border)] text-[var(--apple-primary)] shrink-0">
                                        {item.categoria}
                                    </span>
                                </div>
                                <div className="grid grid-cols-2 gap-3 text-xs">
                                    <div>
                                        <p className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-70">Alumno</p>
                                        <p className="font-bold text-[var(--apple-text-main)] uppercase truncate">{item.nomina}</p>
                                    </div>
                                    <div>
                                        <p className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-70">Vehículo</p>
                                        <div className="inline-flex w-8 h-8 rounded-lg bg-[var(--apple-text-main)] text-[var(--apple-bg)] items-center justify-center font-black text-xs mt-0.5">
                                            {item.numeroVehiculo}
                                        </div>
                                    </div>
                                    <div>
                                        <p className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-70">Fecha</p>
                                        <p className="font-bold tabular-nums">{item.fecha}</p>
                                    </div>
                                    <div>
                                        <p className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-70">Semana</p>
                                        <p className="font-bold text-[var(--apple-text-sub)] uppercase italic">{item.dia}</p>
                                    </div>
                                </div>
                                <div className="flex flex-wrap gap-3 text-xs border-t border-[var(--apple-border)] pt-3">
                                    <div>
                                        <span className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-70">Salida </span>
                                        <span className="font-black text-emerald-600 tabular-nums">{item.horaSalida}</span>
                                    </div>
                                    <div>
                                        <span className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-70">Llegada </span>
                                        <span className="font-black text-rose-500 tabular-nums">{item.horaLlegada || '--:--:--'}</span>
                                    </div>
                                    <div>
                                        <span className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] opacity-70">Tiempo </span>
                                        <span className="font-black tabular-nums">{item.tiempo}</span>
                                    </div>
                                </div>
                                <div className="flex justify-between items-center gap-2">
                                    {item.horaLlegada ? (
                                        <span className="text-[8px] font-black bg-emerald-500 text-white px-2 py-1 rounded-full uppercase">Completada</span>
                                    ) : (
                                        <span className="text-[8px] font-black bg-amber-500 text-white px-2 py-1 rounded-full uppercase animate-pulse">En pista</span>
                                    )}
                                </div>
                            </article>
                                ))}
                            </div>

                            <div className="hidden md:block rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-card)] overflow-hidden">
                                {cargado && data.length > 500 && (
                                    <div className="px-6 py-3 border-b border-[var(--apple-border)] bg-[var(--apple-bg)]/20">
                                        <p className="text-[10px] font-black text-[var(--apple-primary)] uppercase tracking-widest leading-relaxed">
                                            Mostrando 500 de {data.length} registros para optimizar carga. Use «Descargar Excel» para el reporte completo.
                                        </p>
                                    </div>
                                )}
                                <div className="reports-table-wrap overflow-x-auto custom-scrollbar">
                        <table className="reports-data-table w-full min-w-[920px] text-left border-collapse">
                            <thead>
                                <tr className="border-b border-[var(--apple-border)] bg-[var(--apple-bg)]/30">
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">idprofesor</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">profesor</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">categoria</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">numero_vehiculo</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">idalumno</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">nomina</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">dia</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">fecha</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">hora_salida</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">hora_llegada</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">tiempo</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">cancelado</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-[var(--apple-border)]">
                                {loading && data.length === 0 && (
                                    <tr>
                                        <td colSpan="12" className="py-20 text-center">
                                            <div className="flex flex-col items-center gap-3">
                                                <div className="w-12 h-12 rounded-full border-2 border-t-[var(--apple-primary)] border-transparent animate-spin" />
                                                <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40">
                                                    Cargando reporte…
                                                </p>
                                            </div>
                                        </td>
                                    </tr>
                                )}
                                {!loading && !cargado && (
                                    <tr>
                                        <td colSpan="12" className="py-20 text-center px-6">
                                            <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-50 max-w-lg mx-auto leading-relaxed">
                                                Ajusta los filtros y pulsa <span className="text-[var(--apple-primary)]">Buscar</span> para cargar el reporte.
                                            </p>
                                        </td>
                                    </tr>
                                )}
                                {!loading && cargado && data.length === 0 && (
                                    <tr>
                                        <td colSpan="12" className="py-20 text-center">
                                            <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-40">
                                                Sin registros para los filtros seleccionados
                                            </p>
                                        </td>
                                    </tr>
                                )}
                                {cargado && data.slice(0, 500).map((item, index) => (
                                    <tr
                                        key={item.idPractica}
                                        className="hover:bg-[var(--apple-bg)]/60 transition-colors group animate-apple-in"
                                        style={{ animationDelay: `${Math.min(index * 0.03, 0.6)}s`, animationFillMode: 'both' }}
                                    >
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-bold text-[var(--apple-text-sub)] tabular-nums">{item.idProfesor}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-[var(--apple-text-main)] uppercase">{item.profesor}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black">
                                            <span className="bg-[var(--apple-bg)] px-2 py-1 rounded border border-[var(--apple-border)] text-[var(--apple-primary)]">
                                                {item.categoria}
                                            </span>
                                        </td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-center">
                                            <span className="inline-flex w-7 h-7 rounded bg-[var(--apple-text-main)] text-[var(--apple-bg)] items-center justify-center font-black text-[10px]">
                                                {item.numeroVehiculo}
                                            </span>
                                        </td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-bold text-[var(--apple-text-sub)] tabular-nums">{item.idAlumno}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-[var(--apple-text-main)] uppercase truncate max-w-[150px]">{item.nomina}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[10px] font-bold text-[var(--apple-text-sub)] uppercase italic">{item.dia}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-bold tabular-nums">{item.fecha}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-emerald-600 tabular-nums">{item.horaSalida}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-rose-500 tabular-nums">{item.horaLlegada || "--:--:--"}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-bold tabular-nums">{item.tiempo}</td>
                                        <td className="px-3 lg:px-5 py-3 lg:py-4 text-center">
                                            {item.cancelado ? (
                                                <span className="text-[10px] font-black text-rose-500">SÍ</span>
                                            ) : (
                                                <span className="text-[10px] font-black text-[var(--apple-text-sub)] opacity-20">NO</span>
                                            )}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                                </div>
                            </div>
                        </>
                    )}
                </div>
            </div>
        </Layout>
    );
};

export default Reports;
