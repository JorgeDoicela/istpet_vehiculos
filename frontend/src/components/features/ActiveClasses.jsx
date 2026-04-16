import React, { useState, useEffect, useRef, useLayoutEffect } from 'react';
import { fmtTimeSpan, fmtTiempoEnRuta } from '../../utils/agendaUi';

const MAX_VISIBLE_CARDS = 3;

/**
 * Active Classes Component: Absolute SIGAFI Parity 2026.
 * All property naming aligned with ClaseActiva model (idRegistro, idAlumno, numeroVehiculo).
 */
const ActiveClasses = ({ classes, clockSkew = 0 }) => {
    const safeClasses = Array.isArray(classes) ? classes : [];

    const [horaActual, setHoraActual] = useState('');
    const [listMaxPx, setListMaxPx] = useState(null);
    const gridRef = useRef(null);

    useEffect(() => {
        const tick = () => {
            const syncedNow = new Date(new Date().getTime() + clockSkew);
            setHoraActual(syncedNow.toLocaleTimeString('es-EC', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }));
        };
        tick();
        const id = setInterval(tick, 1000);
        return () => clearInterval(id);
    }, [clockSkew]);

    const classCount = Array.isArray(classes) ? classes.length : 0;

    useLayoutEffect(() => {
        const grid = gridRef.current;
        if (!grid || classCount === 0) {
            // Using requestAnimationFrame to avoid "setState during render" warning from lint
            requestAnimationFrame(() => setListMaxPx(null));
            return;
        }

        const measure = () => {
            const g = gridRef.current;
            if (!g) return;
            const cards = [...g.querySelectorAll('[data-active-class-card]')];
            if (cards.length === 0) {
                setListMaxPx(null);
                return;
            }
            const m = Math.min(cards.length, MAX_VISIBLE_CARDS);
            let top = Infinity;
            let bottom = -Infinity;
            for (let i = 0; i < m; i++) {
                const r = cards[i].getBoundingClientRect();
                top = Math.min(top, r.top);
                bottom = Math.max(bottom, r.bottom);
            }
            if (!Number.isFinite(top) || !Number.isFinite(bottom) || bottom <= top) return;
            const px = Math.ceil(bottom - top);
            requestAnimationFrame(() => setListMaxPx(Math.max(48, px)));
        };

        measure();
        const ro = new ResizeObserver(measure);
        ro.observe(grid);
        window.addEventListener('resize', measure);
        return () => {
            ro.disconnect();
            window.removeEventListener('resize', measure);
        };
    }, [classCount]);

    return (
        <div className="apple-card space-y-6 lg:space-y-8 p-4 lg:p-8 min-h-0 flex flex-col transition-all">
            <div className="flex items-start gap-4 border-b border-[var(--apple-border)]/70 pb-4 lg:pb-6">
                <div className="flex-1">
                    <h3 className="text-xl lg:text-2xl font-black tracking-tight uppercase text-[var(--apple-text-main)] leading-none">En Ruta</h3>
                    <p className="text-[8px] lg:text-[10px] font-black uppercase tracking-[0.18em] text-[var(--apple-text-sub)] mt-1.5 opacity-60">Monitoreo en tiempo real</p>
                </div>
                <div className="flex items-center gap-2">
                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse shadow-sm shadow-emerald-500/30 shrink-0"></span>
                    <span className="text-lg font-black tabular-nums text-[var(--apple-text-main)] tracking-tight leading-none">{horaActual}</span>
                </div>
            </div>

            <div
                className="flex-1 min-h-0 overflow-y-auto overflow-x-hidden overscroll-y-contain pr-2 custom-scrollbar"
                style={listMaxPx != null ? { maxHeight: listMaxPx } : undefined}
            >
                {safeClasses.length === 0 ? (
                    <div className="flex flex-col items-center justify-center min-h-[220px] py-12 text-[var(--apple-text-sub)] gap-3 opacity-30">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-12 h-12">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                        </svg>
                        <p className="text-[10px] font-black uppercase tracking-[0.2em]">Sin clases activas</p>
                    </div>
                ) : (
                    <div ref={gridRef} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2 gap-4">
                        {safeClasses.map((c, idx) => {
                            const syncedNow = new Date(new Date().getTime() + clockSkew);
                            const tiempoEnRuta = fmtTiempoEnRuta(c.salida, syncedNow);
                            return (
                            <div
                                key={c.idRegistro || c.idPractica || idx}
                                data-active-class-card
                                className="group flex flex-col gap-3 p-4 lg:p-5 rounded-3xl transition-all duration-500 border border-[var(--apple-border)]/80 bg-[var(--apple-card)] hover:border-[var(--istpet-gold)]/30 hover:shadow-[0_8px_24px_rgba(0,0,0,0.07)]"
                            >

                                {/* Fila 1: Nombre + Hora */}
                                <div className="flex justify-between items-start gap-2">
                                    <div className="min-w-0 flex-1">
                                        <h4 className="text-xs font-black uppercase tracking-tight text-[var(--apple-text-main)] truncate leading-tight">{c.estudiante || 'ESTUDIANTE N/A'}</h4>
                                        <p className="text-[8px] font-bold text-[var(--apple-text-sub)] tracking-[0.12em] opacity-55 mt-0.5">ID {c.idAlumno || '---'}</p>
                                    </div>
                                    <div className="text-right shrink-0">
                                        <p className="text-sm font-black tabular-nums text-[var(--istpet-gold)] leading-tight">{fmtTimeSpan(c.salida)}</p>
                                        <p className="text-[7px] font-black uppercase text-[var(--apple-text-sub)] tracking-[0.14em] opacity-50">Salida</p>
                                    </div>
                                </div>

                                {/* Divisor */}
                                <div className="h-px bg-[var(--apple-border)]/50" />

                                {/* Fila 2: Vehículo + tiempo en ruta */}
                                <div className="flex items-center justify-between gap-2">
                                    <div className="flex items-center gap-2 min-w-0">
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="w-4 h-4 shrink-0 text-[var(--istpet-gold)]">
                                            <path d="M5.25 11.25A2.25 2.25 0 0 0 3 13.5v3a2.25 2.25 0 0 0 2.25 2.25H6a1.5 1.5 0 1 0 3 0h6a1.5 1.5 0 1 0 3 0h.75A2.25 2.25 0 0 0 21 16.5v-3a2.25 2.25 0 0 0-2.25-2.25h-.386l-.665-2.217A2.25 2.25 0 0 0 15.545 7.5h-7.09a2.25 2.25 0 0 0-2.154 1.533l-.665 2.217H5.25Zm2.46-2.25h8.58c.322 0 .606.21.699.517l.52 1.733H6.49l.52-1.733A.75.75 0 0 1 7.71 9ZM6 14.625a.75.75 0 1 1 0-1.5h.008a.75.75 0 0 1 0 1.5H6Zm12 0a.75.75 0 1 1 0-1.5h.008a.75.75 0 0 1 0 1.5H18Z" />
                                        </svg>
                                        <span className="text-[10px] font-black text-[var(--apple-text-main)] uppercase tracking-tight truncate">
                                            #{c.numeroVehiculo || '---'} · {c.placa || '---'}
                                        </span>
                                    </div>
                                    {tiempoEnRuta && (
                                        <span className="text-[7px] font-black uppercase tracking-[0.12em] text-emerald-600 shrink-0">
                                            {tiempoEnRuta}
                                        </span>
                                    )}
                                </div>

                                {/* Fila 3: Instructor + Práctica */}
                                <div className="flex items-center justify-between gap-2">
                                    <div className="flex items-center gap-1.5 min-w-0">
                                        <span className="text-[7px] font-black uppercase tracking-[0.12em] text-[var(--apple-text-sub)] opacity-50 shrink-0">Instructor</span>
                                        <span className="text-[8px] font-black uppercase tracking-tight text-[var(--apple-text-main)] truncate">{c.instructor || '—'}</span>
                                    </div>
                                    {c.idPractica ? (
                                        <span className="text-[7px] font-black uppercase tracking-[0.12em] text-[var(--apple-text-sub)] opacity-60 shrink-0">
                                            #{c.idPractica}
                                        </span>
                                    ) : null}
                                </div>

                            </div>
                            );
                        })}
                    </div>
                )}
            </div>

        </div>
    );
};

export default ActiveClasses;
