import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import Layout from '../components/layout/Layout';
import LogisticaHeader from '../components/logistica/LogisticaHeader';
import logisticaService from '../services/logisticaService';
import dashboardService from '../services/dashboardService';
import StatusBadge from '../components/common/StatusBadge';
import VehicleCard from '../components/logistica/VehicleCard';

const ControlOperativo = () => {
    const [searchParams] = useSearchParams();
    const [activeTab, setActiveTab] = useState(searchParams.get('tab') || 'salida');
    const [notification, setNotification] = useState(null);

    // Sync activeTab con URL params
    useEffect(() => {
        const tab = searchParams.get('tab');
        if (tab && (tab === 'salida' || tab === 'llegada')) {
            setActiveTab(tab);
        }
    }, [searchParams]);

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
    const [horaRetorno, setHoraRetorno] = useState('');
    const [agendadosHoy, setAgendadosHoy] = useState([]);
    const [agendadosLoading, setAgendadosLoading] = useState(false);

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
            cargarAgendadosHoy();
        } else {
            cargarClasesActivas();
        }
    }, [activeTab]);

    const cargarAgendadosHoy = async () => {
        setAgendadosLoading(true);
        try {
            const data = await logisticaService.getAgendadosHoy();
            setAgendadosHoy(data);
        } catch (e) {
            console.error(e);
        } finally {
            setAgendadosLoading(false);
        }
    };

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
        if (!claseSeleccionada) {
            showNotification('Seleccione un vehículo en pista', 'error');
            return;
        }
        try {
            await logisticaService.registrarLlegada(claseSeleccionada.id_Registro);
            showNotification('¡Llegada confirmada!');
            setClaseSeleccionada(null);
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

                <div className="mb-10 lg:mb-16 text-center animate-apple-in">
                    <h1 className="text-4xl lg:text-6xl font-black tracking-tighter text-slate-900 mb-2 lg:mb-4 bg-clip-text text-transparent bg-gradient-to-b from-slate-900 to-slate-600 uppercase">
                        Logística Operativa
                    </h1>
                    <p className="text-slate-500 font-medium text-sm lg:text-xl tracking-tight opacity-70 italic">Despacho y Seguimiento de Unidades en Pista</p>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-8 items-start">

                    {/* Formulario Principal (Izquierda) */}
                    <div className="lg:col-span-7 xl:col-span-8 space-y-6 lg:space-y-8 animate-apple-in" style={{ animationDelay: '0.2s' }}>

                        {activeTab === 'salida' ? (
                            <div className="apple-card overflow-hidden">
                                <div className="flex items-center justify-between mb-10">
                                    <h3 className="text-2xl font-black text-slate-800 tracking-tight">Registro de Salida</h3>
                                    <div className="flex gap-2 items-center text-xs font-black text-slate-400 tracking-widest uppercase">
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
                                            <div className="flex flex-col lg:flex-row items-center lg:items-start gap-4 lg:gap-6 text-center lg:text-left">
                                                {estudianteData.fotoBase64 ? (
                                                    <div className="h-24 w-24 rounded-2xl overflow-hidden shadow-lg border-2 border-white flex-shrink-0">
                                                        <img
                                                            src={`data:image/jpeg;base64,${estudianteData.fotoBase64}`}
                                                            alt="Alumno"
                                                            className="w-full h-full object-cover"
                                                        />
                                                    </div>
                                                ) : (
                                                    <div className="h-16 w-16 rounded-2xl bg-blue-600 flex items-center justify-center text-white text-2xl font-black text-shadow-sm shadow-lg shadow-blue-500/30 flex-shrink-0">
                                                        {estudianteData.estudianteNombre?.[0]}
                                                    </div>
                                                )}
                                                <div className="flex-1">
                                                    <h4 className="text-lg lg:text-xl font-black text-slate-900 leading-tight mb-1">{estudianteData.estudianteNombre}</h4>
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

                                            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                                                <div className="bg-white/80 p-4 rounded-2xl border border-blue-200/50 shadow-sm">
                                                    <p className="text-[9px] font-black text-slate-400 uppercase tracking-[0.15em] leading-none mb-1.5">Licencia</p>
                                                    <p className="text-sm lg:text-base font-black text-blue-700">TIPO {estudianteData.tipoLicencia}</p>
                                                </div>
                                                <div className="bg-white/80 p-4 rounded-2xl border border-blue-200/50 shadow-sm">
                                                    <p className="text-[9px] font-black text-slate-400 uppercase tracking-[0.15em] leading-none mb-1.5">Paralelo</p>
                                                    <p className="text-sm lg:text-base font-black text-slate-700">"{estudianteData.paralelo}"</p>
                                                </div>
                                                <div className="bg-white/80 p-4 rounded-2xl border border-blue-200/50 shadow-sm col-span-2 sm:col-span-1">
                                                    <p className="text-[10px] font-black text-slate-400 uppercase tracking-[0.15em] leading-none mb-1.5">Jornada</p>
                                                    <p className="text-sm lg:text-base font-black text-slate-700">{estudianteData.jornada}</p>
                                                </div>
                                            </div>

                                            {/* ALERTA DE AGENDAMIENTO PROACTIVO */}
                                            {estudianteData.tienePracticaHoy && (
                                                <div className="bg-gradient-to-r from-blue-600 to-indigo-600 p-6 rounded-2xl text-white shadow-xl shadow-blue-600/20 flex flex-col md:flex-row justify-between items-center gap-4 border border-blue-400/30">
                                                    <div className="flex items-center gap-4 text-center md:text-left">
                                                        <div className="h-12 w-12 rounded-xl bg-white/20 backdrop-blur-md flex items-center justify-center">
                                                            <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                        </div>
                                                        <div>
                                                            <p className="text-[10px] font-black uppercase tracking-widest text-blue-100/80 mb-0.5">Práctica Agendada Central</p>
                                                            <p className="text-sm font-bold">{estudianteData.practicaVehiculo} • {estudianteData.practicaHora || '--:--'} • {estudianteData.practicaInstructor}</p>
                                                        </div>
                                                    </div>
                                                    <button
                                                        onClick={() => {
                                                            const veh = vehiculos.find(v => v.idVehiculo === estudianteData.idPracticaCentral || v.vehiculoStr.includes(estudianteData.practicaVehiculo));
                                                            const inst = instructores.find(i => i.fullName.includes(estudianteData.practicaInstructor?.split(' ')[0] || '---'));
                                                            if (veh) setVehiculoSeleccionado(veh);
                                                            if (inst) setInstructorSeleccionado(inst);
                                                            showNotification('Datos de agenda aplicados');
                                                        }}
                                                        className="px-6 py-2 bg-white text-blue-600 rounded-xl text-xs font-black uppercase shadow-lg shadow-black/10 hover:bg-blue-50 transition-all active:scale-95"
                                                    >
                                                        Cargar Datos
                                                    </button>
                                                </div>
                                            )}
                                        </div>
                                    ) : !salidaLoading && salidaCedula.length >= 1 && (
                                        <div className="p-8 border-2 border-dashed border-slate-100 rounded-[2rem] text-center">
                                            <p className="text-slate-400 font-bold text-sm tracking-tight">Ingrese una cédula válida para cargar datos académicos</p>
                                        </div>
                                    )}

                                    <div className="pt-6">
                                        <div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-8 px-2 gap-4">
                                            <div className="flex flex-col">
                                                <h3 className="text-xl font-black text-slate-800 tracking-tight leading-none mb-1">Selección de Vehículo</h3>
                                                <p className="text-[10px] font-black text-slate-400 uppercase tracking-widest">
                                                    Escoja una unidad operativa
                                                </p>
                                            </div>

                                            {/* Selector de Licencia Estilo Toggle Premium */}
                                            <div className="flex bg-slate-100 p-1 rounded-2xl border border-slate-200 shadow-inner">
                                                {['C', 'D', 'E'].map(lic => (
                                                    <button
                                                        key={lic}
                                                        onClick={() => {/* El filtro es automático por el alumno, pero aquí se muestra el estado */ }}
                                                        disabled={estudianteData && estudianteData.tipoLicencia !== lic}
                                                        className={`px-4 py-1.5 rounded-xl text-[10px] font-black transition-all ${estudianteData?.tipoLicencia === lic
                                                            ? 'bg-white text-blue-600 shadow-sm ring-1 ring-slate-200'
                                                            : 'text-slate-400 opacity-40'
                                                            }`}
                                                    >
                                                        TIPO {lic}
                                                    </button>
                                                ))}
                                            </div>
                                        </div>

                                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
                                            {vehiculos.length > 0 ? (
                                                (() => {
                                                    const filteredVehicles = estudianteData
                                                        ? vehiculos.filter(v => v.idTipoLicencia <= estudianteData.idTipoLicencia)
                                                        : vehiculos;

                                                    if (filteredVehicles.length === 0) {
                                                        return (
                                                            <div className="col-span-full py-10 px-8 text-center bg-blue-50/30 rounded-[2rem] border-2 border-dashed border-blue-100">
                                                                <p className="text-blue-900/40 text-xs font-black uppercase tracking-widest">
                                                                    Sin unidades "{estudianteData?.tipoLicencia}" disponibles
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
                                                <div className="col-span-full py-20 text-center apple-glass rounded-[2rem] opacity-30">
                                                    <svg className="animate-spin h-8 w-8 text-blue-500 mx-auto mb-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                                                    <p className="text-slate-400 font-bold text-xs uppercase tracking-widest">Conectando con Flota...</p>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {/* Selección de Instructor */}
                                    <div className="pt-6 animate-apple-in">
                                        <div className="flex items-center justify-between mb-6">
                                            <h3 className="text-xl font-black text-slate-800 tracking-tight">Asignación de Instructor</h3>
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

                                    <div className="pt-10 flex flex-row items-stretch justify-between gap-2 sm:gap-4 border-t border-slate-100 h-24 sm:h-32">
                                        <div className="flex-1 flex items-stretch min-w-0">
                                            <div className="w-full flex items-center gap-2 sm:gap-6 bg-white border-2 border-blue-50 px-3 sm:px-8 rounded-[2.5rem] sm:rounded-[4rem] shadow-xl group transition-all hover:shadow-2xl relative overflow-hidden">
                                                <div className="absolute top-0 right-0 w-24 h-24 bg-blue-50 rounded-full translate-x-12 -translate-y-12 opacity-50"></div>

                                                <div className="relative">
                                                    <div className="h-10 w-10 sm:h-12 sm:w-12 rounded-2xl bg-blue-600 flex items-center justify-center text-white shadow-lg shadow-blue-600/30 group-hover:rotate-12 transition-transform duration-500">
                                                        <svg className="h-5 w-5 sm:h-6 sm:w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                    </div>
                                                    <div className="absolute -bottom-1 -right-1 w-3 h-3 sm:w-4 sm:h-4 bg-emerald-500 rounded-full border-4 border-white animate-pulse shadow-sm"></div>
                                                </div>

                                                <div className="flex flex-col relative z-10">
                                                    <span className="text-[9px] font-black text-blue-600/60 uppercase tracking-[0.25em] leading-none mb-1.5">Salida</span>
                                                    <div className="flex items-baseline gap-1.5 sm:gap-3">
                                                        <span className="text-xl sm:text-4xl font-black tracking-tighter leading-none text-slate-900">{horaRetorno}</span>
                                                        <span className="text-[10px] sm:text-xs font-black text-blue-600 bg-blue-50 px-2 sm:px-3 py-1 rounded-lg uppercase tracking-widest">{new Date().toLocaleDateString('es-ES', { weekday: 'short' })}</span>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                        <button
                                            onClick={procesarSalida}
                                            disabled={!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado}
                                            className={`flex-shrink-0 btn-apple-primary px-6 sm:px-12 text-sm sm:text-xl font-black tracking-tight rounded-[2.5rem] sm:rounded-[4rem] ${(!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado) && 'opacity-30 grayscale cursor-not-allowed transform-none shadow-none text-slate-400'}`}>
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
                                                        <div className="px-2 py-1 bg-white border border-slate-200 rounded-lg text-xs font-black text-slate-800">#{c.numeroVehiculo || c.numero_vehiculo}</div>
                                                        <StatusBadge status="En Pista" />
                                                    </div>
                                                    <h5 className="font-bold text-slate-800 leading-tight uppercase mb-1">{c.instructor}</h5>
                                                    <p className="text-xs text-slate-500 font-medium truncate mb-4">Estudiante: {c.estudiante || 'Cargando...'}</p>
                                                    <div className="flex items-center gap-3 text-[10px] font-black text-slate-400 tracking-widest uppercase">
                                                        <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                        Salida: {new Date(c.salida).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
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
                                            <div className="flex flex-row items-stretch justify-between gap-2 sm:gap-4 w-full h-24 sm:h-32">
                                                <div className="flex-1 flex items-stretch min-w-0">
                                                    <div className="w-full flex items-center gap-2 sm:gap-6 bg-white border-2 border-emerald-50 px-3 sm:px-8 rounded-[2.5rem] sm:rounded-[4rem] shadow-xl group transition-all hover:shadow-2xl relative overflow-hidden">
                                                        <div className="absolute top-0 right-0 w-24 h-24 bg-emerald-50 rounded-full translate-x-12 -translate-y-12 opacity-50"></div>

                                                        <div className="relative">
                                                            <div className="h-10 w-10 sm:h-12 sm:w-12 rounded-2xl bg-emerald-600 flex items-center justify-center text-white shadow-lg shadow-emerald-600/30 group-hover:rotate-12 transition-transform duration-500">
                                                                <svg className="h-5 w-5 sm:h-6 sm:w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                            </div>
                                                            <div className="absolute -bottom-1 -right-1 w-3 h-3 sm:w-4 sm:h-4 bg-blue-500 rounded-full border-4 border-white animate-pulse shadow-sm"></div>
                                                        </div>

                                                        <div className="flex flex-col relative z-10">
                                                            <span className="text-[9px] font-black text-emerald-600/60 uppercase tracking-[0.25em] leading-none mb-1.5">Retorno (Live)</span>
                                                            <div className="flex items-baseline gap-1.5 sm:gap-3">
                                                                <span className="text-xl sm:text-4xl font-black tracking-tighter leading-none text-slate-900">{new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                                                                <span className="text-[10px] sm:text-xs font-black text-emerald-600 bg-emerald-50 px-2 sm:px-3 py-1 rounded-lg uppercase tracking-widest">{new Date().toLocaleDateString('es-ES', { weekday: 'short' })}</span>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                                <button
                                                    onClick={procesarLlegada}
                                                    className="flex-shrink-0 bg-amber-500 hover:bg-amber-600 text-white rounded-[2.5rem] sm:rounded-[4rem] px-6 sm:px-12 text-sm sm:text-xl font-black tracking-tight shadow-lg shadow-amber-500/30 transition-all hover:-translate-y-1 active:scale-95">
                                                    Confirmar Retorno
                                                </button>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>

                    {/* Dashboard Lateral / Info (Derecha) */}
                    <div className="lg:col-span-5 xl:col-span-4 space-y-8 animate-apple-in" style={{ animationDelay: '0.3s' }}>

                        {/* Widget de Resumen */}
                        <div className="apple-glass rounded-[2.5rem] p-8 border-white/60 relative overflow-hidden group">
                            <div className="absolute top-0 right-0 w-32 h-32 bg-blue-500/5 rounded-full -translate-y-12 translate-x-12 blur-3xl group-hover:bg-blue-500/10 transition-colors duration-700"></div>

                            <h4 className="text-xs font-black text-slate-400 tracking-[0.2em] uppercase mb-8">Agenda ISTPET (SIGAFI)</h4>

                            <div className="space-y-4 max-h-[400px] overflow-y-auto pr-2 custom-scrollbar">
                                {agendadosHoy.length > 0 ? (
                                    agendadosHoy.map(ag => (
                                        <div key={ag.idPractica} className="bg-white/40 p-4 rounded-2xl border border-white/40 hover:bg-white/60 transition-all group/item">
                                            <div className="flex justify-between items-start mb-2">
                                                <span className="text-xs font-black text-blue-600">{ag.horaSalida ? ag.horaSalida.substring(0, 5) : '--:--'}</span>
                                                <span className="text-[9px] font-black bg-slate-200 text-slate-500 px-1.5 py-0.5 rounded uppercase">{ag.vehiculoDetalle}</span>
                                            </div>
                                            <p className="text-[11px] font-bold text-slate-800 uppercase truncate">{ag.cedulaAlumno}</p>
                                            <p className="text-[9px] font-black text-slate-400 uppercase tracking-tighter truncate">{ag.profesorNombre}</p>
                                            <button
                                                onClick={() => setSalidaCedula(ag.cedulaAlumno)}
                                                className="mt-3 w-full py-1.5 bg-blue-500/10 text-blue-600 rounded-lg text-[10px] font-black uppercase tracking-widest opacity-0 group-hover/item:opacity-100 transition-all hover:bg-blue-500 hover:text-white"
                                            >
                                                Cargar Cédula
                                            </button>
                                        </div>
                                    ))
                                ) : agendadosLoading ? (
                                    <div className="py-10 text-center">
                                        <div className="animate-spin h-5 w-5 border-2 border-blue-500 border-t-transparent rounded-full mx-auto"></div>
                                    </div>
                                ) : (
                                    <div className="py-10 text-center opacity-40">
                                        <p className="text-[10px] font-bold text-slate-500 uppercase tracking-widest">Sin agendas para hoy</p>
                                    </div>
                                )}
                            </div>
                        </div>



                    </div>
                </div>
            </div>
        </Layout>
    );
};

export default ControlOperativo;
