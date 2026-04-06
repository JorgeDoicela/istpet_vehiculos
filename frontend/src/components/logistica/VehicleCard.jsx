import React from 'react';
import StatusBadge from '../common/StatusBadge';

const VehicleCard = ({ vehiculo, isSelected, onSelect }) => {
    const isOperativo = vehiculo.estado === 'OPERATIVO' || !vehiculo.estado;

    return (
        <div
            onClick={() => isOperativo ? onSelect(vehiculo) : null}
            className={`group relative p-5 rounded-[2rem] border-2 transition-all duration-500 cursor-pointer overflow-hidden ${
                !isOperativo ? 'opacity-30 grayscale cursor-not-allowed border-[var(--apple-border)] bg-[var(--apple-bg)]' :
                isSelected
                    ? 'bg-gradient-to-br from-[var(--apple-primary)]/[0.05] to-[var(--apple-card)] border-[var(--apple-primary)] shadow-xl shadow-blue-500/10 scale-[1.03]'
                    : 'bg-[var(--apple-card)] border-[var(--apple-border)] hover:border-[var(--apple-primary)]/50 hover:bg-[var(--apple-bg)] hover:shadow-lg hover:-translate-y-1'
                }`}
        >
            <div className="flex items-center gap-5">
                {/* Icono de Vehículo Estilizado */}
                <div className={`p-3 rounded-2xl transition-all duration-500 ${isSelected ? 'bg-[var(--apple-primary)] text-white shadow-lg shadow-blue-500/30' : 'bg-[var(--apple-bg)] text-[var(--apple-text-sub)] group-hover:bg-[var(--apple-primary)]/20 group-hover:text-[var(--apple-primary)]'}`}>
                    <svg className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z" />
                    </svg>
                </div>

                <div className="flex flex-col">
                    <div className="flex items-center gap-2">
                        <span className={`text-4xl font-black tracking-tighter transition-colors ${isSelected ? 'text-[var(--apple-primary)]' : 'text-[var(--apple-text-main)]'}`}>
                            #{vehiculo.numeroVehiculo || vehiculo.numero_vehiculo || vehiculo.vehiculoStr?.split('#').pop()}
                        </span>
                        <div className={`w-2 h-2 rounded-full ${isOperativo ? 'bg-emerald-500 animate-pulse' : 'bg-[var(--apple-border)]'}`}></div>
                    </div>
                    <p className={`text-[10px] font-black uppercase tracking-widest transition-colors ${isSelected ? 'text-[var(--apple-primary)]/70' : 'text-[var(--apple-text-sub)]'}`}>
                        {vehiculo.marca || 'UNIDAD'} {vehiculo.modelo}
                    </p>
                </div>
            </div>

            {/* Micro-indicador de Selección */}
            {isSelected && (
                <div className="absolute top-4 right-4 h-6 w-6 bg-[var(--apple-primary)] rounded-full flex items-center justify-center text-white scale-110 animate-apple-in">
                    <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="4">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                </div>
            )}
        </div>
    );
};

export default VehicleCard;
