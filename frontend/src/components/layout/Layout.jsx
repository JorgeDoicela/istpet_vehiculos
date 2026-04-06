import React from 'react';
import Sidebar from './Sidebar';
import { useTheme } from '../common/ThemeContext';
import logoImg from '../../assets/logo.png';

const Layout = ({ children }) => {
    const { theme, toggleTheme } = useTheme();
    return (
        <div className="flex min-h-screen">
            {/* Sidebar Flotante */}
            <Sidebar />

            {/* Área de Contenido Principal */}
            <main className="flex-1 lg:ml-80 p-4 lg:p-12 transition-all duration-700 animate-apple-in">
                <header className="mb-8 lg:mb-16 flex items-center justify-between">
                    <div className="flex items-center gap-6">
                        <div className={`
                            p-3 rounded-2xl shadow-xl border flex items-center justify-center group overflow-hidden relative transition-all duration-500
                            ${theme === 'dark' ? 'bg-slate-800/50 border-white/10' : 'bg-[var(--istpet-navy)] border-white/10'}
                        `}>
                            <div className="absolute inset-0 bg-gradient-to-tr from-white/5 to-transparent pointer-events-none"></div>
                            <img src={logoImg} alt="Logo ISTPET" className="w-14 h-14 object-contain transition-transform duration-500 group-hover:scale-110" />
                        </div>
                        <div className="flex flex-col">
                            <h2 className="text-xl lg:text-3xl font-black tracking-tighter text-[var(--istpet-navy)] leading-none mb-1">
                                Gestión Conducción <span className="text-[var(--istpet-gold)] font-black">ISTPET</span>
                            </h2>
                            <p className="text-[var(--apple-text-sub)] font-bold text-[9px] uppercase tracking-[0.25em] opacity-60">Dirección de Logística y Flota</p>
                        </div>
                    </div>

                    <div className="flex items-center gap-4 lg:gap-6">
                        {/* Theme Toggle Button */}
                        <button
                            onClick={toggleTheme}
                            className="w-12 h-12 rounded-2xl apple-glass flex items-center justify-center text-[var(--istpet-gold)] hover:scale-110 transition-all duration-500 shadow-xl"
                            title={theme === 'light' ? 'Cambiar a Modo Oscuro' : 'Cambiar a Modo Claro'}
                        >
                            {theme === 'light' ? (
                                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z" />
                                </svg>
                            ) : (
                                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z" />
                                </svg>
                            )}
                        </button>

                        <button className="relative w-12 h-12 rounded-2xl apple-glass flex items-center justify-center text-[var(--apple-text-sub)] hover:text-[var(--apple-primary)] transition-all duration-500 shadow-xl group">
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6 transition-transform group-hover:rotate-12">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                            </svg>
                        </button>

                        <div className={`w-12 h-12 rounded-3xl shadow-xl border-4 border-white transition-all duration-700 bg-gradient-to-tr ${theme === 'dark' ? 'from-amber-500 to-amber-200' : 'from-[var(--istpet-navy)] to-slate-700'}`}></div>
                    </div>
                </header>

                {children}
            </main>
        </div>
    );
};

export default Layout;
