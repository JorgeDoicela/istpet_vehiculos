import React from 'react';
import { fmtTimeSpan } from '../../utils/agendaUi';

/**
 * Active Classes Component: Absolute SIGAFI Parity 2026.
 * All property naming aligned with ClaseActiva model (idRegistro, idAlumno, numeroVehiculo).
 */
const ActiveClasses = ({ classes }) => {
  const safeClasses = Array.isArray(classes) ? classes : [];

  return (
    <div className="apple-card space-y-6 lg:space-y-8 p-4 lg:p-8 min-h-[400px] flex flex-col transition-all">
      <div className="flex items-center justify-between border-b border-[var(--apple-border)] pb-4 lg:pb-6">
        <div>
           <h3 className="text-xl lg:text-2xl font-black tracking-tighter uppercase text-[var(--apple-text-main)] leading-none">En Ruta</h3>
           <p className="text-[8px] lg:text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)] mt-1.5 opacity-60">Monitoreo Real-time</p>
        </div>
        <div className="flex items-center gap-2 px-3 py-1.5 bg-emerald-500/5 rounded-full border border-emerald-500/10">
           <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse shadow-sm shadow-emerald-500/20"></span>
           <span className="text-[8px] font-black uppercase text-emerald-600 tracking-widest">Live Sync</span>
        </div>
      </div>

      <div className="flex-1 space-y-3 lg:space-y-4 overflow-y-auto max-h-[600px] pr-2 custom-scrollbar">
        {safeClasses.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full py-12 text-[var(--apple-text-sub)] gap-3 opacity-30">
             <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-12 h-12">
               <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
             </svg>
             <p className="text-[10px] font-black uppercase tracking-[0.2em]">Sin clases activas</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2 gap-3">
            {safeClasses.map((c, idx) => (
              <div key={c.idRegistro || c.idPractica || idx} className="group p-3 lg:p-4 bg-[var(--apple-bg)] rounded-2xl transition-all duration-500 border border-[var(--apple-border)] hover:border-[var(--istpet-gold)]/40 hover:shadow-lg">
                <div className="flex justify-between items-start mb-3">
                  <div className="min-w-0 flex-1">
                     <h4 className="text-[11px] lg:text-xs font-black uppercase tracking-tighter text-[var(--apple-text-main)] truncate">{c.estudiante || 'ESTUDIANTE N/A'}</h4>
                     <p className="text-[8px] font-black uppercase text-[var(--apple-text-sub)] tracking-widest opacity-60">ID: {c.idAlumno?.substring(0,6) || '---'}...</p>
                  </div>
                  <div className="text-right ml-2">
                     <p className="text-[11px] lg:text-[13px] font-black tabular-nums text-[var(--istpet-gold)]">
                       {fmtTimeSpan(c.salida)}
                     </p>
                     <p className="text-[7px] font-black uppercase text-[var(--apple-text-sub)] tracking-widest opacity-50">Salida</p>
                  </div>
                </div>
                
                <div className="flex items-center gap-3 p-2 bg-[var(--apple-card)] rounded-xl border border-[var(--apple-border)]">
                   <div className="w-7 h-7 rounded-lg bg-[var(--istpet-gold)] flex items-center justify-center text-white shadow-md">
                      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-3.5 h-3.5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 18.75a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 0 1-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h1.125c.621 0 1.129-.504 1.129-1.125V14.25M7.5 14.25v-3.375c0-.621.504-1.125 1.125-1.125h9.192c.465 0 .867.285 1.03.681l1.43 3.494c.041.1.063.208.063.317v1.125m-15.5 0h15.5" />
                      </svg>
                   </div>
                   <div className="min-w-0">
                      <h5 className="text-[10px] font-black text-[var(--apple-text-main)] tracking-tighter uppercase truncate">#{c.numeroVehiculo || '---'} — {c.placa || 'PLACA'}</h5>
                      <p className="text-[7px] font-bold text-[var(--apple-text-sub)] uppercase tracking-widest opacity-50">Unidad Operativa</p>
                   </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="mt-6 p-4 lg:p-5 bg-[var(--istpet-navy)] rounded-2xl text-white shadow-lg relative overflow-hidden group">
          <div className="absolute top-0 right-0 w-24 h-24 bg-white/5 rounded-full blur-2xl translate-x-8 -translate-y-8"></div>
          <div className="flex justify-between items-center relative z-10">
              <div>
                  <p className="text-[8px] font-black uppercase tracking-[0.2em] text-white/40 mb-0.5">Resumen</p>
                  <span className="text-2xl font-black text-white">{safeClasses.length}</span>
              </div>
              <span className="text-[8px] font-black uppercase text-[var(--istpet-gold)] tracking-widest bg-[var(--istpet-gold)]/10 px-2 py-1 rounded-md border border-[var(--istpet-gold)]/20">Sync OK</span>
          </div>
      </div>
    </div>
  );
};

export default ActiveClasses;
