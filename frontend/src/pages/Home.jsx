import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import Layout from '../components/layout/Layout';
import ActiveClasses from '../components/features/ActiveClasses';
import SkeletonLoader from '../components/features/SkeletonLoader';
import dashboardService from '../services/dashboardService';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { fmtFechaAgenda, fmtUltimaCargaAgenda, estadoAgendaChip, fmtTimeSpan } from '../utils/agendaUi';

const CircularKPI = ({ value, max, label, color = 'blue' }) => {
    const percentage = Math.min((value / max) * 100, 100);
    const radius = 36;
    const circumference = 2 * Math.PI * radius;
    const offset = circumference - (percentage / 100) * circumference;

    return (
        <div className="flex flex-col items-center">
            <div className="relative w-24 h-24">
                <svg className="w-full h-full transform -rotate-90">
                    <circle cx="48" cy="48" r={radius} stroke="currentColor" strokeWidth="8" fill="transparent" className="text-[var(--apple-border)]" />
                    <circle cx="48" cy="48" r={radius} stroke="currentColor" strokeWidth="8" fill="transparent"
                        strokeDasharray={circumference} strokeDashoffset={offset} strokeLinecap="round"
                        className={`transition-all duration-1000 ${color === 'blue' ? 'text-[var(--apple-primary)]' : 'text-emerald-500'}`} />
                </svg>
                <div className="absolute inset-0 flex items-center justify-center font-black text-xl text-[var(--apple-text-main)]">{Math.round(percentage)}%</div>
            </div>
            <p className="mt-3 text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">{label}</p>
        </div>
    );
};

