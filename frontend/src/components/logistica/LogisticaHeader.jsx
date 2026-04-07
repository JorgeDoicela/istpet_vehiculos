import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

const LogisticaHeader = ({ activeTab, title = "Logística Operativa" }) => {
    const navigate = useNavigate();
    const location = useLocation();

    // Determinar qué botón está activo basado en la ruta y el prop activeTab
    const currentPath = location.pathname;
    
    // Si estamos en /monitoreo, el activo es 'monitoreo'
    // Si estamos en /, depende del prop activeTab ('salida' o 'llegada')
    const view = currentPath === '/monitoreo' ? 'monitoreo' : activeTab;

    const handleNavigation = (target) => {
        if (target === 'monitoreo') {
            navigate('/monitoreo');
        } else {
            // Navegar a la raíz con el parámetro de tab correspondiente
            navigate(`/?tab=${target}`);
        }
    };

    return (
        <div className="mb-10 lg:mb-16 animate-apple-in" style={{ animationDelay: '0.1s' }}>
            {/* Título Principal Gestión Conducción */}
            <div className="mb-8 lg:mb-12 text-center">
                <h1 className="text-4xl lg:text-6xl font-black tracking-tighter text-slate-900 mb-2 lg:mb-4 bg-clip-text text-transparent bg-gradient-to-b from-slate-900 to-slate-600">
                    {title}
                </h1>
                <p className="text-slate-500 font-medium text-sm lg:text-xl tracking-tight opacity-70">
                    Control de flota y gestión académica en tiempo real
                </p>
            </div>

            {/* Selector de 3 Botones (Unified Nav) */}
            <div className="flex justify-center">
                <div className="bg-slate-200/50 backdrop-blur-2xl p-2 rounded-[2.5rem] flex gap-2 border border-white/40 shadow-2xl w-full max-w-sm lg:max-w-2xl relative">
                    {/* Botón SALIDA */}
                    <button
                        onClick={() => handleNavigation('salida')}
                        className={`flex-1 py-4 px-4 sm:px-8 rounded-[2rem] text-[10px] sm:text-xs font-black uppercase tracking-[0.2em] transition-all duration-500 flex flex-col items-center gap-1.5 ${view === 'salida' ? 'bg-white text-[var(--istpet-navy)] shadow-xl scale-105 border border-[var(--istpet-gold)]/20' : 'text-slate-500 hover:text-slate-700 hover:bg-white/40 opacity-70'}`}>
                        <svg className="w-4 h-4 sm:w-5 sm:h-5 opacity-80" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M12 19V5m0 0l-7 7m7-7l7 7" /></svg>
                        <span>Salida</span>
                    </button>

                    {/* Botón LLEGADA */}
                    <button
                        onClick={() => handleNavigation('llegada')}
                        className={`flex-1 py-4 px-4 sm:px-8 rounded-[2rem] text-[10px] sm:text-xs font-black uppercase tracking-[0.2em] transition-all duration-500 flex flex-col items-center gap-1.5 ${view === 'llegada' ? 'bg-white text-[var(--istpet-gold)] shadow-xl scale-105 border border-[var(--istpet-gold)]/20' : 'text-slate-500 hover:text-slate-700 hover:bg-white/40 opacity-70'}`}>
                        <svg className="w-4 h-4 sm:w-5 sm:h-5 opacity-80" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M12 5v14m0 0l7-7m-7 7l-7-7" /></svg>
                        <span>Llegada</span>
                    </button>

                    {/* Botón MONITOREO */}
                    <button
                        onClick={() => handleNavigation('monitoreo')}
                        className={`flex-1 py-4 px-4 sm:px-8 rounded-[2rem] text-[10px] sm:text-xs font-black uppercase tracking-[0.2em] transition-all duration-500 flex flex-col items-center gap-1.5 ${view === 'monitoreo' ? 'bg-slate-900 text-white shadow-xl scale-105' : 'text-slate-500 hover:text-slate-700 hover:bg-white/40 opacity-70'}`}>
                        <svg className="w-4 h-4 sm:w-5 sm:h-5 opacity-80" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" /></svg>
                        <span>Monitoreo</span>
                    </button>
                </div>
            </div>
        </div>
    );
};

export default LogisticaHeader;
