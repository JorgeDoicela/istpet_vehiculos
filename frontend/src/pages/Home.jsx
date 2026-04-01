import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import ActiveClasses from '../components/features/ActiveClasses';
import SkeletonLoader from '../components/features/SkeletonLoader';
import dashboardService from '../services/dashboardService';

const Home = () => {
    const [activeClasses, setActiveClasses] = useState([]);
    const [syncResult, setSyncResult] = useState(null);
    const [syncing, setSyncing] = useState(false);
    const [loading, setLoading] = useState(true);
    const [notification, setNotification] = useState(null);

    useEffect(() => {
        fetchInitialData();
    }, []);

    const fetchInitialData = async () => {
        setLoading(true);
        console.log('[DASHBOARD] Solicitando monitor de clases activas...');
        try {
            const cData = await dashboardService.getClasesActivas();
            console.log('[DASHBOARD SUCCESS] Clases recibidas:', cData?.length || 0);
            setActiveClasses(cData);
        } catch (err) {
            console.error('[DASHBOARD ERROR] Fallo al cargar monitor:', err.message);
        } finally {
            setLoading(false);
        }
    };

    const showNotification = (message, type = 'success') => {
        setNotification({ message, type });
        setTimeout(() => setNotification(null), 4000);
    };

    const handleSyncData = async () => {
        setSyncing(true);
        setSyncResult(null);
        try {
            const log = await dashboardService.syncStudents();
            setSyncResult(log);
            showNotification('Sincronización completada con éxito');
        } catch (err) {
            showNotification('Error en la conexión con la Aduana', 'error');
        } finally {
            setSyncing(false);
        }
    };

    const CircularKPI = ({ value, max, label, color = 'blue' }) => {
        const percentage = Math.min((value / max) * 100, 100);
        const radius = 36;
        const circumference = 2 * Math.PI * radius;
        const offset = circumference - (percentage / 100) * circumference;

        return (
            <div className="flex flex-col items-center">
                <div className="relative w-24 h-24">
                    <svg className="w-full h-full transform -rotate-90">
                        <circle cx="48" cy="48" r={radius} stroke="currentColor" strokeWidth="8" fill="transparent" className="text-slate-100" />
                        <circle cx="48" cy="48" r={radius} stroke="currentColor" strokeWidth="8" fill="transparent"
                            strokeDasharray={circumference} strokeDashoffset={offset} strokeLinecap="round"
                            className={`transition-all duration-1000 ${color === 'blue' ? 'text-blue-500' : 'text-emerald-500'}`} />
                    </svg>
                    <div className="absolute inset-0 flex items-center justify-center font-black text-xl text-slate-800">{Math.round(percentage)}%</div>
                </div>
                <p className="mt-3 text-[10px] font-black uppercase tracking-widest text-slate-400">{label}</p>
            </div>
        );
    };

    return (
        <Layout>
            {/* Sistema de Notificaciones Zenith */}
            {notification && (
                <div className="apple-toast border-white border animate-apple-in">
                    <div className={`w-3 h-12 rounded-full ${notification.type === 'error' ? 'bg-rose-500' : 'bg-[var(--apple-primary)]'}`}></div>
                    <p className="text-sm font-bold text-slate-800">{notification.message}</p>
                </div>
            )}

            <div className="space-y-12">
                <div className="max-w-4xl">
                    <h1 className="text-5xl font-black tracking-tighter text-slate-900 uppercase">Dashboard </h1>
                    <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-3">VERSION CORPORATIVA ZENITH 2026</p>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
                    <div className="lg:col-span-2 space-y-12 animate-apple-in">
                        {/* KPI WIDGETS */}
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                            <div className="apple-card flex flex-col items-center justify-center p-10 bg-white/40">
                                <CircularKPI value={85} max={100} label="Flota Operativa" />
                            </div>
                            <div className="apple-card flex flex-col items-center justify-center p-10 bg-white/40">
                                <CircularKPI value={activeClasses.length} max={15} label="Ocupación Live" color="emerald" />
                            </div>
                            <div className="apple-card flex flex-col items-center justify-center p-10 bg-slate-900 text-white !border-none">
                                <div className="text-center">
                                    <p className="text-[10px] font-black uppercase tracking-[0.25em] text-slate-400 mb-4">Integridad</p>
                                    <p className="text-4xl font-black">99.9%</p>
                                    <p className="text-[9px] font-bold text-emerald-400 mt-2 uppercase">Sistema Protegido</p>
                                </div>
                            </div>
                        </div>

                        {/* INTEGRITY CENTER */}
                        <div className="apple-card p-12 bg-white/60 border-2 border-white">
                            <div className="flex items-center gap-6 mb-12">
                                <div className={`p-6 bg-[var(--apple-primary)] rounded-[2.5rem] shadow-2xl text-white ${syncing ? 'animate-spin' : ''}`}>
                                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="w-10 h-10">
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0 3.181 3.183a8.25 8.25 0 0 0 13.803-3.7M4.031 9.865a8.25 8.25 0 0 1 13.803-3.7l3.181 3.182m0-4.991v4.99" />
                                    </svg>
                                </div>
                                <div>
                                    <h3 className="text-3xl font-black tracking-tighter uppercase text-slate-900">Integrity Center Pro</h3>
                                    <p className="text-[var(--apple-text-sub)] text-[10px] font-black uppercase tracking-widest mt-1">Sincronización Segura vía Aduana Digital</p>
                                </div>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 items-center">
                                <div>
                                    <p className="text-sm text-slate-500 font-bold mb-8 leading-relaxed">
                                        La sincronización masiva utiliza el protocolo de "Aduana Digital" para filtrar información externa y proteger las 11 tablas locales del sistema.
                                    </p>
                                    <button onClick={handleSyncData} disabled={syncing} className="btn-apple-primary w-full shadow-2xl">
                                        {syncing ? 'Verificando Nodo...' : 'Ejecutar Sincronización Pro'}
                                    </button>
                                </div>

                                <div className={`p-10 rounded-[3rem] border-2 transition-all duration-1000 min-h-[220px] flex flex-col justify-center ${!syncResult ? 'bg-white/30 border-dashed border-slate-300/40 opacity-50' :
                                        syncResult.estado === 'OK' ? 'bg-emerald-50/50 border-emerald-100 shadow-xl' : 'bg-amber-50/50 border-amber-100 shadow-xl'
                                    }`}>
                                    {syncing ? (
                                        <div className="space-y-4">
                                            <SkeletonLoader type="text" className="w-1/2" />
                                            <SkeletonLoader type="title" />
                                        </div>
                                    ) : !syncResult ? (
                                        <p className="text-center text-slate-400 font-black text-[10px] uppercase tracking-widest">En espera de sincronización...</p>
                                    ) : (
                                        <div className="space-y-6">
                                            <div className="flex items-center justify-between">
                                                <span className="text-[10px] font-black uppercase text-slate-400">Auditoría Transaccional</span>
                                                <span className={`px-4 py-1 rounded-full text-[9px] font-black uppercase text-white ${syncResult.estado === 'OK' ? 'bg-emerald-500' : 'bg-amber-500'
                                                    }`}>{syncResult.estado}</span>
                                            </div>
                                            <div className="grid grid-cols-2 gap-8">
                                                <div>
                                                    <p className="text-[10px] font-black uppercase text-slate-400">Éxito</p>
                                                    <p className="text-5xl font-black text-emerald-600">{syncResult.registrosProcesados}</p>
                                                </div>
                                                <div>
                                                    <p className="text-[10px] font-black uppercase text-slate-400">Auditados</p>
                                                    <p className="text-5xl font-black text-amber-500">{syncResult.registrosFallidos}</p>
                                                </div>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="animate-apple-in">
                        {loading ? (
                            <div className="apple-card space-y-6 p-10 bg-white/40">
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
