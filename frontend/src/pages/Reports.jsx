import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import reportService from '../services/reportService';
import { logisticaService } from '../services/logisticaService';
import { useTheme } from '../components/common/ThemeContext';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import './Reports.css';

/**
 * Reports: Absolute SIGAFI Parity Edition 2026.
 * All property naming aligned with SIGAFI / Backend Refactor (idAlumno, idProfesor, numeroVehiculo).
 */
const Reports = () => {
    const { theme } = useTheme();
    const [loading, setLoading] = useState(false);
    const [data, setData] = useState([]);
    const [instructores, setInstructores] = useState([]);
    const [filtros, setFiltros] = useState({
        fechaInicio: new Date().toISOString().split('T')[0],
        fechaFin: new Date().toISOString().split('T')[0],
        instructorId: ''
    });
    const [statusMsg, setStatusMsg] = useState('');

    useEffect(() => {
        cargarInstructores();
        ejecutarReporte();
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
        setStatusMsg('');
        try {
            const res = await reportService.getReportePracticas(filtros);
            
            // Extraer lista con soporte para mayúsculas/minúsculas de forma limpia
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
            
            if (listaNormalizada.length > 0) {
                setStatusMsg(`Se encontraron ${listaNormalizada.length} registros totales.`);
            } else {
                setStatusMsg('No se encontraron registros para el periodo seleccionado.');
            }
        } catch (error) {
            console.error('Error al generar reporte:', error);
            setStatusMsg('Error de conexión con el servidor de reportes.');
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
            'ID Profesor': item.idProfesor,
            'Profesor': item.profesor,
            'Categoria': item.categoria,
            'Vehículo': item.numeroVehiculo,
            'ID Alumno': item.idAlumno,
            'Nómina': item.nomina,
            'Día': item.dia,
            'Fecha': item.fecha,
            'Hora Salida': item.horaSalida,
            'Hora Llegada': item.horaLlegada || '--:--:--',
            'Tiempo': item.tiempo,
            'Observaciones': item.observaciones
        })));
        const workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, worksheet, "Reporte_Practicas");
        const excelBuffer = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
        const dataBlob = new Blob([excelBuffer], { type: 'application/octet-stream' });
        saveAs(dataBlob, `Reporte_ISTPET_${filtros.fechaInicio}_${filtros.fechaFin}.xlsx`);
    };

    return (
        <Layout>
            <div className="reports-container animate-apple-in w-full max-w-full min-w-0">
                <div className="reports-header mb-6 sm:mb-8">
                    <div className="flex flex-col gap-4 sm:gap-6 lg:flex-row lg:items-center lg:justify-between">
                        <div className="min-w-0">
                            <p className="text-[10px] font-black tracking-[0.3em] text-[var(--istpet-gold)] uppercase mb-1">Administración Zenith</p>
                            <h1 className="text-2xl sm:text-3xl lg:text-4xl font-black text-[var(--apple-text-main)] tracking-tighter break-words">Reporte de Prácticas</h1>
                            {statusMsg && (
                                <p className="text-[10px] font-bold text-[var(--apple-primary)] mt-2 uppercase tracking-wide opacity-80 bg-[var(--apple-primary)]/10 inline-block px-3 py-1 rounded-full">
                                    {statusMsg}
                                </p>
                            )}
                        </div>
                        
                        <div className="flex w-full sm:w-auto shrink-0">
                            <button 
                                onClick={exportToExcel}
                                disabled={data.length === 0}
                                className="w-full sm:w-auto justify-center px-6 py-3 bg-[var(--istpet-gold)] text-white rounded-full text-xs font-black uppercase tracking-widest shadow-xl shadow-amber-500/20 hover:scale-[1.02] active:scale-95 transition-all flex items-center gap-2 disabled:opacity-30"
                            >
                                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                </svg>
                                Descargar Excel
                            </button>
                        </div>
                    </div>
                </div>

                <div className="apple-glass-card p-4 sm:p-6 lg:p-8 mb-6 sm:mb-8 rounded-[1.5rem] sm:rounded-[2.5rem]">
                    <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 sm:gap-6 items-end">
                        <div className="space-y-2">
                            <label className="text-[9px] font-black uppercase tracking-[0.2em] text-[var(--apple-text-sub)] opacity-60">Desde</label>
                            <input 
                                type="date" 
                                name="fechaInicio"
                                value={filtros.fechaInicio}
                                onChange={handleFiltroChange}
                                className="w-full bg-[var(--apple-bg)]/50 border-2 border-[var(--apple-border)] rounded-2xl px-4 py-3 text-sm font-bold text-[var(--apple-text-main)] focus:border-[var(--istpet-gold)] transition-all outline-none shadow-inner"
                            />
                        </div>
                        <div className="space-y-2">
                            <label className="text-[9px] font-black uppercase tracking-[0.2em] text-[var(--apple-text-sub)] opacity-60">Hasta</label>
                            <input 
                                type="date" 
                                name="fechaFin"
                                value={filtros.fechaFin}
                                onChange={handleFiltroChange}
                                className="w-full bg-[var(--apple-bg)]/50 border-2 border-[var(--apple-border)] rounded-2xl px-4 py-3 text-sm font-bold text-[var(--apple-text-main)] focus:border-[var(--istpet-gold)] transition-all outline-none shadow-inner"
                            />
                        </div>
                        <div className="space-y-2">
                            <label className="text-[9px] font-black uppercase tracking-[0.2em] text-[var(--apple-text-sub)] opacity-60">Filtro Instructor</label>
                            <select 
                                name="instructorId"
                                value={filtros.instructorId}
                                onChange={handleFiltroChange}
                                className="w-full bg-[var(--apple-bg)]/50 border-2 border-[var(--apple-border)] rounded-2xl px-4 py-3 text-sm font-bold text-[var(--apple-text-main)] focus:border-[var(--istpet-gold)] transition-all outline-none shadow-inner appearance-none"
                            >
                                <option value="">TODOS LOS DOCENTES</option>
                                {instructores.map(i => (
                                    <option key={i.idInstructor} value={i.idInstructor}>{i.fullName}</option>
                                ))}
                            </select>
                        </div>
                        <button 
                            onClick={ejecutarReporte}
                            className="bg-[var(--apple-text-main)] text-[var(--apple-bg)] h-12 sm:col-span-2 xl:col-span-1 rounded-2xl font-black text-xs uppercase tracking-widest hover:opacity-90 transition-all flex items-center justify-center gap-2"
                        >
                            {loading ? (
                                <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                </svg>
                            ) : "Actualizar"}
                        </button>
                    </div>
                </div>

                <div className="apple-glass-card overflow-hidden rounded-[1.5rem] sm:rounded-[2.5rem]">
                    <div className="md:hidden p-4 space-y-3">
                        {loading && (
                            <div className="flex justify-center py-16">
                                <svg className="animate-spin h-8 w-8 text-[var(--istpet-gold)]" viewBox="0 0 24 24">
                                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                                </svg>
                            </div>
                        )}
                        {data.length > 500 && (
                            <div className="px-6 py-3 bg-[var(--istpet-gold)]/10 border-b border-[var(--istpet-gold)]/10">
                                <p className="text-[10px] font-black text-[var(--istpet-gold)] uppercase tracking-widest leading-relaxed">
                                    Limitado a los últimos 500 de {data.length} registros. Use Excel para el total.
                                </p>
                            </div>
                        )}
                        {!loading && data.length > 0 && data.slice(0, 500).map((item) => (
                            <article
                                key={item.idPractica}
                                className="rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-bg)]/30 p-4 space-y-3"
                            >
                                <div className="flex flex-wrap items-start justify-between gap-2">
                                    <div className="min-w-0 flex-1">
                                        <p className="text-[10px] font-black uppercase tracking-wider text-[var(--apple-text-sub)]">Profesor</p>
                                        <p className="text-sm font-black text-[var(--apple-text-main)] uppercase truncate">{item.profesor}</p>
                                    </div>
                                    <span className="text-[9px] font-black px-2 py-1 rounded-lg bg-[var(--apple-bg)] border border-[var(--apple-border)] text-[var(--istpet-gold)] shrink-0">
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

                    {data.length > 500 && (
                        <div className="px-6 py-3 bg-[var(--istpet-gold)]/10 border-b border-[var(--istpet-gold)]/10">
                            <p className="text-[10px] font-black text-[var(--istpet-gold)] uppercase tracking-widest leading-relaxed">
                                Mostrando 500 de {data.length} registros para optimizar carga. Use "Descargar Excel" para el reporte completo.
                            </p>
                        </div>
                    )}

                    <div className="hidden md:block reports-table-wrap overflow-x-auto custom-scrollbar">
                        <table className="reports-data-table w-full min-w-[920px] text-left border-collapse">
                            <thead>
                                <tr className="border-b border-[var(--apple-border)] bg-[var(--apple-bg)]/20">
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
                                {data.length === 0 ? (
                                    <tr>
                                        <td colSpan="12" className="py-20 text-center animate-pulse">
                                            <div className="flex flex-col items-center gap-3">
                                                <div className="w-12 h-12 rounded-full border-2 border-t-[var(--istpet-gold)] border-transparent animate-spin"></div>
                                                <p className="text-[10px] font-black uppercase tracking-[0.3em] text-[var(--apple-text-sub)] opacity-40">
                                                    {loading ? "PROCESANDO REPORTES MASIVOS..." : "NO HAY REGISTROS"}
                                                </p>
                                            </div>
                                        </td>
                                    </tr>
                                ) : (
                                    data.slice(0, 500).map((item) => (
                                        <tr key={item.idPractica} className="hover:bg-[var(--apple-bg)]/60 transition-colors group">
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-bold text-[var(--apple-text-sub)] tabular-nums">{item.idProfesor}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-[var(--apple-text-main)] uppercase">{item.profesor}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black">
                                                <span className="bg-[var(--apple-bg)] px-2 py-1 rounded border border-[var(--apple-border)] text-[var(--istpet-gold)]">
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
                                    ))
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </Layout>
    );
};

export default Reports;
