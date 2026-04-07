import React from 'react';

const VehicleCard = ({ vehiculo, isSelected, isSuggested, onSelect }) => {
    const isOperativo = vehiculo.estado === 'OPERATIVO' || !vehiculo.estado;
    const numero = vehiculo.numeroVehiculo || vehiculo.numero_vehiculo || vehiculo.vehiculoStr?.split('#').pop();
    const placa = vehiculo.placa;

    return (
        <div
            onClick={() => isOperativo ? onSelect(vehiculo) : null}
            className={`
                group relative p-2 rounded-[2.5rem] border-2 transition-all duration-500 cursor-pointer overflow-hidden
                flex flex-col items-center justify-center min-h-[75px] lg:min-h-[90px]
                ${!isOperativo
                    ? 'opacity-30 grayscale cursor-not-allowed border-[var(--apple-border)] bg-[var(--apple-bg)]'
                    : isSelected
                        ? 'bg-gradient-to-br from-[var(--apple-primary)]/[0.1] to-white border-[var(--apple-primary)] shadow-lg shadow-blue-500/10 scale-[1.02] z-10'
                        : isSuggested
                            ? 'bg-emerald-50/40 border-emerald-500/30 hover:border-emerald-500 hover:bg-emerald-50'
                            : 'bg-[var(--apple-card)] border-[var(--apple-border)] hover:border-[var(--apple-primary)]/50 hover:bg-[var(--apple-bg)] hover:shadow-md hover:-translate-y-0.5'
                }
            `}
        >
            {/* Badge Sugerido SIGAFI */}
            {isSuggested && !isSelected && (
                <div className="absolute top-3 left-4 bg-emerald-500 text-white px-2 py-0.5 rounded-full text-[6px] font-black uppercase tracking-widest shadow-sm animate-pulse z-10">
                    Sugerido
                </div>
            )}

            {/* Icono Ultra Compacto */}
            <div className={`mb-1 transition-all duration-500 ${isSelected ? 'text-[var(--apple-primary)] scale-110' : 'text-[var(--apple-text-sub)] group-hover:text-[var(--apple-primary)]'}`}>
                <svg className="h-4 w-4 lg:h-5 lg:w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z" />
                </svg>
            </div>

            <div className="text-center">
                <div className="flex items-center justify-center gap-1">
                    <span className={`text-lg lg:text-xl font-black tracking-tighter transition-colors ${isSelected ? 'text-[var(--apple-primary)]' : 'text-[var(--apple-text-main)]'}`}>
                        #{numero}
                    </span>
                    <div className={`w-1 h-1 rounded-full shrink-0 ${isOperativo ? 'bg-emerald-500 animate-pulse' : 'bg-[var(--apple-border)]'}`}></div>
                </div>
                {/* Placa si está disponible, si no UNIDAD */}
                <p className={`text-[5px] lg:text-[7px] font-black uppercase tracking-[0.15em] leading-none transition-colors ${isSelected ? 'text-[var(--apple-primary)]/70' : 'text-[var(--apple-text-sub)]'}`}>
                    {placa || 'UNIDAD'}
                </p>
            </div>

            {/* Check de Selección Ultra Compacto */}
            {isSelected && (
                <div className="absolute top-3 right-4 h-4 w-4 bg-[var(--apple-primary)] rounded-full flex items-center justify-center text-white scale-110 animate-apple-in shadow-sm">
                    <svg className="h-1.5 w-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                </div>
            )}
        </div>
    );
};

export default VehicleCard;
