import React from 'react';
import StatusBadge from '../common/StatusBadge';

const VehicleCard = ({ vehiculo, isSelected, onSelect }) => {
    return (
        <div
            onClick={() => onSelect(vehiculo)}
            className={`relative p-4 rounded-2xl border-2 transition-all cursor-pointer group hover:scale-[1.02] active:scale-[0.98] ${isSelected
                    ? 'bg-blue-50 border-blue-500 shadow-md ring-4 ring-blue-100'
                    : 'bg-white/40 border-slate-200/60 hover:border-blue-300 hover:bg-white/80'
                }`}
        >
            <div className="flex justify-between items-center mb-1">
        <div className="bg-white/90 border border-slate-200 px-3 py-1 rounded-xl shadow-sm">
            <span className="text-2xl font-black text-slate-800 tracking-tight">#{vehiculo.numero_vehiculo || vehiculo.vehiculoStr.split(' #').pop()}</span>
        </div>
        <StatusBadge status={vehiculo.estado || 'Disponible'} />
      </div>
      
      <div className="mb-4 px-1">
        <p className="text-[10px] font-bold text-slate-400/80 tracking-tighter uppercase italic">{vehiculo.vehiculoStr.split(' - ')[0]}</p>
      </div>

      <div className="mt-2 pt-3 border-t border-slate-100/60">
        <div className="flex items-center gap-2">
            <div className="h-6 w-6 rounded-lg bg-slate-100 flex items-center justify-center text-slate-400">
                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
            </div>
            <span className="text-[9px] font-black text-slate-500 uppercase tracking-tight truncate">
                {vehiculo.instructorNombre || 'Sin Instructor'}
            </span>
        </div>
      </div>

            {isSelected && (
                <div className="absolute -top-1.5 -right-1.5 h-5 w-5 bg-blue-500 rounded-full flex items-center justify-center text-white shadow-sm border border-white">
                    <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="4">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                </div>
            )}
        </div>
    );
};

export default VehicleCard;
