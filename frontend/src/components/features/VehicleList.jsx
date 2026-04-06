import React from 'react';

const VehicleList = ({ vehicles }) => {
  // Aseguramos que vehicles sea un array antes de renderizar
  const safeVehicles = Array.isArray(vehicles) ? vehicles : [];

  return (
    <div className="space-y-12">
      <div className="flex items-center justify-between px-4">
        <h3 className="text-3xl font-black tracking-tighter text-slate-900 uppercase">Flota Operativa</h3>
        <span className="px-6 py-2 bg-white/50 backdrop-blur-md rounded-full text-[10px] font-black uppercase tracking-widest text-slate-500 shadow-sm border border-white/50">
          {safeVehicles.length} Unidades
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
        {safeVehicles.map((v) => (
          <div key={v.id_Vehiculo || v.id_vehiculo || Math.random()} className="apple-card group">
            <div className="flex justify-between items-start mb-10">
              <div className="flex items-center gap-4">
                <div className="w-14 h-14 bg-white rounded-3xl flex items-center justify-center text-slate-900 shadow-xl border border-slate-50 group-hover:scale-110 transition-transform duration-700">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-7 h-7">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 18.75a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 01-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h1.125c.621 0 1.129-.504 1.129-1.125V14.25M7.5 14.25v-3.375c0-.621.504-1.125 1.125-1.125h9.192c.465 0 .867.285 1.03.681l1.43 3.494c.041.1.063.208.063.317v1.125m-15.5 0h15.5" />
                  </svg>
                </div>
                <div>
                   <p className="text-[10px] font-black uppercase tracking-widest text-slate-400">Unidad</p>
                   {/* Soportamos tanto numero_Vehiculo como numero_vehiculo */}
                   <h4 className="text-2xl font-black text-slate-900 tracking-tighter">#{v.numero_Vehiculo || v.numero_vehiculo || '---'}</h4>
                </div>
              </div>
              <span className={`px-4 py-1 rounded-2xl text-[9px] font-black uppercase border-2 ${
                (v.estado_Mecanico || v.estado_mecanico) === 'OPERATIVO' 
                  ? 'bg-emerald-50 text-emerald-600 border-emerald-100' 
                  : 'bg-rose-50 text-rose-600 border-rose-100'
              }`}>
                {v.estado_Mecanico || v.estado_mecanico || 'DESCONOCIDO'}
              </span>
            </div>

            <div className="space-y-6">
              <div className="grid grid-cols-2 gap-4">
                 <div className="p-4 bg-white/40 rounded-3xl">
                    <p className="text-[9px] font-black uppercase text-slate-400">Marca</p>
                    <p className="text-sm font-bold text-slate-700">{v.marca || 'N/A'}</p>
                 </div>
                 <div className="p-4 bg-white/40 rounded-3xl">
                    <p className="text-[9px] font-black uppercase text-slate-400">Placa</p>
                    <p className="text-sm font-bold text-slate-700">{v.placa || '---'}</p>
                 </div>
              </div>
            </div>


          </div>
        ))}
      </div>
    </div>
  );
};

export default VehicleList;
