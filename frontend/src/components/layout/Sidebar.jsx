import React from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import logoImg from '../../assets/logo.png';

const Sidebar = () => {
    const location = useLocation();
    const searchParams = new URLSearchParams(location.search);
    const currentTab = searchParams.get('tab');

    const menuItems = [
        {
            id: 'salida',
            path: '/?tab=salida',
            name: 'Salida',
            icon: (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 19V5m0 0l-7 7m7-7l7 7" />
                </svg>
            )
        },
        {
            id: 'llegada',
            path: '/?tab=llegada',
            name: 'Llegada',
            icon: (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 5v14m0 0l7-7m-7 7l-7-7" />
                </svg>
            )
        },
        {
            id: 'monitoreo',
            path: '/monitoreo',
            name: 'Monitoreo',
            icon: (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
            )
        }
    ];

    // Función para determinar si un item está activo (especialmente para los tabs)
    const isItemActive = (item) => {
        if (item.id === 'monitoreo') return location.pathname === '/monitoreo';
        if (location.pathname !== '/') return false;
        if (!currentTab && item.id === 'salida') return true; // Default tab
        return currentTab === item.id;
    };

    return (
        <>
            {/* Sidebar Desktop */}
            <aside className="fixed left-6 top-6 bottom-6 w-72 apple-glass rounded-[3rem] p-8 hidden lg:flex flex-col z-50">
                <div className="mb-12 flex items-center gap-4 px-4">
                    <div className="w-12 h-12 bg-white rounded-2xl flex items-center justify-center shadow-lg border border-slate-100 overflow-hidden p-2">
                        <img src={logoImg} alt="ISTPET Logo" className="w-full h-full object-contain scale-110" />
                    </div>
                    <h1 className="text-xl font-black text-slate-800 tracking-tighter uppercase text-[10px] leading-tight">ISTPET<br />ZENITH 2026</h1>
                </div>

                <nav className="flex-1 space-y-4">
                    {menuItems.map((item) => {
                        const active = isItemActive(item);
                        return (
                            <NavLink
                                key={item.id}
                                to={item.path}
                                className={`
                                    flex items-center gap-5 px-6 py-4 rounded-2xl transition-all duration-500 group
                                    ${active
                                        ? 'bg-[var(--apple-primary)] text-white shadow-xl shadow-blue-500/20'
                                        : 'text-[var(--apple-text-sub)] hover:bg-white hover:text-[var(--apple-text-main)] hover:shadow-lg'
                                    }
                                `}
                            >
                                <span className="transition-transform duration-500 group-hover:scale-110">
                                    {item.icon}
                                </span>
                                <span className="text-sm font-bold tracking-tight">{item.name}</span>
                            </NavLink>
                        );
                    })}
                </nav>

                <div className="mt-auto pt-8 border-t border-white/20 px-4">
                    <div className="p-4 bg-white/40 rounded-3xl backdrop-blur-md">
                        <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Usuario</p>
                        <p className="text-xs font-bold text-slate-800 mt-1">Admin ISTPET</p>
                    </div>
                </div>
            </aside>

            {/* Bottom Tab Bar Mobile (Minimalist White Style) */}
            <nav className="fixed bottom-0 left-0 right-0 h-20 bg-white/95 backdrop-blur-xl border-t border-slate-100 z-[100] flex items-center justify-around px-4 lg:hidden shadow-[0_-10px_40px_rgba(0,0,0,0.03)]">
                {menuItems.map((item) => {
                    const active = isItemActive(item);
                    return (
                        <NavLink
                            key={item.id}
                            to={item.path}
                            className={`
                                flex flex-col items-center justify-center gap-1.5 transition-all duration-300 flex-1
                                ${active ? 'text-blue-600 scale-110' : 'text-slate-400 hover:text-slate-600'}
                            `}
                        >
                            <div className="p-1 px-3 rounded-xl transition-all">
                                {item.icon}
                            </div>
                            <span className={`text-[9px] font-black uppercase tracking-[0.2em] ${active ? 'opacity-100' : 'opacity-40'}`}>
                                {item.name}
                            </span>
                        </NavLink>
                    );
                })}
            </nav>
        </>
    );
};

export default Sidebar;
