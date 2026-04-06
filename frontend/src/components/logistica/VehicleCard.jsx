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
            <div className="flex justify-between items-center mb-4">
                <div className="bg-white/90 border border-slate-200 px-3 py-1 rounded-xl shadow-sm">
                    <span className="text-xl font-black text-slate-800 tracking-tight">#{vehiculo.numero_vehiculo || vehiculo.vehiculoStr.split(' #').pop()}</span>
                </div>
                <StatusBadge status={vehiculo.estado || 'Disponible'} />
            </div>

            <div className="mb-4 px-1">
                <p className="text-[10px] uppercase font-black text-slate-400 tracking-widest leading-none mb-1">Placa</p>
                <p className="text-sm font-bold text-slate-700">{vehiculo.vehiculoStr.split(' - ')[0]}</p>
            </div>

            <div className="mt-3 pt-3 border-t border-slate-100">
                <p className="text-[10px] uppercase font-black text-blue-500 tracking-widest leading-none mb-1 text-right">Instructor</p>
                <p className="text-xs font-bold text-slate-600 text-right truncate">
                    {vehiculo.instructorNombre || 'Sin asignar'}
                </p>
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
