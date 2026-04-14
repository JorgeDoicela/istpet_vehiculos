import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import ActiveClasses from '../components/features/ActiveClasses';
import SkeletonLoader from '../components/features/SkeletonLoader';
import dashboardService from '../services/dashboardService';
import { fmtDuracionSalidaLlegada, fmtHoraPractica } from '../utils/agendaUi';
import { useOperativeAlerts } from '../context/OperativeAlertsContext';

const Home = () => {
    const { publishClasesActivas } = useOperativeAlerts();
    const [activeClasses, setActiveClasses] = useState([]);
    const [completedPack, setCompletedPack] = useState({ practicas: [], fuenteDatos: '', obtenidoEn: null });
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchInitialData();
    }, []);

    useEffect(() => {
        publishClasesActivas(activeClasses);
        return () => publishClasesActivas([]);
    }, [activeClasses, publishClasesActivas]);

    const fetchInitialData = async () => {
        setLoading(true);
        try {
            const cData = await dashboardService.getClasesActivas();
            setActiveClasses(cData);

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

    return (
        <Layout>

            <div className="space-y-8">
                <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 px-2">
                    <div>
                        <p className="text-[10px] lg:text-xs font-black text-[var(--istpet-gold)] uppercase tracking-[0.2em] mb-0">
                            Visión de flota en tiempo real
                        </p>
                        <h1 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase leading-tight">
                            Pista &amp; Monitoreo
                        </h1>
                    </div>

                </div>

                <div className="grid grid-cols-1 gap-8">
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
                            <ul className="space-y-2.5">
                                {completedPack.practicas.map((ag) => {
                                    const duracion = fmtDuracionSalidaLlegada(ag.hora_salida, ag.SigafiHoraLlegada);
                                    return (
                                        <li
                                            key={ag.idPractica}
                                            className="rounded-2xl border border-[var(--apple-border)] bg-[var(--apple-card)] px-4 py-3 hover:border-emerald-500/30 transition-colors space-y-2"
                                        >
                                            {/* Fila 1: nombre */}
                                            <p className="text-sm font-black text-[var(--apple-text-main)] uppercase leading-snug line-clamp-1">
                                                {ag.AlumnoNombre}
                                            </p>

                                            {/* Fila 2: vehículo + instructor (izq) | salida → llegada (der) */}
                                            <div className="flex items-center justify-between gap-4">
                                                <div className="min-w-0 flex-1 space-y-1">
                                                    <div className="flex items-center gap-1.5 min-w-0">
                                                        <svg className="h-3 w-3 shrink-0 text-[var(--istpet-gold)]" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
                                                            <path d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z" />
                                                        </svg>
                                                        <span className="text-[10px] font-black text-[var(--apple-text-main)] uppercase tracking-tight truncate">{ag.VehiculoDetalle}</span>
                                                    </div>
                                                    <div className="flex items-center gap-1 min-w-0">
                                                        <span className="text-[7px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-50 shrink-0">Instructor</span>
                                                        <span className="text-[9px] font-bold text-[var(--apple-text-main)] truncate">{ag.ProfesorNombre}</span>
                                                    </div>
                                                </div>

                                                {/* Tiempos apilados */}
                                                <div className="shrink-0 flex flex-col items-end gap-0.5 tabular-nums">
                                                    <div className="flex items-baseline gap-1.5">
                                                        <span className="text-[7px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-50">Salida</span>
                                                        <span className="text-xs font-black text-[var(--apple-text-main)]">{fmtHoraPractica(ag.hora_salida)}</span>
                                                    </div>
                                                    <div className="flex items-baseline gap-1.5">
                                                        <span className="text-[7px] font-black uppercase tracking-widest text-emerald-600 opacity-80">Llegada</span>
                                                        <span className="text-xs font-black text-emerald-600">{fmtHoraPractica(ag.SigafiHoraLlegada)}</span>
                                                    </div>
                                                    {duracion ? (
                                                        <div className="flex items-baseline gap-1.5">
                                                            <span className="text-[7px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] opacity-50">En pista</span>
                                                            <span className="text-xs font-black text-[var(--apple-text-sub)]">{duracion}</span>
                                                        </div>
                                                    ) : null}
                                                </div>
                                            </div>

                                            {/* Fila 3: meta */}
                                            <div className="flex flex-wrap items-center gap-x-2.5 text-[9px] font-bold tabular-nums text-[var(--apple-text-sub)] opacity-65">
                                                {ag.idalumno ? <span>{String(ag.idalumno).trim()}</span> : null}
                                                {ag.idPractica != null ? <span>#{ag.idPractica}</span> : null}
                                                {ag.idPeriodo ? <span>{ag.idPeriodo}</span> : null}
                                            </div>
                                        </li>
                                    );
                                })}
                            </ul>
                        </div>
                    )}
                </div>
            </div>
        </Layout>
    );
};

export default Home;
