import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import LogisticaHeader from '../components/logistica/LogisticaHeader';
import ActiveClasses from '../components/features/ActiveClasses';
import SkeletonLoader from '../components/features/SkeletonLoader';
import dashboardService from '../services/dashboardService';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';

const Home = () => {
    const { isAuthorized } = useAuth();
    const [activeClasses, setActiveClasses] = useState([]);
    const [loading, setLoading] = useState(true);
    const [syncing, setSyncing] = useState(false);
    const [notification, setNotification] = useState(null);

    useEffect(() => {
        fetchInitialData();
    }, []);

    const fetchInitialData = async () => {
        setLoading(true);
        try {
            const cData = await dashboardService.getClasesActivas();
            setActiveClasses(cData);
        } catch (err) {
            console.error('[DASHBOARD ERROR]', err.message);
        } finally {
            setLoading(false);
        }
    };

    const handleSync = async () => {
        setSyncing(true);
        try {
            const response = await api.post('/sync/all');
            if (response.data.success) {
                showNotification('Sincronización SIGAFI completada con éxito');
                fetchInitialData();
            } else {
                showNotification(response.data.message || 'Error en sincronización', 'error');
            }
        } catch (err) {
            showNotification('Fallo de conexión con servicio de sincronización', 'error');
        } finally {
            setSyncing(false);
        }
    };

    const showNotification = (message, type = 'success') => {
        setNotification({ message, type });
        setTimeout(() => setNotification(null), 4000);
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

    return (
        <Layout>
            {/* Sistema de Notificaciones Zenith */}
            {notification && (
                <div className="apple-toast border border-white/10 animate-apple-in" style={{ zIndex: 1000 }}>
                    <div className={`w-3 h-12 rounded-full ${notification.type === 'error' ? 'bg-rose-500' : 'bg-[var(--apple-primary)]'}`}></div>
                    <p className="text-sm font-bold text-[var(--apple-text-main)]">{notification.message}</p>
                </div>
            )}

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
