import React, { useState, useRef, useEffect } from 'react';
import Sidebar from './Sidebar';
import { useTheme } from '../common/ThemeContext';
import { useAuth } from '../../context/AuthContext';
import { useOperativeAlerts } from '../../context/OperativeAlertsContext';
import { fmtTiempoEnRuta, fmtTimeSpan } from '../../utils/agendaUi';

const logoImg = '/favicon.png';

const Layout = ({ children }) => {
    const { theme, toggleTheme } = useTheme();
    const { logout } = useAuth();
    const {
        alertasExcesoRuta,
        limiteMinutosEnRuta,
        systemNotificationsSupported,
        systemNotifPermission,
        requestSystemNotifications
    } = useOperativeAlerts();
    const [notifOpen, setNotifOpen] = useState(false);
    const notifWrapRef = useRef(null);

    useEffect(() => {
        if (!notifOpen) return;
        const close = (e) => {
            if (notifWrapRef.current && !notifWrapRef.current.contains(e.target)) {
                setNotifOpen(false);
            }
        };
        document.addEventListener('mousedown', close);
        return () => document.removeEventListener('mousedown', close);
    }, [notifOpen]);

    /** Solicitud automática de permiso: primer toque o ~1 s (lo que ocurra antes). */
    useEffect(() => {
        if (!systemNotificationsSupported || systemNotifPermission !== 'default') return;

        let done = false;
        let timerId = null;

        const ask = () => {
            if (done) return;
            done = true;
            void requestSystemNotifications();
        };

        const onFirstPointer = () => {
            if (timerId != null) {
                clearTimeout(timerId);
                timerId = null;
            }
            document.removeEventListener('pointerdown', onFirstPointer, true);
            ask();
        };

        document.addEventListener('pointerdown', onFirstPointer, true);
        timerId = setTimeout(() => {
            document.removeEventListener('pointerdown', onFirstPointer, true);
            ask();
        }, 1000);

        return () => {
            if (timerId != null) clearTimeout(timerId);
            document.removeEventListener('pointerdown', onFirstPointer, true);
        };
    }, [systemNotificationsSupported, systemNotifPermission, requestSystemNotifications]);

    const nAlertas = alertasExcesoRuta.length;

    return (
        <div className="flex min-h-screen">
            {/* Sidebar Flotante */}
            <Sidebar />

            {/* Área de Contenido Principal */}
            <main className="flex-1 lg:ml-80 transition-all duration-700">
                {/* Header Fijo Zenith 2026 */}
                <header className="fixed top-0 left-0 lg:left-80 right-0 z-40 bg-[var(--apple-bg)]/80 backdrop-blur-2xl border-b border-[var(--apple-border)] px-4 lg:px-6 py-2 lg:py-4 flex items-center justify-between transition-all duration-500 animate-apple-in">
                    <div className="flex items-center gap-2 lg:gap-5 hover:opacity-90 transition-all cursor-pointer">
                        <img
                            src={logoImg}
                            alt="Logo ISTPET"
                            className="w-9 h-9 lg:w-16 lg:h-16 object-contain transition-all duration-700 hover:scale-105 active:scale-95"
                        />
                        <div className="flex flex-col">
                            <h2 className="text-[10px] lg:text-base font-black tracking-[0.15em] text-[var(--apple-text-main)] leading-none mb-0.5 uppercase drop-shadow-sm">
                                Escuela Conducción
                            </h2>
                            <span className="text-[10px] lg:text-base font-black tracking-tighter text-[var(--istpet-gold)] leading-none uppercase">
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

                        <div className="relative" ref={notifWrapRef}>
                            <button
                                type="button"
                                onClick={() => setNotifOpen((o) => !o)}
                                className="relative p-2 text-[var(--apple-text-main)] hover:text-[var(--istpet-gold)] hover:scale-120 transition-all duration-500 group"
                                title="Alertas de rutas prolongadas"
                                aria-expanded={notifOpen}
                                aria-haspopup="true"
                            >
                                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-5 h-5 lg:w-6 lg:h-6 transition-transform group-hover:rotate-12">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                                </svg>
                                {nAlertas > 0 ? (
                                    <span className="absolute top-1 right-0.5 min-w-[1.1rem] h-[1.1rem] px-0.5 flex items-center justify-center rounded-full bg-amber-500 text-[9px] font-black text-white leading-none shadow-sm">
                                        {nAlertas > 9 ? '9+' : nAlertas}
                                    </span>
                                ) : null}
                            </button>
                            {notifOpen ? (
                                <div
                                    className={`fixed inset-x-4 top-16 md:absolute md:inset-auto md:right-0 md:top-full md:mt-2 
                                               w-auto md:w-[22rem] max-h-[70vh] md:max-h-[28rem] 
                                               overflow-y-auto rounded-[2rem] z-[120] py-2 px-1 animate-apple-in
                                               apple-glass`}
                                    role="menu"
                                >
                                    <div className="flex items-center justify-between px-4 py-3 border-b border-[var(--apple-border)]/50 mb-1">
                                        <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">
                                            Rutas Prolongadas ({limiteMinutosEnRuta / 60}h+)
                                        </p>
                                        <button onClick={() => setNotifOpen(false)} className="md:hidden p-1 text-[var(--apple-text-sub)]">
                                            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                        </button>
                                    </div>
                                    {systemNotificationsSupported ? (
                                        systemNotifPermission === 'default' ? (
                                            <div className="px-3 py-2.5 border-b border-[var(--apple-border)]/50 space-y-2">
                                                <p className="text-[10px] font-semibold text-[var(--apple-text-sub)] leading-snug">
                                                    Recibe avisos de rutas largas en la barra del sistema o en el teléfono (Chrome/Android; en iPhone suele requerir añadir la web a inicio).
                                                </p>
                                                <button
                                                    type="button"
                                                    onClick={() => requestSystemNotifications()}
                                                    className="w-full py-2.5 rounded-xl bg-[var(--istpet-navy)] text-white text-[10px] font-black uppercase tracking-widest touch-manipulation"
                                                >
                                                    Activar notificaciones del navegador
                                                </button>
                                            </div>
                                        ) : null
                                    ) : (
                                        <p className="px-3 py-2 text-[10px] text-[var(--apple-text-sub)] border-b border-[var(--apple-border)]/50">
                                            Este navegador no admite notificaciones del sistema.
                                        </p>
                                    )}
                                    {nAlertas === 0 ? (
                                        <p className="px-3 py-6 text-center text-xs font-semibold text-[var(--apple-text-sub)]">
                                            No hay vehículos con ruta prolongada.
                                        </p>
                                    ) : (
                                        <ul className="py-1">
                                            {alertasExcesoRuta.map((c) => {
                                                const placaTxt = (c.placa || '').trim();
                                                const vehTxt =
                                                    c.numeroVehiculo != null
                                                        ? `#${c.numeroVehiculo}${placaTxt ? ` · ${placaTxt}` : ''}`
                                                        : placaTxt || '—';
                                                const enRuta = fmtTiempoEnRuta(c.salida);
                                                return (
                                                    <li
                                                        key={c.idPractica}
                                                        className="px-4 py-3.5 rounded-2xl hover:bg-[var(--apple-primary)]/5 border-b border-[var(--apple-border)]/30 last:border-0 transition-colors"
                                                    >
                                                        <p className="text-[13px] font-black text-[var(--apple-text-main)] uppercase truncate tracking-tight">
                                                            {c.estudiante || '—'}
                                                        </p>
                                                        <div className="flex items-center gap-2 mt-1">
                                                            <span className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase">
                                                                {vehTxt}
                                                            </span>
                                                            <span className="text-[10px] font-black text-amber-500 bg-amber-500/10 px-2 py-0.5 rounded-lg">
                                                                {enRuta}
                                                            </span>
                                                        </div>
                                                        <p className="text-[10px] font-semibold text-[var(--apple-text-sub)] mt-2 opacity-60">
                                                            Salida {fmtTimeSpan(c.salida)} · {c.instructor || '—'}
                                                        </p>
                                                    </li>
                                                );
                                            })}
                                        </ul>
                                    )}
                                </div>
                            ) : null}
                        </div>

                        {/* Logout Button */}
                        <button
                            onClick={logout}
                            className="p-2 text-rose-500 hover:scale-120 transition-all duration-500 group active:scale-95"
                            title="Cerrar Sesión"
                        >
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-5 h-5 lg:w-6 lg:h-6 transition-transform group-hover:translate-x-0.5">
                                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
                            </svg>
                        </button>
                    </div>
                </header>

                <div className="p-3 lg:p-12 pt-16 lg:pt-28 pb-24 lg:pb-12">
                    {children}
                </div>
            </main>
        </div>
    );
};

export default Layout;
