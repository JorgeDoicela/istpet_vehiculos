import React from 'react';

const ConfirmModal = ({ 
  isOpen, 
  onClose, 
  onConfirm, 
  title = "¿Está seguro?", 
  message = "Esta acción no se puede deshacer.", 
  confirmText = "Eliminar", 
  cancelText = "Cancelar",
  type = "danger" 
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[1000] flex items-center justify-center p-4 lg:p-6 animate-fadeIn">
      {/* Overlay */}
      <div 
        className="absolute inset-0 bg-black/40 backdrop-blur-sm transition-opacity"
        onClick={onClose}
      />
      
      {/* Modal Card */}
      <div className="relative apple-glass rounded-[2rem] w-full max-w-md overflow-hidden shadow-2xl animate-apple-in" style={{ animationDuration: '0.4s' }}>
        <div className="p-8">
          <div className="flex flex-col items-center text-center">
            {/* Icon */}
            <div className={`w-16 h-16 rounded-full flex items-center justify-center mb-6 
              ${type === 'danger' ? 'bg-rose-500/10 text-rose-500' : 'bg-[var(--apple-primary)]/10 text-[var(--apple-primary)]'}`}
            >
              {type === 'danger' ? (
                <svg className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              ) : (
                <svg className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
              )}
            </div>

            <h3 className="text-xl font-black text-[var(--apple-text-main)] mb-3 tracking-tight">
              {title}
            </h3>
            
            <p className="text-sm font-semibold text-[var(--apple-text-sub)] leading-relaxed">
              {message}
            </p>
          </div>

          <div className="grid grid-cols-2 gap-4 mt-8">
            <button
              onClick={onClose}
              className="px-6 py-3.5 rounded-full text-sm font-black text-[var(--apple-text-main)] bg-[var(--apple-bg)] hover:bg-[var(--apple-border)]/10 transition-all active:scale-95 border border-[var(--apple-border)]"
            >
              {cancelText}
            </button>
            <button
              onClick={() => {
                onConfirm();
                onClose();
              }}
              className={`px-6 py-3.5 rounded-full text-sm font-black text-white shadow-lg transition-all active:scale-95
                ${type === 'danger' ? 'bg-rose-500 shadow-rose-500/20 hover:bg-rose-600' : 'bg-[var(--istpet-gold)] shadow-amber-500/20 hover:brightness-105'}`}
            >
              {confirmText}
            </button>
          </div>
        </div>
        
        {/* Decorative glass highlight */}
        <div className="absolute top-0 right-0 h-full w-24 bg-gradient-to-l from-white/[0.05] to-transparent skew-x-12 translate-x-12 pointer-events-none"></div>
      </div>
      
      <style dangerouslySetInnerHTML={{ __html: `
        @keyframes fadeIn {
          from { opacity: 0; }
          to { opacity: 1; }
        }
      `}} />
    </div>
  );
};

export default ConfirmModal;
