import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import ActiveClasses from '../components/features/ActiveClasses';
import SkeletonLoader from '../components/features/SkeletonLoader';
import dashboardService from '../services/dashboardService';

const Home = () => {
    const [activeClasses, setActiveClasses] = useState([]);
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
                    <h1 className="text-5xl font-black tracking-tighter text-slate-900 uppercase">Monitoreo Live </h1>
                    <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-3">SISTEMA SIMPLIFICADO DE CONTROL</p>
                </div>

                <div className="grid grid-cols-1 gap-12">
                    <div className="space-y-12 animate-apple-in">
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
