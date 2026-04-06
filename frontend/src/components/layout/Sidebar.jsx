import React from 'react';
import { NavLink } from 'react-router-dom';

const Sidebar = () => {
    const menuItems = [
        {
            path: '/',
            name: 'Control Operativo',
            icon: (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-6 h-6">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v12m-3-2.818l.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
            )
        },
        {
            path: '/monitoreo',
            name: 'Monitoreo en Pista',
            icon: (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-6 h-6">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6A2.25 2.25 0 016 3.75h2.25A2.25 2.25 0 0110.5 6v2.25a2.25 2.25 0 01-2.25 2.25H6a2.25 2.25 0 01-2.25-2.25V6zM3.75 15.75A2.25 2.25 0 016 13.5h2.25a2.25 2.25 0 012.25 2.25V18a2.25 2.25 0 01-2.25 2.25H6A2.25 2.25 0 013.75 18v-2.25zM13.5 6a2.25 2.25 0 012.25-2.25H18A2.25 2.25 0 0120.25 6v2.25A2.25 2.25 0 0118 10.5h-2.25a2.25 2.25 0 01-2.25-2.25V6zM13.5 15.75a2.25 2.25 0 012.25-2.25H18a2.25 2.25 0 012.25 2.25V18a2.25 2.25 0 01-2.25 2.25H18a2.25 2.25 0 01-2.25-2.25v-2.25z" />
                </svg>
            )
        }
        // Nota: Los módulos de 'Estudiantes' y 'Flota ISTPET' se han ocultado por directiva
        // operativa, para reducir distracciones visuales. Los endpoints y vistas siguen activos si se navega a /estudiantes
    ];

    return (
        <aside className="fixed left-6 top-6 bottom-6 w-72 apple-glass rounded-[3rem] p-8 flex flex-col z-50">
            <div className="mb-12 flex items-center gap-4 px-4">
                <div className="w-12 h-12 bg-white rounded-2xl flex items-center justify-center shadow-lg border border-slate-100">
                    <span className="text-xl font-black text-slate-900 tracking-tighter">I</span>
                </div>
                <h1 className="text-xl font-black text-slate-800 tracking-tighter">ISTPET AIR</h1>
            </div>

            <nav className="flex-1 space-y-4">
                {menuItems.map((item) => (
                    <NavLink
                        key={item.path}
                        to={item.path}
                        className={({ isActive }) => `
              flex items-center gap-5 px-6 py-4 rounded-2xl transition-all duration-500 group
              ${isActive
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
                ))}
            </nav>

            <div className="mt-auto pt-8 border-t border-white/20 px-4">
                <div className="p-4 bg-white/40 rounded-3xl backdrop-blur-md">
                    <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Usuario</p>
                    <p className="text-xs font-bold text-slate-800 mt-1">Admin ISTPET 2026</p>
                </div>
            </div>
        </aside>
    );
};

export default Sidebar;
