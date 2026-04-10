import React, { createContext, useContext, useState, useCallback, useRef } from 'react';

const ToastContext = createContext(null);

/**
 * Tipos disponibles: 'success' | 'error' | 'warning' | 'info'
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

    const toast = useCallback((message, type = 'success', duration = 4000) => {
        const id = ++_nextId;
        setToasts(prev => [...prev.slice(-4), { id, message, type }]);
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

const TYPE_STYLES = {
    success: { bar: 'bg-[var(--apple-primary)]',  icon: '✓' },
    error:   { bar: 'bg-rose-500',                 icon: '✕' },
    warning: { bar: 'bg-amber-400',                icon: '!' },
    info:    { bar: 'bg-sky-400',                  icon: 'i' },
};

const ToastContainer = ({ toasts, onDismiss }) => {
    if (toasts.length === 0) return null;
    return (
        <div className="fixed bottom-6 right-6 z-[9999] flex flex-col gap-3 pointer-events-none">
            {toasts.map(t => (
                <Toast key={t.id} toast={t} onDismiss={onDismiss} />
            ))}
        </div>
    );
};

const Toast = ({ toast, onDismiss }) => {
    const styles = TYPE_STYLES[toast.type] || TYPE_STYLES.info;
    return (
        <div
            className="pointer-events-auto flex items-center gap-3 min-w-[280px] max-w-[420px]
                       bg-[var(--apple-card)] border border-[var(--apple-border)] rounded-2xl
                       shadow-2xl px-4 py-3 animate-apple-in"
            role="alert"
        >
            <div className={`w-1.5 h-10 rounded-full shrink-0 ${styles.bar}`} />
            <p className="text-sm font-bold text-[var(--apple-text-main)] flex-1 leading-snug">
                {toast.message}
            </p>
            <button
                onClick={() => onDismiss(toast.id)}
                className="text-[var(--apple-text-sub)] hover:text-[var(--apple-text-main)]
                           text-lg leading-none ml-1 shrink-0 transition-colors"
                aria-label="Cerrar notificación"
            >
                ×
            </button>
        </div>
    );
};

export default ToastProvider;
