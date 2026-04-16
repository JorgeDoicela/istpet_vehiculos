import React from 'react';

/**
 * Vehicle Selection Card: Absolute SIGAFI Parity 2026.
 * All property naming aligned with SIGAFI schemas (idVehiculo, numeroVehiculo, vehiculoStr).
 */
const placaDesdeVehiculoStr = (str) => {
    if (!str?.trim()) return '---';
    const s = str.trim();
    const sigafi = s.match(/#\s*[\w-]+\s*\(([^)]+)\)/);
    if (sigafi) return sigafi[1].trim();
    return s.split(' - ')[0]?.trim() || '---';
};

const VehicleCard = ({ vehiculo, isSelected, onSelect }) => {
    // Exact mapping from refactored DTO
    const numero = vehiculo.numeroVehiculo || vehiculo.vehiculoStr?.split('#').pop() || '??';
    const placa = placaDesdeVehiculoStr(vehiculo.vehiculoStr);
    const mostrarInstructor =
        vehiculo.instructorNombre &&
        vehiculo.instructorNombre.trim() !== '' &&
        vehiculo.instructorNombre.trim().toUpperCase() !== 'DOCENTE ASIGNADO';
    
    // License-based color themes & icons
    // C: 1, 3 | D: 4 | E: 5
    const getLicenseTheme = (id) => {
        switch(id) {
            case 4: return { name: 'D', color: '#f59e0b', bg: 'bg-amber-500/10', border: 'border-amber-500/20', text: 'text-amber-600', icon: 'M8 7h8M8 11h8M5 15h14M4 19h16l-1.5-6h-13L4 19zm0 0v-2' }; // Bus
            case 5: return { name: 'E', color: '#6366f1', bg: 'bg-indigo-500/10', border: 'border-indigo-500/20', text: 'text-indigo-600', icon: 'M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0zM13 13Va2 2 0 012-2h2a2 2 0 012 2v2H13v-2zM4 11V6a2 2 0 012-2h4M4 11l4 0M10 4l0 7' }; // Truck
            default: return { name: 'C', color: '#0ea5e9', bg: 'bg-sky-500/10', border: 'border-sky-500/20', text: 'text-sky-600', icon: 'M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z' }; // Car
        }
    };

    const theme = getLicenseTheme(vehiculo.idTipoLicencia);
    const isOperativo = true;

    return (
        <div
            id={`veh-card-${vehiculo.idVehiculo}`}
            onClick={() => isOperativo ? onSelect(vehiculo) : null}
            className={`
                group relative p-4 rounded-[2rem] border-2 transition-all duration-500 cursor-pointer overflow-hidden
                flex flex-col items-center justify-center min-h-[100px]
                ${isSelected
                    ? `bg-white border-[${theme.color}] shadow-xl shadow-[${theme.color}]/20 scale-[1.02] z-10`
                    : `bg-white border-[var(--apple-border)] hover:border-[${theme.color}]/50 hover:shadow-lg hover:-translate-y-1`
                }
            `}
            style={isSelected ? { borderColor: theme.color, boxShadow: `0 15px 30px -10px ${theme.color}40` } : {}}
        >
            {/* Badge de Licencia */}
            <div className={`absolute top-3 left-3 px-2 py-0.5 rounded-lg text-[7px] font-black uppercase tracking-widest ${theme.bg} ${theme.text}`}>
                Licencia {theme.name}
            </div>

            {/* Icono de Vehículo con degradado de fondo */}
            <div className={`mb-2 p-3 rounded-2xl transition-all duration-500 ${isSelected ? 'scale-110' : 'group-hover:scale-105'}`}
                 style={{ backgroundColor: isSelected ? theme.color : `${theme.color}10`, color: isSelected ? 'white' : theme.color }}>
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                    <path strokeLinecap="round" strokeLinejoin="round" d={theme.icon} />
                </svg>
            </div>

            <div className="text-center">
                <span className={`block text-xl font-black tracking-tight transition-colors ${isSelected ? 'text-[var(--apple-text-main)]' : 'text-[var(--apple-text-main)]'}`}>
                    #{numero}
                </span>
                <p className={`text-[10px] font-bold uppercase tracking-widest leading-none mt-1 opacity-60`}>
                    {placa}
                </p>
                {mostrarInstructor && (
                    <div className="mt-3 pt-2 border-t border-[var(--apple-border)]/50 w-full">
                         <p className="text-[6px] font-black text-[var(--apple-text-sub)] uppercase tracking-wider truncate max-w-[80px]">
                            {vehiculo.instructorNombre}
                        </p>
                    </div>
                )}
            </div>

            {/* Check de Selección */}
            {isSelected && (
                <div className="absolute top-3 right-3 h-5 w-5 rounded-full flex items-center justify-center text-white scale-110 animate-apple-in shadow-sm"
                     style={{ backgroundColor: theme.color }}>
                    <svg className="h-2.5 w-2.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                </div>
            )}
        </div>
    );
};

export default VehicleCard;
