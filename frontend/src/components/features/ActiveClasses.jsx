import React from 'react';

const ActiveClasses = ({ classes }) => {
  // Aseguramos que classes sea un array
  const safeClasses = Array.isArray(classes) ? classes : [];

  return (
    <div className="apple-card space-y-10 p-10 min-h-[500px] flex flex-col">
      <div className="flex items-center justify-between border-b border-white/40 pb-8">
        <div>
           <h3 className="text-2xl font-black tracking-tighter uppercase text-slate-900">En Ruta</h3>
           <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">Monitoreo Real-time</p>
        </div>
        <div className="flex items-center gap-2">
           <span className="w-2 h-2 rounded-full bg-blue-500 animate-pulse"></span>
           <span className="text-[9px] font-black uppercase text-blue-500 tracking-tighter">Live Sync</span>
        </div>
      </div>

      <div className="flex-1 space-y-6">
        {safeClasses.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-slate-400 gap-4 opacity-50">
             <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1} stroke="currentColor" className="w-16 h-16">
               <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
             </svg>
             <p className="text-[10px] font-black uppercase tracking-[0.2em]">Sin clases activas ahora</p>
          </div>
        ) : (
          safeClasses.map((c) => (
            <div key={c.id_Registro || c.id_registro || Math.random()} className="group p-6 bg-white/40 rounded-3xl hover:bg-white transition-all duration-700 border border-transparent hover:border-white hover:shadow-xl hover:-translate-y-1">
              <div className="flex justify-between items-start mb-4">
                <div>
                   <h4 className="text-sm font-black uppercase tracking-tighter text-slate-900">{c.estudiante || 'ESTUDIANTE N/A'}</h4>
                   <p className="text-[9px] font-black uppercase text-slate-400 tracking-widest">{c.cedula || '---'}</p>
                </div>
                <div className="text-right">
                   <p className="text-[14px] font-black tabular-nums text-slate-800">
                     {c.salida ? new Date(c.salida).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '--:--'}
                   </p>
                   <p className="text-[8px] font-black uppercase text-slate-400 tracking-widest">Salida</p>
                </div>
              </div>
              
              <div className="flex items-center gap-4 p-3 bg-white/60 rounded-2xl">
                 <div className="w-8 h-8 rounded-xl bg-[var(--apple-primary)] flex items-center justify-center text-white shadow-lg">
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="w-4 h-4">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 18.75a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h6m-9 0H3.375a1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h1.125c.621 0 1.129-.504 1.129-1.125V14.25M7.5 14.25v-3.375c0-.621.504-1.125 1.125-1.125h9.192c.465 0 .867.285 1.03.681l1.43 3.494c.041.1.063.208.063.317v1.125m-15.5 0h15.5" />
                    </svg>
                 </div>
                 <div>
                    <h5 className="text-[10px] font-black text-slate-800 tracking-tighter uppercase">{c.placa || 'PLACA N/A'}</h5>
                    <p className="text-[8px] font-bold text-slate-400 uppercase tracking-widest">Unidad #{c.numero_Vehiculo || c.numero_vehiculo || '---'}</p>
                 </div>
              </div>
            </div>
          ))
        )}
      </div>

      <div className="mt-8 p-6 bg-slate-900 rounded-[2rem] text-white">
          <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mb-2">Resumen Operativo</p>
          <div className="flex justify-between items-end">
              <span className="text-3xl font-black text-white">{safeClasses.length}</span>
              <span className="text-[9px] font-black uppercase text-blue-400 tracking-widest pb-1">Unidades en Pista</span>
          </div>
      </div>
    </div>
  );
};

export default ActiveClasses;
