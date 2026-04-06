import React from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTheme } from '../common/ThemeContext';
import logoImg from '../../assets/logo.png';

const Sidebar = () => {
    const { theme } = useTheme();
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

    const isItemActive = (item) => {
        if (item.id === 'monitoreo') return location.pathname === '/monitoreo';
        if (location.pathname !== '/') return false;
        if (!currentTab && item.id === 'salida') return true;
        return currentTab === item.id;
    };

    return (
        <>
            {/* Sidebar Desktop */}
            <aside className="fixed left-6 top-6 bottom-6 w-72 apple-glass rounded-[3rem] p-8 hidden lg:flex flex-col z-50">
                <div className="mb-12 flex items-center gap-4 px-4">
                    <div className={`w-12 h-12 rounded-2xl flex items-center justify-center shadow-lg border border-white/10 overflow-hidden p-2 backdrop-blur-md ${theme === 'dark' ? 'bg-white/10' : 'bg-[var(--istpet-navy)]'}`}>
                        <img src={logoImg} alt="ISTPET Logo" className="w-full h-full object-contain scale-110" />
                    </div>
                    <h1 className="text-xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase text-[10px] leading-tight">ISTPET<br />ZENITH 2026</h1>
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
                                        ? (theme === 'light'
                                            ? 'bg-[var(--istpet-navy)] text-white shadow-xl shadow-slate-200'
                                            : 'bg-[var(--apple-primary)] text-white shadow-xl shadow-blue-500/20')
                                        : theme === 'light'
                                            ? 'text-[var(--istpet-gold)] hover:bg-[var(--apple-card)] hover:shadow-lg'
                                            : 'text-[var(--apple-text-sub)] hover:bg-[var(--apple-card)] hover:text-[var(--apple-text-main)] hover:shadow-lg'
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

                <div className="mt-auto pt-8 border-t border-[var(--apple-border)] px-4 text-center">
                    <div className="p-4 apple-glass rounded-3xl">
                        <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Usuario</p>
                        <p className="text-xs font-bold text-[var(--apple-text-main)] mt-1">Admin ISTPET</p>
                    </div>
                </div>
            </aside>

            {/* Bottom Tab Bar Mobile (Themed Style) */}
            <nav className="fixed bottom-0 left-0 right-0 h-16 lg:h-20 bg-[var(--apple-card)]/90 backdrop-blur-2xl border-t border-[var(--apple-border)] z-[100] flex items-center justify-around px-2 lg:hidden shadow-[0_-10px_40px_rgba(0,0,0,0.05)]">
                {menuItems.map((item) => {
                    const active = isItemActive(item);
                    return (
                        <NavLink
                            key={item.id}
                            to={item.path}
                            className={`
                                flex flex-col items-center justify-center gap-0.5 transition-all duration-300 flex-1
                                ${active
                                    ? (theme === 'light' ? 'text-[var(--istpet-navy)]' : 'text-[var(--apple-primary)]') + ' scale-105'
                                    : (theme === 'light' ? 'text-[var(--istpet-gold)]' : 'text-[var(--apple-text-sub)] hover:text-[var(--apple-text-main)]')
                                }
                            `}
                        >
                            <div className="p-0.5 px-2 rounded-xl transition-all">
                                {item.icon}
                            </div>
                            <span className={`text-[8px] font-black uppercase tracking-widest ${active ? 'opacity-100' : 'opacity-60'}`}>
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
