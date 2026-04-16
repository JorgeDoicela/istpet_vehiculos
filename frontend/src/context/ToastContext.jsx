/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, useCallback, useRef } from 'react';

const ToastContext = createContext(null);

/**
 * Modern Toast System - Apple Zenith 2026 Edition
 * Supports strings or objects: { title, description }
 */
export const useToast = () => {
    const ctx = useContext(ToastContext);
    if (!ctx) throw new Error('useToast debe usarse dentro de <ToastProvider>');
    return ctx;
};

let _nextId = 0;

export const ToastProvider = ({ children }) => {
    const [toasts, setToasts] = useState([]);
    const timers = useRef({});

    const dismiss = useCallback((id) => {
        clearTimeout(timers.current[id]);
        delete timers.current[id];
        setToasts(prev => prev.filter(t => t.id !== id));
    }, []);

    const toast = useCallback((payload, type = 'success', duration = 4000) => {
        const id = ++_nextId;
        const entry = typeof payload === 'string' 
            ? { id, title: payload, description: '', type }
            : { id, ...payload, type };
            
        setToasts(prev => [...prev.slice(-4), entry]);
        timers.current[id] = setTimeout(() => dismiss(id), duration);
        return id;
    }, [dismiss]);

    const success = useCallback((msg, duration) => toast(msg, 'success', duration), [toast]);
    const error   = useCallback((msg, duration) => toast(msg, 'error',   duration), [toast]);
    const warning = useCallback((msg, duration) => toast(msg, 'warning', duration), [toast]);
    const info    = useCallback((msg, duration) => toast(msg, 'info',    duration), [toast]);

    return (
        <ToastContext.Provider value={{ toast, success, error, warning, info, dismiss }}>
            {children}
            <ToastContainer toasts={toasts} onDismiss={dismiss} />
        </ToastContext.Provider>
    );
};

const TYPE_CONFIG = {
    success: { 
        bg: 'bg-emerald-500/10', 
        border: 'border-emerald-500/20',
        iconColor: 'text-emerald-500',
        icon: (
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M5 13l4 4L19 7" />
            </svg>
        )
    },
    error: { 
        bg: 'bg-rose-500/10', 
        border: 'border-rose-500/20',
        iconColor: 'text-rose-500',
        icon: (
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M6 18L18 6M6 6l12 12" />
            </svg>
        )
    },
    warning: { 
        bg: 'bg-amber-500/10', 
        border: 'border-amber-500/20',
        iconColor: 'text-amber-500',
        icon: (
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
        )
    },
    info: { 
        bg: 'bg-sky-500/10', 
        border: 'border-sky-500/20',
        iconColor: 'text-sky-500',
        icon: (
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
        )
    },
};

const ToastContainer = ({ toasts, onDismiss }) => {
    if (toasts.length === 0) return null;
    return (
        <div className="fixed top-6 left-1/2 -translate-x-1/2 md:left-auto md:translate-x-0 md:top-24 md:right-6 z-[9999] 
                        flex flex-col items-center md:items-end gap-3 pointer-events-none w-full px-4 md:px-0">
            {toasts.map(t => (
                <Toast key={t.id} toast={t} onDismiss={onDismiss} />
            ))}
        </div>
    );
};

const Toast = ({ toast, onDismiss }) => {
    const config = TYPE_CONFIG[toast.type] || TYPE_CONFIG.info;
    return (
        <div
            className={`pointer-events-auto flex items-center gap-4 w-full md:min-w-[380px] md:w-auto max-w-[500px]
                       apple-glass rounded-2xl p-4 pr-5 
                       animate-apple-in transition-all active:scale-95`}
            role="alert"
        >
            <div className={`p-2.5 rounded-xl ${config.bg} ${config.iconColor} shrink-0`}>
                {config.icon}
            </div>
            
            <div className="flex-1 min-w-0">
                <h4 className="text-[13px] font-black text-[var(--apple-text-main)] uppercase tracking-tight leading-none mb-1">
                    {toast.title}
                </h4>
                {toast.description && (
                    <p className="text-[11px] font-bold text-[var(--apple-text-sub)] leading-snug opacity-80">
                        {toast.description}
                    </p>
                )}
            </div>

            <button
                onClick={() => onDismiss(toast.id)}
                className="text-[var(--apple-text-sub)] hover:text-rose-500
                           shrink-0 transition-all opacity-30 hover:opacity-100 p-1 ml-2"
                aria-label="Cerrar"
            >
                <svg className="w-4 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
            </button>
        </div>
    );
};

export default ToastProvider;
