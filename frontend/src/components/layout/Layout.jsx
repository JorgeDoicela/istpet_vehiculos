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
            <main className="flex-1 lg:ml-80 transition-all duration-700">
                {/* Header Fijo Zenith 2026 */}
                <header className="fixed top-0 left-0 lg:left-80 right-0 z-40 bg-[var(--apple-bg)]/80 backdrop-blur-2xl border-b border-[var(--apple-border)] px-4 lg:px-12 py-3 lg:py-6 flex items-center justify-between transition-all duration-500 animate-apple-in">
                        <div className="flex items-center gap-4 lg:gap-6">
                            <img 
                                src={logoImg} 
                                alt="Logo ISTPET" 
                                className="w-12 h-12 lg:w-20 lg:h-20 object-contain transition-all duration-500 hover:scale-105 drop-shadow-[0_2px_8px_rgba(0,30,80,0.15)]" 
                            />
                            <div className="flex flex-col">
                                <h2 className="text-sm lg:text-xl font-black tracking-tighter text-[var(--istpet-navy)] leading-none mb-1 uppercase opacity-80">
                                    Gestión Conducción
                                </h2>
                                <span className="text-lg lg:text-3xl font-black tracking-tighter text-[var(--istpet-gold)] leading-none -mt-1">
                                    ISTPET
                                </span>
                            </div>
                        </div>

                    <div className="flex items-center gap-3 lg:gap-6">
                        {/* Theme Toggle Button */}
                        <button
                            onClick={toggleTheme}
                            className="p-2 text-[var(--istpet-gold)] hover:scale-120 hover:text-[var(--istpet-gold)] transition-all duration-500 group"
                            title={theme === 'light' ? 'Cambiar a Modo Oscuro' : 'Cambiar a Modo Claro'}
                        >
                            {theme === 'light' ? (
                                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-5 h-5 lg:w-6 lg:h-6">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z" />
                                </svg>
                            ) : (
                                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-5 h-5 lg:w-6 lg:h-6">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z" />
                                </svg>
                            )}
                        </button>

                        <button className="relative p-2 text-[var(--apple-text-sub)] hover:text-[var(--apple-primary)] hover:scale-120 transition-all duration-500 group">
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-5 h-5 lg:w-6 lg:h-6 transition-transform group-hover:rotate-12">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                            </svg>
                        </button>

                        {/* Logout Button */}
                        <button 
                            onClick={() => {
                                if (window.confirm('¿Deseas cerrar la sesión?')) {
                                    localStorage.clear();
                                    window.location.reload();
                                }
                            }}
                            className="p-2 text-rose-500 hover:scale-120 transition-all duration-500 group active:scale-95"
                            title="Cerrar Sesión"
                        >
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-5 h-5 lg:w-6 lg:h-6 transition-transform group-hover:translate-x-0.5">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
                            </svg>
                        </button>
                    </div>
                </header>

                <div className="p-4 lg:p-12 pt-24 lg:pt-36">
                    {children}
                </div>
            </main>
        </div>
    );
};

export default Layout;