const Home = () => {
    const { isAuthorized } = useAuth();
    const { success: toastSuccess, error: toastError } = useToast();
    const [activeClasses, setActiveClasses] = useState([]);
    const [agendaPack, setAgendaPack] = useState({ practicas: [], fuenteDatos: '', obtenidoEn: null });
    const [completedPack, setCompletedPack] = useState({ practicas: [], fuenteDatos: '', obtenidoEn: null });
    const [loading, setLoading] = useState(true);
    const [syncing, setSyncing] = useState(false);

    useEffect(() => {
        fetchInitialData();
    }, []);

    const fetchInitialData = async () => {
        setLoading(true);
        try {
            const cData = await dashboardService.getClasesActivas();
            setActiveClasses(cData);
            try {
                const aPack = await dashboardService.getAgendaReciente(10);
                setAgendaPack(aPack);
            } catch (agErr) {
                console.warn('[AGENDA]', agErr?.message || agErr);
            }

            try {
                const cPack = await dashboardService.getHistorialHoy(10);
                setCompletedPack(cPack);
            } catch (hErr) {
                console.warn('[HISTORIAL]', hErr?.message || hErr);
            }
        } catch (err) {
            console.error('[DASHBOARD ERROR]', err.message);
        } finally {
            setLoading(false);
        }
    };

    const handleSync = async () => {
        setSyncing(true);
        try {
            const response = await api.post('/Sync/master');
            const ok = response?.data?.success ?? response?.data?.Success;
            const msg = response?.data?.message ?? response?.data?.Message;
            if (ok) {
                toastSuccess('Sincronización SIGAFI completada con éxito');
                fetchInitialData();
            } else {
                toastError(msg || 'Error en sincronización');
            }
        } catch (err) {
            toastError('Fallo de conexión con el servicio de sincronización');
        } finally {
            setSyncing(false);
        }
    };

    return (
        <Layout>

            <div className="space-y-8">
                <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 px-2">
                    <div>
                        <h1 className="text-3xl lg:text-5xl font-black tracking-tighter text-[var(--apple-text-main)] uppercase bg-clip-text text-transparent bg-gradient-to-b from-[var(--apple-text-main)] to-[var(--apple-text-sub)]">
                            Pista & Monitoreo
                        </h1>
                        <p className="text-[var(--apple-text-sub)] font-bold text-[10px] lg:text-sm uppercase tracking-widest opacity-70 mt-1">Visión global de flota en tiempo real</p>
                    </div>

                    {/* Botón de Sincronización solo para Admin */}
                    {isAuthorized(['admin']) && (
                        <button
                            onClick={handleSync}
                            disabled={syncing}
                            className={`flex items-center gap-3 px-6 py-3 rounded-2xl font-black uppercase tracking-[0.15em] text-[10px] transition-all duration-500 shadow-lg ${syncing ? 'bg-[var(--apple-border)] text-[var(--apple-text-sub)] cursor-wait' : 'bg-[var(--istpet-gold)] text-white hover:scale-105 active:scale-95 shadow-amber-500/20'}`}
                        >
                            {syncing ? (
                                <>
                                    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                                    Sincronizando...
                                </>
                            ) : (
                                <>
                                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={3} stroke="currentColor" className="w-4 h-4">
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99" />
                                    </svg>
                                    Sync SIGAFI
                                </>
                            )}
                        </button>
                    )}
                </div>

                <div className="grid grid-cols-1 gap-8">
                    <div className="animate-apple-in">
                        {/* KPI WIDGETS COMPACTOS */}
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 lg:gap-6">
                            <div className="apple-card flex flex-col items-center justify-center p-6 lg:p-8 bg-[var(--apple-card)]">
                                <CircularKPI value={85} max={100} label="Flota Operativa" />
                            </div>
                            <div className="apple-card flex flex-col items-center justify-center p-6 lg:p-8 bg-[var(--apple-card)]">
                                <CircularKPI value={activeClasses.length} max={15} label="Ocupación Live" color="emerald" />
                            </div>
                            <div className="apple-card flex flex-col items-center justify-center p-6 lg:p-8 bg-[var(--istpet-navy)] text-white !border-none relative overflow-hidden group shadow-xl">
                                <div className="absolute top-0 right-0 w-24 h-24 bg-white/5 rounded-full blur-2xl translate-x-12 -translate-y-12 group-hover:bg-white/10 transition-all duration-700"></div>
                                <div className="text-center relative z-10">
                                    <p className="text-[8px] font-black uppercase tracking-[0.2em] text-white/40 mb-2">Integridad</p>
                                    <p className="text-3xl font-black">99.9%</p>
                                    <p className="text-[8px] font-bold text-emerald-400 mt-1 uppercase">Protegido</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    {!loading && agendaPack.practicas.length > 0 && (
                        <div className="apple-card p-6 lg:p-8 bg-[var(--apple-card)] border border-[var(--apple-border)] animate-apple-in">
                            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-5">
                                <div>
                                    <h2 className="text-lg font-black uppercase tracking-tight text-[var(--apple-text-main)]">Agenda de hoy (Pendientes)</h2>
                                    <p className="text-[9px] font-bold text-[var(--apple-text-sub)] uppercase tracking-wider mt-1">
                                        {agendaPack.fuenteDatos === 'local' ? 'Espejo local' : 'SIGAFI'}
                                        {agendaPack.obtenidoEn ? ` · ${fmtUltimaCargaAgenda(agendaPack.obtenidoEn)}` : ''}
                                    </p>
                                </div>
                                <Link
                                    to="/control-operativo?tab=salida"
                                    className="text-center sm:text-right text-[10px] font-black uppercase tracking-widest text-[var(--apple-primary)] hover:underline"
                                >
                                    Abrir control operativo →
                                </Link>
                            </div>
                            <ul className="space-y-3">
                                {agendaPack.practicas.map((ag) => {
                                    const chip = estadoAgendaChip(ag.estadoOperativo);
                                    return (
                                        <li
                                            key={ag.idPractica || ag.idAsignacionHorario}
                                            className="flex flex-wrap items-center gap-3 justify-between rounded-2xl border border-[var(--apple-border)]/60 bg-[var(--apple-bg)]/50 px-4 py-3"
                                        >
                                            <div className="min-w-0 flex-1">
                                                <p className="text-sm font-black text-[var(--apple-text-main)] uppercase truncate">{ag.AlumnoNombre || ag.idalumno}</p>
                                                <p className="text-[10px] font-bold text-[var(--apple-text-sub)] mt-1">
                                                    {fmtFechaAgenda(ag.fecha)} · {fmtTimeSpan(ag.hora_salida)} · {ag.VehiculoDetalle}
                                                </p>
                                            </div>
                                            <span className={`text-[8px] font-black uppercase px-2.5 py-1 rounded-full shrink-0 ${chip.cls}`}>{chip.label}</span>
                                        </li>
                                    );
                                })}
                            </ul>
                        </div>
                    )}

                    {!loading && completedPack.practicas.length > 0 && (
                        <div className="apple-card p-6 lg:p-8 bg-[var(--apple-card)] border border-[var(--apple-border)] animate-apple-in">
                            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-5">
                                <div>
                                    <h2 className="text-lg font-black uppercase tracking-tight text-[var(--apple-text-main)]">Retornos Recientes</h2>
                                    <p className="text-[9px] font-bold text-[var(--apple-text-sub)] uppercase tracking-wider mt-1">
                                        Estudiantes que ya completaron sus prácticas hoy
                                    </p>
                                </div>
                            </div>
                            <ul className="space-y-3 font-semibold">
                                {completedPack.practicas.map((ag) => (
                                    <li
                                        key={ag.idPractica}
                                        className="flex items-center gap-3 justify-between rounded-2xl border border-emerald-500/20 bg-emerald-50/5 px-4 py-3"
                                    >
                                        <div className="min-w-0 flex-1">
                                            <p className="text-sm font-black text-[var(--apple-text-main)] uppercase truncate">{ag.AlumnoNombre}</p>
                                            <p className="text-[10px] font-bold text-[var(--apple-text-sub)] mt-1">
                                                {ag.VehiculoDetalle} · {ag.ProfesorNombre}
                                            </p>
                                        </div>
                                        <div className="text-right shrink-0">
                                            <p className="text-[10px] font-black text-emerald-500 uppercase">Completado</p>
                                            <p className="text-[9px] font-bold text-[var(--apple-text-sub)] mt-0.5">{fmtTimeSpan(ag.SigafiHoraLlegada)}</p>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    <div className="animate-apple-in">
                        {loading ? (
                            <div className="apple-card space-y-6 p-10 bg-[var(--apple-card)]">
                                <SkeletonLoader type="title" />
                                <SkeletonLoader type="card" />
                                <SkeletonLoader type="card" />
                            </div>
                        ) : (
                            <ActiveClasses classes={activeClasses} />
                        )}
                    </div>
                </div>
            </div>
        </Layout>
    );
};

export default Home;
