import React from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTheme } from '../common/ThemeContext';
import { useAuth } from '../../context/AuthContext';
import logoArriba from '../../assets/logo_arriba.png';

const Sidebar = () => {
    const { theme } = useTheme();
    const { user, logout, isAuthorized } = useAuth();
    const location = useLocation();
    const searchParams = new URLSearchParams(location.search);
    const currentTab = searchParams.get('tab');

    const menuItems = [
        {
            id: 'salida',
            path: '/?tab=salida',
            name: 'Salida',
            roles: ['admin', 'logistica', 'guardia'],
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
            roles: ['admin', 'logistica', 'guardia'],
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
            roles: ['admin', 'logistica', 'guardia'],
            icon: (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
            )
        },
        {
            id: 'reportes',
            path: '/reportes',
            name: 'Reportes',
            roles: ['admin'],
            icon: (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                </svg>
            )
        }
    ];

    const filteredItems = menuItems.filter(item => isAuthorized(item.roles));

    const isItemActive = (item) => {
        if (item.id === 'monitoreo') return location.pathname === '/monitoreo';
        if (item.id === 'reportes') return location.pathname === '/reportes';
        if (location.pathname !== '/') return false;
        if (!currentTab && item.id === 'salida') return true;
        return currentTab === item.id;
    };

    return (
        <>
            <aside className="fixed left-6 top-6 bottom-6 w-72 apple-glass rounded-[3rem] p-8 hidden lg:flex flex-col z-50">
                <header className="mb-12 px-2">
                    <img 
                        src={logoArriba} 
                        alt="ISTPET Gestión" 
                        className="w-full h-auto max-h-20 object-contain mx-auto drop-shadow-xl animate-apple-in" 
                    />
                </header>

                <nav className="flex-1 space-y-4">
                    {filteredItems.map((item) => {
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

                <div className="mt-auto space-y-4">
                    <div className="p-5 apple-card-inner rounded-3xl border border-[var(--apple-border)] bg-[var(--apple-bg)]/50 backdrop-blur-md">
                        <div className="flex items-center gap-3 mb-3">
                            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-[var(--istpet-gold)] to-amber-600 flex items-center justify-center text-white font-black text-sm shadow-md">
                                {user?.nombre?.substring(0, 1) || 'U'}
                            </div>
                            <div className="min-w-0">
                                <p className="text-xs font-black text-[var(--apple-text-main)] truncate tracking-tight">{user?.nombre || 'Usuario'}</p>
                                <p className="text-[9px] font-black uppercase text-[var(--istpet-gold)] tracking-widest">{user?.rol || 'Rol'}</p>
                            </div>
                        </div>
                        
                        <button 
                            onClick={logout}
                            className="w-full py-2.5 rounded-xl bg-rose-500/10 hover:bg-rose-500 text-rose-500 hover:text-white text-[10px] font-black uppercase tracking-[0.2em] transition-all duration-300 border border-rose-500/20"
                        >
                            Cerrar Sesión
                        </button>
                    </div>
                </div>
            </aside>

            {/* Mobile Tab Bar */}
            <nav className="fixed bottom-0 left-0 right-0 h-16 lg:h-20 bg-[var(--apple-card)]/90 backdrop-blur-2xl border-t border-[var(--apple-border)] z-[100] flex items-center justify-around px-2 lg:hidden shadow-[0_-10px_40px_rgba(0,0,0,0.05)]">
                {filteredItems.map((item) => {
                    const active = isItemActive(item);
                    return (
                        <NavLink
                            key={item.id}
                            to={item.path}
                            className={`flex flex-col items-center justify-center gap-0.5 transition-all duration-300 flex-1 ${active ? 'text-[var(--istpet-gold)] scale-110' : 'text-slate-400 opacity-70'}`}
                        >
                            <div className="p-0.5 px-2 rounded-xl">
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
