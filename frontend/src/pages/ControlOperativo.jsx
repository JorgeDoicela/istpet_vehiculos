import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import logisticaService from '../services/logisticaService';
import dashboardService from '../services/dashboardService';
import StatusBadge from '../components/common/StatusBadge';
import VehicleCard from '../components/logistica/VehicleCard';

const ControlOperativo = () => {
    const [activeTab, setActiveTab] = useState('salida'); // salida | llegada
    const [notification, setNotification] = useState(null);

    // --- Estado Salida ---
    const [salidaCedula, setSalidaCedula] = useState('');
    const [salidaLoading, setSalidaLoading] = useState(false);
    const [estudianteData, setEstudianteData] = useState(null);
    const [vehiculos, setVehiculos] = useState([]);
    const [vehiculoSeleccionado, setVehiculoSeleccionado] = useState(null);
    const [instructores, setInstructores] = useState([]);
    const [instructorSeleccionado, setInstructorSeleccionado] = useState(null);

    // --- Estado Llegada ---
    const [clasesActivas, setClasesActivas] = useState([]);
    const [claseSeleccionada, setClaseSeleccionada] = useState(null);
    const [kmLlegada, setKmLlegada] = useState('');
    const [horaRetorno, setHoraRetorno] = useState('');

    const showNotification = (message, type = 'success') => {
        setNotification({ message, type });
        setTimeout(() => setNotification(null), 4000);
    };

    // Reloj para Hora Retorno (Llegada)
    useEffect(() => {
        const clockInt = setInterval(() => {
            const now = new Date();
            const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            setHoraRetorno(timeStr);
        }, 1000);
        return () => clearInterval(clockInt);
    }, []);

    // Carga de datos base
    useEffect(() => {
        if (activeTab === 'salida') {
            cargarVehiculosDisponibles();
            cargarInstructores();
        } else {
            cargarClasesActivas();
        }
    }, [activeTab]);

    const cargarInstructores = async () => {
        try {
            const data = await logisticaService.getInstructores();
            setInstructores(data);
        } catch (e) {
            showNotification('Error cargando catálogo de instructores', 'error');
        }
    };

    // Autobúsqueda de estudiante (Debounce)
    useEffect(() => {
        const timer = setTimeout(() => {
            if (salidaCedula.length >= 10 && activeTab === 'salida') {
                ejecutarBusquedaEstudiante();
            }
        }, 600);
        return () => clearTimeout(timer);
    }, [salidaCedula]);

    const cargarVehiculosDisponibles = async () => {
        try {
            const data = await logisticaService.getVehiculosDisponibles();
            setVehiculos(data);
        } catch (e) {
            showNotification('Error cargando flota', 'error');
        }
    };

    const cargarClasesActivas = async () => {
        try {
            const data = await dashboardService.getClasesActivas();
            setClasesActivas(data);
        } catch (e) {
            showNotification('Error cargando vehículos en pista', 'error');
        }
    };

    const ejecutarBusquedaEstudiante = async () => {
        setSalidaLoading(true);
        setEstudianteData(null);
        try {
            const data = await logisticaService.buscarEstudiante(salidaCedula);
            setEstudianteData(data);
            showNotification('Estudiante localizado');
        } catch (err) {
            showNotification(err.message || 'No localizado', 'error');
        } finally {
            setSalidaLoading(false);
        }
    };

    const handleSeleccionarVehiculo = (veh) => {
        setVehiculoSeleccionado(veh);
        // Desacoplamiento Total: Ya no se pre-selecciona el instructor fijo.
        // El usuario DEBE elegirlo manualmente del dropdown en el Paso 3.
    };

    const procesarSalida = async () => {
        if (!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado) {
            showNotification('Faltan datos (Estudiante, Vehículo o Instructor)', 'error');
            return;
        }
        try {
            await logisticaService.registrarSalida(
                estudianteData.idMatricula,
                vehiculoSeleccionado.idVehiculo,
                instructorSeleccionado.id_Instructor
            );
            showNotification('¡Vehículo en pista registrado!');
            setEstudianteData(null);
            setSalidaCedula('');
            setVehiculoSeleccionado(null);
            setInstructorSeleccionado(null);
            cargarVehiculosDisponibles();
        } catch (err) {
            showNotification(err.message, 'error');
        }
    };

    const procesarLlegada = async () => {
        if (!claseSeleccionada || !kmLlegada) {
            showNotification('Seleccione un vehículo y escriba el KM de llegada', 'error');
            return;
        }
        try {
            await logisticaService.registrarLlegada(claseSeleccionada.id_Registro, kmLlegada);
            showNotification('¡Llegada confirmada!');
            setClaseSeleccionada(null);
            setKmLlegada('');
            cargarClasesActivas();
        } catch (err) {
            showNotification(err.message, 'error');
        }
    };

    return (
        <Layout>
            {notification && (
                <div className="apple-toast border-white border animate-apple-in">
                    <div className={`w-1.5 h-6 rounded-full ${notification.type === 'error' ? 'bg-rose-500' : 'bg-[var(--apple-primary)]'}`}></div>
                    <p className="text-sm font-bold text-slate-800">{notification.message}</p>
                </div>
            )}

            <div className="max-w-6xl mx-auto pt-10 pb-20 px-6">
                
                {/* Header Premium */}
                <div className="mb-12 text-center animate-apple-in">
                    <h1 className="text-5xl font-black tracking-tighter text-slate-900 mb-4 bg-clip-text text-transparent bg-gradient-to-b from-slate-900 to-slate-600">
                        Logística Operativa
                    </h1>
                    <p className="text-slate-500 font-medium text-lg">Control de flota y gestión académica en tiempo real</p>
                </div>

                {/* Tabs Estilo Zen */}
                <div className="flex justify-center mb-16 animate-apple-in" style={{ animationDelay: '0.1s' }}>
                    <div className="bg-slate-200/50 backdrop-blur-xl p-1.5 rounded-full flex gap-1 border border-white/40 shadow-inner">
                        <button 
                            onClick={() => setActiveTab('salida')} 
                            className={`px-10 py-3 rounded-full text-sm font-bold transition-all duration-500 ${activeTab === 'salida' ? 'bg-white text-blue-600 shadow-xl scale-100' : 'text-slate-500 hover:text-slate-700 hover:bg-white/40 scale-95 opacity-70'}`}>
                            Salida a Pista
                        </button>
                        <button 
                            onClick={() => setActiveTab('llegada')} 
                            className={`px-10 py-3 rounded-full text-sm font-bold transition-all duration-500 ${activeTab === 'llegada' ? 'bg-white text-blue-600 shadow-xl scale-100' : 'text-slate-500 hover:text-slate-700 hover:bg-white/40 scale-95 opacity-70'}`}>
                            Llegada de Vehículo
                        </button>
                    </div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
                    
                    {/* Formulario Principal (Izquierda) */}
                    <div className="lg:col-span-12 xl:col-span-7 space-y-8 animate-apple-in" style={{ animationDelay: '0.2s' }}>
                        
                        {activeTab === 'salida' ? (
                            <div className="apple-card overflow-hidden">
                                <div className="flex items-center justify-between mb-10">
                                    <h3 className="text-2xl font-black text-slate-800 tracking-tight">Registro de Salida</h3>
                                    <div className="flex gap-2 items-center text-xs font-black text-slate-400 tracking-widest uppercase">
                                        <div className="w-2 h-2 rounded-full bg-blue-500 animate-pulse"></div>
                                        Paso 1: Identificación
                                    </div>
                                </div>

                                {/* Búsqueda Estudiante */}
                                <div className="space-y-6">
                                    <div className="relative group">
                                        <label className="absolute left-6 -top-3 px-2 bg-white text-[10px] font-black text-blue-500 tracking-[0.2em] uppercase transition-all group-focus-within:text-blue-600">
                                            Cédula del Estudiante
                                        </label>
                                        <div className="flex items-center gap-4">
                                            <input 
                                                type="text" 
                                                placeholder="Ej. 1725555377"
                                                maxLength={10}
                                                value={salidaCedula}
                                                onChange={(e) => setSalidaCedula(e.target.value)}
                                                className="w-full bg-slate-50/50 border-2 border-slate-100 rounded-3xl px-8 py-5 text-xl font-bold text-slate-800 focus:border-blue-500 focus:bg-white outline-none transition-all shadow-inner" 
                                            />
                                            {salidaLoading && (
                                                <div className="absolute right-6">
                                                    <svg className="animate-spin h-6 w-6 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {/* Data Estudiante (Solo si existe) */}
                                    {estudianteData ? (
                                        <div className="bg-blue-50/50 border border-blue-100 rounded-[2rem] p-8 space-y-6 transition-all animate-apple-in">
                                            <div className="flex items-start gap-6">
                                                <div className="h-16 w-16 rounded-2xl bg-blue-600 flex items-center justify-center text-white text-2xl font-black text-shadow-sm shadow-lg shadow-blue-500/30">
                                                    {estudianteData.estudianteNombre?.[0]}
                                                </div>
                                                <div className="flex-1">
                                                    <h4 className="text-xl font-black text-slate-900 leading-tight mb-1">{estudianteData.estudianteNombre}</h4>
                                                    <div className="flex items-center gap-2 flex-wrap">
                                                        <span className="text-[10px] font-black bg-blue-100 text-blue-700 px-2 py-0.5 rounded-md uppercase tracking-wider">
                                                            {estudianteData.cursoDetalle}
                                                        </span>
                                                        <span className="text-[10px] font-black bg-slate-100 text-slate-500 px-2 py-0.5 rounded-md uppercase tracking-wider">
                                                            PERIODO: {estudianteData.periodo}
                                                        </span>
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
                                                <div className="bg-white/80 p-4 rounded-2xl border border-blue-200/50 shadow-sm">
                                                    <p className="text-[9px] font-black text-slate-400 uppercase tracking-[0.15em] leading-none mb-1.5">Licencia</p>
                                                    <p className="text-base font-black text-blue-700">TIPO {estudianteData.tipoLicencia}</p>
                                                </div>
                                                <div className="bg-white/80 p-4 rounded-2xl border border-blue-200/50 shadow-sm">
                                                    <p className="text-[9px] font-black text-slate-400 uppercase tracking-[0.15em] leading-none mb-1.5">Paralelo</p>
                                                    <p className="text-base font-black text-slate-700">"{estudianteData.paralelo}"</p>
                                                </div>
                                                <div className="bg-white/80 p-4 rounded-2xl border border-blue-200/50 shadow-sm col-span-2 sm:col-span-1">
                                                    <p className="text-[9px] font-black text-slate-400 uppercase tracking-[0.15em] leading-none mb-1.5">Jornada</p>
                                                    <p className="text-base font-black text-slate-700">{estudianteData.jornada}</p>
                                                </div>
                                            </div>
                                        </div>
                                    ) : !salidaLoading && salidaCedula.length >= 1 && (
                                        <div className="p-8 border-2 border-dashed border-slate-100 rounded-[2rem] text-center">
                                            <p className="text-slate-400 font-bold text-sm tracking-tight">Ingrese una cédula válida para cargar datos académicos</p>
                                        </div>
                                    )}

                                    <div className="pt-6">
                                        <div className="flex items-center justify-between mb-6">
                                            <h3 className="text-xl font-black text-slate-800 tracking-tight">Selección de Vehículo</h3>
                                            <span className="text-[10px] font-black text-slate-400 tracking-[0.2em] uppercase">Paso 2: Asignación Flota</span>
                                        </div>
                                        
                                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                            {vehiculos.length > 0 ? (
                                                (() => {
                                                    const filteredVehicles = estudianteData 
                                                        ? vehiculos.filter(v => v.idTipoLicencia <= estudianteData.idTipoLicencia)
                                                        : vehiculos;

                                                    if (filteredVehicles.length === 0) {
                                                        return (
                                                            <div className="col-span-full py-8 px-6 text-center bg-amber-50 rounded-3xl border border-amber-100">
                                                                <p className="text-amber-800 text-xs font-black uppercase tracking-widest leading-relaxed">
                                                                    No hay vehículos disponibles tipo "{estudianteData?.tipoLicencia}" operando actualmente.
                                                                </p>
                                                            </div>
                                                        );
                                                    }

                                                    return filteredVehicles.map(v => (
                                                        <VehicleCard 
                                                            key={v.idVehiculo} 
                                                            vehiculo={v} 
                                                            isSelected={vehiculoSeleccionado?.idVehiculo === v.idVehiculo}
                                                            onSelect={handleSeleccionarVehiculo}
                                                        />
                                                    ));
                                                })()
                                            ) : (
                                                <div className="col-span-full py-12 text-center apple-glass rounded-3xl opacity-50">
                                                    <p className="text-slate-400 font-bold">Cargando flota disponible...</p>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {/* Selección de Instructor */}
                                    <div className="pt-6 animate-apple-in">
                                        <div className="flex items-center justify-between mb-6">
                                            <h3 className="text-xl font-black text-slate-800 tracking-tight">Asignación de Instructor</h3>
                                            <span className="text-[10px] font-black text-slate-400 tracking-[0.2em] uppercase">Paso 3: Personal Autorizado</span>
                                        </div>
                                        
                                        <div className="relative group">
                                            <label className="absolute left-6 -top-3 px-2 bg-white text-[9px] font-black text-emerald-600 tracking-[0.2em] uppercase z-10 transition-all group-focus-within:text-emerald-700">
                                                Instructor de Clase
                                            </label>
                                            <select 
                                                value={instructorSeleccionado?.id_Instructor || ''}
                                                onChange={(e) => {
                                                    const inst = instructores.find(i => i.id_Instructor.toString() === e.target.value);
                                                    setInstructorSeleccionado(inst);
                                                }}
                                                className="w-full bg-slate-50/50 border-2 border-slate-100 rounded-2xl px-8 py-5 text-sm font-bold text-slate-700 focus:border-emerald-500 focus:bg-white outline-none transition-all shadow-inner appearance-none cursor-pointer"
                                            >
                                                <option value="" disabled>-- SELECCIONE DOCENTE --</option>
                                                {instructores.map(i => (
                                                    <option key={i.id_Instructor} value={i.id_Instructor}>
                                                        {i.fullName}
                                                    </option>
                                                ))}
                                            </select>
                                            <div className="absolute right-6 top-6 pointer-events-none text-slate-400">
                                                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" /></svg>
                                            </div>
                                        </div>

                                    </div>

                                    <div className="pt-10 flex justify-end gap-4 border-t border-slate-100">
                                         <div className="flex-1 flex flex-col justify-center">
                                            <div className="flex items-center gap-2 text-[10px] font-black text-slate-400 tracking-widest uppercase">
                                                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                Timestamp: {new Date().toLocaleDateString('es-ES', { weekday: 'short' })} {horaRetorno}
                                            </div>
                                         </div>
                                         <button 
                                            onClick={procesarSalida}
                                            disabled={!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado}
                                            className={`btn-apple-primary px-12 py-5 text-lg font-black tracking-tight ${(!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado) && 'opacity-30 grayscale cursor-not-allowed transform-none shadow-none text-slate-400'}`}>
                                            Confirmar Salida
                                         </button>
                                    </div>
                                </div>
                            </div>
                        ) : (
                            <div className="apple-card">
                                 <div className="flex items-center justify-between mb-10">
                                    <h3 className="text-2xl font-black text-slate-800 tracking-tight">Registro de Llegada</h3>
                                    <div className="flex gap-2 items-center text-xs font-black text-slate-400 tracking-widest uppercase">
                                        <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></div>
                                        Vehículos en Pista
                                    </div>
                                </div>

                                <div className="space-y-8">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                        {clasesActivas.length > 0 ? (
                                            clasesActivas.map(c => (
                                                <div 
                                                    key={c.id_Registro}
                                                    onClick={() => setClaseSeleccionada(c)}
                                                    className={`p-6 rounded-3xl border-2 transition-all cursor-pointer ${claseSeleccionada?.id_Registro === c.id_Registro ? 'bg-emerald-50 border-emerald-500 shadow-md ring-4 ring-emerald-100' : 'bg-slate-50/50 border-slate-100 hover:border-slate-300'}`}
                                                >
                                                    <div className="flex justify-between items-start mb-4">
                                                        <div className="px-2 py-1 bg-white border border-slate-200 rounded-lg text-xs font-black text-slate-800">#{c.numero_vehiculo}</div>
                                                        <StatusBadge status="En Pista" />
                                                    </div>
                                                    <h5 className="font-bold text-slate-800 leading-tight uppercase mb-1">{c.instructor}</h5>
                                                    <p className="text-xs text-slate-500 font-medium truncate mb-4">Estudiante: {c.estudiante || 'Cargando...'}</p>
                                                    <div className="flex items-center gap-3 text-[10px] font-black text-slate-400 tracking-widest uppercase">
                                                        <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                        Salida: {new Date(c.salida).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}
                                                    </div>
                                                </div>
                                            ))
                                        ) : (
                                            <div className="col-span-full py-16 text-center apple-glass rounded-[2.5rem] opacity-40">
                                                <p className="text-slate-400 font-bold">No hay vehículos registrados en pista actualmente.</p>
                                            </div>
                                        )}
                                    </div>

                                    {claseSeleccionada && (
                                        <div className="mt-8 p-8 bg-amber-50/70 border border-amber-200 rounded-[2.5rem] animate-apple-in">
                                            <div className="flex items-center gap-4 mb-8">
                                                <div className="h-12 w-12 rounded-2xl bg-amber-500/10 flex items-center justify-center text-amber-600">
                                                    <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04kM12 21.75c-3.176 0-6.156-1.257-8.384-3.535A11.961 11.961 0 012.25 12c0-3.176 1.258-6.156 3.535-8.384A11.961 11.961 0 0112 2.25c3.176 0 6.156 1.257 8.384 3.535A11.961 11.961 0 0121.75 12c0 3.176-1.257 6.156-3.535 8.384A11.961 11.961 0 0112 21.75z" /></svg>
                                                </div>
                                                <div>
                                                    <h6 className="text-amber-900 font-black tracking-tight leading-none mb-1">Cierre de Operación</h6>
                                                    <p className="text-amber-700/60 text-xs font-bold uppercase tracking-widest">Ingrese los detalles finales</p>
                                                </div>
                                            </div>

                                            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 items-end">
                                                <div className="space-y-3">
                                                    <label className="text-[10px] font-black text-amber-800 tracking-[0.2em] uppercase ml-4">KM Final de Llegada</label>
                                                    <div className="relative">
                                                        <input 
                                                            type="number" 
                                                            value={kmLlegada}
                                                            onChange={(e) => setKmLlegada(e.target.value)}
                                                            className="w-full bg-white border-2 border-amber-300 rounded-2xl px-8 py-4 text-2xl font-black text-amber-900 focus:border-amber-500 focus:ring-4 focus:ring-amber-200/50 outline-none transition-all shadow-inner"
                                                            placeholder="0"
                                                        />
                                                        <span className="absolute right-6 top-5 text-xs font-black text-amber-400">KM</span>
                                                    </div>
                                                </div>
                                                <button 
                                                    onClick={procesarLlegada}
                                                    className="w-full bg-amber-500 hover:bg-amber-600 text-white rounded-2xl py-5 text-lg font-black tracking-tight shadow-lg shadow-amber-500/30 transition-all hover:-translate-y-1 active:scale-95">
                                                    Registrar Llegada
                                                </button>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>

                    {/* Dashboard Lateral / Info (Derecha) */}
                    <div className="lg:col-span-12 xl:col-span-5 space-y-8 animate-apple-in" style={{ animationDelay: '0.3s' }}>
                        
                        {/* Widget de Resumen */}
                        <div className="apple-glass rounded-[2.5rem] p-8 border-white/60 relative overflow-hidden group">
                             <div className="absolute top-0 right-0 w-32 h-32 bg-blue-500/5 rounded-full -translate-y-12 translate-x-12 blur-3xl group-hover:bg-blue-500/10 transition-colors duration-700"></div>
                             
                             <h4 className="text-xs font-black text-slate-400 tracking-[0.2em] uppercase mb-8">Resumen de Operación</h4>
                             
                             <div className="space-y-6">
                                <div className="flex justify-between items-center bg-white/40 p-5 rounded-3xl border border-white/40">
                                    <div className="flex gap-4 items-center">
                                        <div className="h-10 w-10 rounded-xl bg-blue-100 flex items-center justify-center text-blue-600">
                                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" /></svg>
                                        </div>
                                        <p className="text-sm font-bold text-slate-700 uppercase tracking-tight">Salidas Hoy</p>
                                    </div>
                                    <span className="text-2xl font-black text-blue-600">08</span>
                                </div>

                                <div className="flex justify-between items-center bg-white/40 p-5 rounded-3xl border border-white/40">
                                    <div className="flex gap-4 items-center">
                                        <div className="h-10 w-10 rounded-xl bg-emerald-100 flex items-center justify-center text-emerald-600">
                                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                        </div>
                                        <p className="text-sm font-bold text-slate-700 uppercase tracking-tight">Retornos</p>
                                    </div>
                                    <span className="text-2xl font-black text-emerald-600">05</span>
                                </div>

                                <div className="flex justify-between items-center bg-white/40 p-5 rounded-3xl border border-white/40">
                                    <div className="flex gap-4 items-center">
                                        <div className="h-10 w-10 rounded-xl bg-amber-100 flex items-center justify-center text-amber-600">
                                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                        </div>
                                        <p className="text-sm font-bold text-slate-700 uppercase tracking-tight">Pendientes</p>
                                    </div>
                                    <span className="text-2xl font-black text-amber-600">03</span>
                                </div>
                             </div>

                             <div className="mt-10 pt-10 border-t border-slate-200/40">
                                 <p className="text-[10px] font-black text-blue-500 uppercase tracking-widest mb-4">Última Actividad</p>
                                 <div className="flex gap-4 items-start opacity-70">
                                     <div className="w-1 h-12 rounded-full bg-blue-200 mt-1"></div>
                                     <div>
                                         <p className="text-[10px] font-black text-slate-400">08:12 AM</p>
                                         <p className="text-xs font-bold text-slate-700 leading-tight">Salida registrada #12 - TRUJILLO REDROBAN</p>
                                     </div>
                                 </div>
                             </div>
                        </div>

                        {/* Banner Estilo Apple Card */}
                        <div className="bg-gradient-to-br from-indigo-600 to-blue-700 rounded-[2.5rem] p-8 text-white shadow-2xl relative overflow-hidden group">
                             <div className="absolute top-0 right-0 p-4 opacity-10 group-hover:scale-125 transition-transform duration-700">
                                <svg className="h-40 w-40" fill="currentColor" viewBox="0 0 24 24"><path d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z" /></svg>
                             </div>
                             <h5 className="text-2xl font-black tracking-tight mb-2">Flota en Pista</h5>
                             <p className="text-blue-100 text-sm font-medium leading-relaxed mb-6 opacity-80">Gestione eficientemente el flujo de vehículos y estudiantes de la institución ISTPET.</p>
                             <div className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 backdrop-blur-md rounded-full text-[10px] font-black uppercase tracking-widest">
                                <div className="w-1.5 h-1.5 rounded-full bg-emerald-400"></div>
                                Sistema Activo 2026
                             </div>
                        </div>

                    </div>
                </div>
            </div>
        </Layout>
    );
};

export default ControlOperativo;
