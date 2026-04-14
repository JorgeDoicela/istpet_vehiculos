import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import ActiveClasses from '../components/features/ActiveClasses';
import SkeletonLoader from '../components/features/SkeletonLoader';
import dashboardService from '../services/dashboardService';
import { fmtTimeSpan } from '../utils/agendaUi';

const Home = () => {
    const [activeClasses, setActiveClasses] = useState([]);
    const [completedPack, setCompletedPack] = useState({ practicas: [], fuenteDatos: '', obtenidoEn: null });
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchInitialData();
    }, []);

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
                        <h1 className="text-3xl lg:text-5xl font-black tracking-tighter text-[var(--apple-text-main)] uppercase bg-clip-text text-transparent bg-gradient-to-b from-[var(--apple-text-main)] to-[var(--apple-text-sub)]">
                            Pista & Monitoreo
                        </h1>
                        <p className="text-[var(--apple-text-sub)] font-bold text-[10px] lg:text-sm uppercase tracking-widest opacity-70 mt-1">Visión de flota en tiempo real</p>
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
                </div>
            </div>
        </Layout>
    );
};

export default Home;
