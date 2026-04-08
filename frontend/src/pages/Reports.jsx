import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import reportService from '../services/reportService';
import logisticaService from '../services/logisticaService';
import { useTheme } from '../components/common/ThemeContext';
import * as XLSX from 'xlsx';
import { saveAs } from 'file-saver';
import './Reports.css';

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
        try {
            const res = await reportService.getReportePracticas(filtros);
            setData(res);
        } catch (error) {
            console.error('Error al generar reporte:', error);
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
                {/* Header Section */}
                <div className="reports-header mb-6 sm:mb-8">
                    <div className="flex flex-col gap-4 sm:gap-6 lg:flex-row lg:items-center lg:justify-between">
                        <div className="min-w-0">
                            <p className="text-[10px] font-black tracking-[0.3em] text-[var(--istpet-gold)] uppercase mb-1">Administración Zenith</p>
                            <h1 className="text-2xl sm:text-3xl lg:text-4xl font-black text-[var(--apple-text-main)] tracking-tighter break-words">Reporte de Prácticas</h1>
                        </div>
                        
                        <div className="flex w-full sm:w-auto shrink-0">
                            <button 
                                onClick={exportToExcel}
                                disabled={data.length === 0}
                                className="w-full sm:w-auto justify-center px-6 py-3 bg-[var(--istpet-gold)] text-white rounded-full text-xs font-black uppercase tracking-widest shadow-xl shadow-amber-500/20 hover:scale-[1.02] sm:hover:scale-[1.05] active:scale-95 transition-all flex items-center gap-2 disabled:opacity-30"
                            >
                                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                </svg>
                                Descargar Excel
                            </button>
                        </div>
                    </div>
                </div>

                {/* Filters Section */}
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
                                    <option key={i.id_Instructor} value={i.id_Instructor}>{i.fullName}</option>
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

                {/* Results: tarjetas en móvil, tabla desde md */}
                <div className="apple-glass-card overflow-hidden rounded-[1.5rem] sm:rounded-[2.5rem]">
                    {/* Vista móvil */}
                    <div className="md:hidden p-4 space-y-3">
                        {loading && (
                            <div className="flex justify-center py-16">
                                <svg className="animate-spin h-8 w-8 text-[var(--istpet-gold)]" viewBox="0 0 24 24">
                                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                                </svg>
                            </div>
                        )}
                        {!loading && data.length > 0 && data.map((item) => (
                            <article
                                key={item.idRegistro}
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
                        {!loading && data.length === 0 && (
                            <div className="py-16 text-center">
                                <div className="opacity-20 mb-3">
                                    <svg className="w-12 h-12 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                    </svg>
                                </div>
                                <p className="text-xs font-black uppercase tracking-widest text-[var(--apple-text-sub)]">No hay registros para este criterio</p>
                            </div>
                        )}
                    </div>

                    {/* Vista tabla (tablet/desktop) */}
                    <div className="hidden md:block reports-table-wrap overflow-x-auto custom-scrollbar">
                        <p className="xl:hidden px-4 pt-3 pb-1 text-[10px] text-[var(--apple-text-sub)] text-center opacity-70">
                            Desliza horizontalmente para ver todas las columnas
                        </p>
                        <table className="reports-data-table w-full min-w-[920px] text-left border-collapse">
                            <thead>
                                <tr className="border-b border-[var(--apple-border)] bg-[var(--apple-bg)]/20">
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">ID Prof</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Profesor</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Categoría</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]"># Veh</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">ID Alumno</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Nómina</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Semana</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Fecha</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Salida</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Llegada</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Tiempo</th>
                                    <th className="px-3 lg:px-5 py-3 lg:py-4 text-[9px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Estado</th>
                                </tr>
                            </thead>
                            <tbody>
                                {data.length > 0 ? (
                                    data.map((item) => (
                                        <tr key={item.idRegistro} className="border-b border-[var(--apple-border)] hover:bg-[var(--apple-primary)]/5 transition-colors group">
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[10px] font-bold text-[var(--apple-text-sub)] font-mono">{item.idProfesor}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-[var(--apple-text-main)] uppercase">{item.profesor}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4">
                                                <span className="text-[9px] font-black px-2 py-0.5 rounded-md bg-[var(--apple-bg)] border border-[var(--apple-border)] text-[var(--istpet-gold)]">
                                                    {item.categoria}
                                                </span>
                                            </td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-center">
                                                <div className="w-8 h-8 rounded-lg bg-[var(--apple-text-main)] text-[var(--apple-bg)] flex items-center justify-center font-black text-xs mx-auto">
                                                    {item.numeroVehiculo}
                                                </div>
                                            </td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[10px] font-bold text-[var(--apple-text-sub)] font-mono">{item.idAlumno}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-[var(--apple-text-main)] uppercase">{item.nomina}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[10px] font-black text-[var(--apple-text-sub)] uppercase italic">{item.dia}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[10px] font-bold text-[var(--apple-text-main)] tabular-nums">{item.fecha}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-emerald-600 tabular-nums">{item.horaSalida}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-rose-500 tabular-nums">{item.horaLlegada || '--:--:--'}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4 text-[11px] font-black text-[var(--apple-text-main)] tabular-nums">{item.tiempo}</td>
                                            <td className="px-3 lg:px-5 py-3 lg:py-4">
                                                {item.horaLlegada ? (
                                                    <span className="text-[8px] font-black bg-emerald-500 text-white px-2 py-1 rounded-full uppercase tracking-tighter">COMPLETADA</span>
                                                ) : (
                                                    <span className="text-[8px] font-black bg-amber-500 text-white px-2 py-1 rounded-full uppercase tracking-tighter animate-pulse">EN PISTA</span>
                                                )}
                                            </td>
                                        </tr>
                                    ))
                                ) : !loading && (
                                    <tr>
                                        <td colSpan="12" className="px-5 py-20 text-center">
                                            <div className="opacity-20 mb-3">
                                                <svg className="w-12 h-12 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                                </svg>
                                            </div>
                                            <p className="text-xs font-black uppercase tracking-widest text-[var(--apple-text-sub)]">No hay registros para este criterio</p>
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>

                <div className="mt-8 text-center opacity-40">
                    <p className="text-[9px] font-black uppercase tracking-[0.4em] text-[var(--apple-text-sub)]">Sistema de Gestión Logística Zenith v2026.1</p>
                </div>
            </div>
        </Layout>
    );
};

export default Reports;
