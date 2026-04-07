import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import Layout from '../components/layout/Layout';
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
    const [sugerencias, setSugerencias] = useState([]);
    const [mostrarSugerencias, setMostrarSugerencias] = useState(false);
    const [salidaLoading, setSalidaLoading] = useState(false);
    const [estudianteData, setEstudianteData] = useState(null);
    const [vehiculos, setVehiculos] = useState([]);
    const [filtroVehiculo, setFiltroVehiculo] = useState('');
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
            const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
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

    // Autocompletado y Sugerencias (Búsqueda Predictiva)
    useEffect(() => {
        const timer = setTimeout(async () => {
            // Si ya tenemos cargado al estudiante de esta cédula, no necesitamos mostrar sugerencias
            if (salidaCedula.length >= 3 && activeTab === 'salida' && estudianteData?.cedula !== salidaCedula) {
                // 1. Filtrado local ultra-rápido de agendados hoy
                const localeMatch = agendadosHoy.filter(ag =>
                    ag.cedulaAlumno.startsWith(salidaCedula) ||
                    ag.alumnoNombre?.toLowerCase().includes(salidaCedula.toLowerCase())
                ).map(ag => ({ 
                    cedula: ag.cedulaAlumno, 
                    nombreCompleto: ag.alumnoNombre, 
                    esAgendado: true,
                    hora: ag.horaSalida?.split(':').slice(0, 2).join(':'),
                    vehiculo: ag.vehiculoDetalle,
                    instructor: ag.profesorNombre
                }));

                // 2. Consulta al backend para alumnos históricos
                try {
                    const serverMatch = await logisticaService.buscarSugerencias(salidaCedula);

                    // Mezclamos y quitamos duplicados
                    const combined = [...localeMatch];
                    serverMatch.forEach(sm => {
                        if (!combined.find(c => c.cedula === sm.cedula)) {
                            combined.push(sm);
                        }
                    });

                    setSugerencias(combined.slice(0, 5));
                    setMostrarSugerencias(combined.length > 0);
                } catch (e) {
                    console.error(e);
                }
            } else {
                setSugerencias([]);
                setMostrarSugerencias(false);
            }
        }, 300); // Debounce corto para sugerencias
        return () => clearTimeout(timer);
    }, [salidaCedula, agendadosHoy, estudianteData]);

    // Autobúsqueda de estudiante DEFINITIVA (Full Load)
    useEffect(() => {
        const timer = setTimeout(() => {
            if (salidaCedula.length === 10 && activeTab === 'salida') {
                // EVITAR DOBLE CARGA: Si ya tenemos la data de esta cédula, no recargar
                if (estudianteData?.cedula === salidaCedula) return;
                
                setMostrarSugerencias(false);
                ejecutarBusquedaEstudiante();
            }
        }, 800);
        return () => clearTimeout(timer);
    }, [salidaCedula, estudianteData]);

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

    const ejecutarBusquedaEstudiante = async (cedulaEnviada = null) => {
        const cedulaABuscar = cedulaEnviada || salidaCedula;
        if (!cedulaABuscar || cedulaABuscar.length < 10) return;

        setSalidaLoading(true);
        setEstudianteData(null);
        try {
            const data = await logisticaService.buscarEstudiante(cedulaABuscar);
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
                <div className="apple-toast border border-white/10 animate-apple-in">
                    <div className={`w-1.5 h-6 rounded-full ${notification.type === 'error' ? 'bg-rose-500' : 'bg-[var(--apple-primary)]'}`}></div>
                    <p className="text-sm font-bold text-[var(--apple-text-main)]">{notification.message}</p>
                </div>
            )}

            <div className="max-w-6xl mx-auto pt-4 lg:pt-8 pb-20 px-6">
                <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-8 items-start">
                    {/* Panel Principal */}
                    <div className="lg:col-span-7 xl:col-span-8 space-y-6 lg:space-y-8 animate-apple-in" style={{ animationDelay: '0.2s' }}>

                        {activeTab === 'salida' ? (
                            <div className="apple-card overflow-hidden">
                                <div className="mb-8 px-2">
                                    <h3 className="text-lg lg:text-xl font-black text-[var(--apple-text-main)] mb-1">Registro de Salida</h3>
                                </div>

                                <div className="space-y-8">
                                    {/* Campo Cédula */}
                                    <div className="relative group">
                                        <label className="absolute left-6 -top-4 px-4 bg-[var(--apple-card)] backdrop-blur-md text-[10px] font-black text-[var(--istpet-gold)] tracking-[0.2em] uppercase transition-all group-focus-within:text-[var(--apple-primary)] z-10">
                                            Cédula del Estudiante
                                        </label>
                                        <div className="relative flex items-center gap-4">
                                            <input
                                                type="text"
                                                placeholder="Ej. 1722..."
                                                maxLength={10}
                                                value={salidaCedula}
                                                onChange={(e) => setSalidaCedula(e.target.value.replace(/\D/g, ''))}
                                                className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[2rem] px-6 lg:px-8 py-3 lg:py-5 text-lg lg:text-xl font-bold text-[var(--apple-text-main)] focus:border-[var(--apple-primary)] focus:bg-[var(--apple-card)] outline-none transition-all shadow-inner tracking-widest"
                                            />

                                            {/* GESTIÓN CONDUCCIÓN AUTO-COMPLETE POPUP */}
                                            {mostrarSugerencias && (
                                                <div className="absolute left-0 right-0 top-full mt-2 bg-white border border-[var(--apple-border)] rounded-[2rem] shadow-2xl z-[100] overflow-hidden animate-apple-in p-2">
                                                    {sugerencias.map((s, idx) => (
                                                        <div
                                                            key={s.cedula}
                                                            onClick={() => {
                                                                setSalidaCedula(s.cedula);
                                                                setMostrarSugerencias(false);
                                                                ejecutarBusquedaEstudiante(s.cedula);
                                                            }}
                                                            className={`p-4 cursor-pointer hover:bg-[var(--apple-primary)]/10 transition-all flex items-center justify-between rounded-2xl group/item ${idx !== sugerencias.length - 1 ? 'mb-1' : ''}`}
                                                        >
                                                            <div className="flex-1 text-left py-2">
                                                                <p className="text-[15px] font-black text-[#1a2544] uppercase tracking-tight leading-none mb-1.5">
                                                                    {s.nombreCompleto}
                                                                </p>
                                                                
                                                                <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                                                                    <p className="text-[11px] font-black text-[var(--apple-text-sub)] tracking-[0.2em]">
                                                                        {s.cedula}
                                                                    </p>
                                                                    
                                                                    {s.esAgendado ? (
                                                                        <div className="flex items-center gap-2">
                                                                            <span className="text-[10px] font-black text-emerald-700 flex items-center gap-1">
                                                                                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                                                {s.hora}
                                                                            </span>
                                                                            <span className="text-[9px] font-bold text-slate-500 uppercase">
                                                                                {s.vehiculo}
                                                                            </span>
                                                                            <span className="text-[9px] font-black bg-emerald-500 text-white px-2 py-0.5 rounded-full shadow-sm animate-pulse">
                                                                                HOY
                                                                            </span>
                                                                        </div>
                                                                    ) : (
                                                                        <span className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase">
                                                                            {s.carrera || 'ESTUDIANTE REGULAR'}
                                                                        </span>
                                                                    )}
                                                                </div>
                                                            </div>
                                                        </div>
                                                    ))}
                                                </div>
                                            )}

                                            {salidaLoading && (
                                                <div className="absolute right-6">
                                                    <svg className="animate-spin h-6 w-6 text-[var(--apple-primary)]" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {/* Información Estudiante */}
                                    {estudianteData ? (
                                        <div className="space-y-6 animate-apple-in">
                                            <div className="bg-white border border-[var(--apple-border)] rounded-[2.5rem] p-4 lg:p-5 shadow-sm hover:shadow-md transition-all group overflow-hidden relative">
                                                <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 lg:gap-8 relative z-10">
                                                    {/* Identidad */}
                                                    <div className="flex-1 min-w-0 text-left">
                                                        <div className="flex items-center gap-3 mb-0.5">
                                                            <h4 className="text-lg font-black text-[var(--apple-text-main)] uppercase tracking-tighter leading-none">
                                                                {estudianteData.estudianteNombre}
                                                            </h4>
                                                            <span className="shrink-0 px-2 py-0.5 bg-[var(--apple-bg)] border border-[var(--apple-border)] rounded-md text-[8px] font-black text-[var(--istpet-gold)] uppercase tracking-widest">
                                                                {estudianteData.periodo}
                                                            </span>
                                                        </div>
                                                        <p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-wide opacity-60">
                                                            {estudianteData.cursoDetalle}
                                                        </p>
                                                    </div>

                                                    {/* Stats Compactas */}
                                                    <div className="flex items-center justify-around lg:justify-end gap-5 lg:gap-7 py-2 lg:py-0 lg:pl-7">
                                                        <div className="text-center">
                                                            <span className="block text-[7px] font-black text-[var(--apple-text-sub)] uppercase mb-0.5 opacity-50">Licencia</span>
                                                            <span className="block text-sm font-black text-[var(--apple-primary)] leading-none">{estudianteData.tipoLicencia}</span>
                                                        </div>
                                                        <div className="h-5 w-px bg-[var(--apple-border)]/30"></div>
                                                        <div className="text-center">
                                                            <span className="block text-[7px] font-black text-[var(--apple-text-sub)] uppercase mb-0.5 opacity-50">Paralelo</span>
                                                            <span className="block text-sm font-black text-[var(--apple-text-main)] leading-none">{estudianteData.paralelo}</span>
                                                        </div>
                                                        <div className="h-5 w-px bg-[var(--apple-border)]/30"></div>
                                                        <div className="text-center">
                                                            <span className="block text-[7px] font-black text-[var(--apple-text-sub)] uppercase mb-0.5 opacity-50">Jornada</span>
                                                            <span className="block text-[9px] font-black text-[var(--apple-text-main)] uppercase leading-none">{estudianteData.jornada}</span>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div className="absolute top-0 right-0 h-full w-24 bg-gradient-to-l from-[var(--apple-primary)]/[0.03] to-transparent skew-x-12 translate-x-12 group-hover:translate-x-8 transition-transform duration-700"></div>

                                                {estudianteData.tienePracticaHoy && (
                                                    <div className="mt-4 bg-gradient-to-r from-[var(--istpet-navy)] to-[#2a3a6a] p-3 rounded-2xl text-white shadow-lg flex items-center justify-between gap-3 overflow-hidden relative">
                                                        <div className="flex items-center gap-3 relative z-10 text-left">
                                                            <div className="h-8 w-8 rounded-xl bg-white/10 flex items-center justify-center flex-shrink-0 backdrop-blur-sm border border-white/10">
                                                                <svg className="h-4 w-4 text-[var(--istpet-gold)]" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>
                                                            </div>
                                                            <div className="leading-tight">
                                                                <p className="text-[9px] font-black uppercase tracking-widest text-[var(--istpet-gold)]">Sugerencia de Agenda</p>
                                                                <p className="text-[10px] font-bold text-white/90">{estudianteData.practicaVehiculo} • {estudianteData.practicaHora}</p>
                                                            </div>
                                                        </div>
                                                        <button
                                                            onClick={(e) => {
                                                                e.stopPropagation();
                                                                const veh = vehiculos.find(v => v.idVehiculo === estudianteData.idPracticaCentral);
                                                                const inst = instructores.find(i => i.fullName === estudianteData.practicaInstructor);
                                                                if (veh) setVehiculoSeleccionado(veh);
                                                                if (inst) setInstructorSeleccionado(inst);
                                                                showNotification('Agenda cargada');
                                                            }}
                                                            className="relative z-10 px-3 py-1.5 bg-[var(--istpet-gold)] text-[var(--istpet-navy)] rounded-xl text-[9px] font-black uppercase hover:scale-105 active:scale-95 transition-all shadow-md shrink-0"
                                                        >
                                                            Cargar Sugerencia
                                                        </button>
                                                        <div className="absolute -right-2 top-0 h-full w-24 bg-white/5 skew-x-12"></div>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    ) : !salidaLoading && salidaCedula.length >= 1 && (
                                        <div className="p-8 border-2 border-dashed border-[var(--apple-border)] rounded-[2rem] text-center">
                                            <p className="text-[var(--apple-text-sub)] font-bold text-sm">Ingrese una cédula válida para cargar datos académicos</p>
                                        </div>
                                    )}

                                    {/* Selección Vehículo */}
                                    <div className="pt-6">
                                            <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between mb-4 lg:mb-8 gap-4 px-2">
                                                <div className="flex-1 text-left min-w-0 flex items-center gap-4">
                                                    <h3 className="text-lg lg:text-xl font-black text-[var(--apple-text-main)] truncate leading-none">Vehículo</h3>
                                                </div>

                                            <div className="flex items-center gap-2 flex-[2]">
                                                {/* Buscador Rápido (Compacto) */}
                                                <div className="relative flex-1 group">
                                                    <div className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)] group-focus-within:text-[var(--apple-primary)] transition-colors">
                                                        <svg className="h-3 w-3 lg:h-4 lg:w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                                                    </div>
                                                    <input
                                                        type="text"
                                                        placeholder="BUSCAR..."
                                                        value={filtroVehiculo}
                                                        onChange={(e) => setFiltroVehiculo(e.target.value.toUpperCase())}
                                                        className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-xl pl-9 pr-2 py-2 text-[10px] lg:text-[11px] font-black tracking-widest text-[var(--apple-text-main)] placeholder:text-[var(--apple-text-sub)]/30 focus:border-[var(--apple-primary)] shadow-inner transition-all outline-none"
                                                    />
                                                </div>

                                                {/* Indicadores de Licencia Compactos */}
                                                <div className="flex bg-[var(--apple-bg)] p-1 rounded-xl border border-[var(--apple-border)] shadow-sm shrink-0">
                                                    {['C', 'D', 'E'].map(lic => (
                                                        <div
                                                            key={lic}
                                                            className={`w-7 h-7 lg:w-9 lg:h-9 flex items-center justify-center rounded-lg text-[9px] lg:text-[10px] font-black transition-all ${estudianteData?.tipoLicencia === lic ? 'bg-white text-[var(--istpet-gold)] shadow-md border border-[var(--istpet-gold)]/20' : 'text-[var(--apple-text-sub)] opacity-20'}`}
                                                        >
                                                            {lic}
                                                        </div>
                                                    ))}
                                                </div>
                                            </div>
                                        </div>

                                        <div className="max-h-[380px] overflow-y-auto pr-2 custom-scrollbar -mr-2">
                                            <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-2">
                                                {vehiculos.length > 0 ? (
                                                    (() => {
                                                        const filtered = vehiculos.filter(v => {
                                                            const matchLicencia = !estudianteData || v.idTipoLicencia <= (estudianteData.idTipoLicencia || 3);
                                                            const matchFiltro = !filtroVehiculo ||
                                                                v.placa?.toLowerCase().includes(filtroVehiculo.toLowerCase()) ||
                                                                v.numero_vehiculo?.toString().includes(filtroVehiculo);
                                                            return matchLicencia && matchFiltro;
                                                        });

                                                        if (filtered.length === 0) {
                                                            return (
                                                                <div className="col-span-full py-12 text-center apple-glass rounded-[2rem] border border-dashed border-[var(--apple-border)] shadow-inner">
                                                                    <div className="text-[var(--apple-text-sub)] opacity-20 mb-2">
                                                                        <svg className="h-8 w-8 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="1.5"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                                                                    </div>
                                                                    <p className="text-[10px] font-black text-[var(--apple-text-sub)] uppercase tracking-[0.2em]">No se encontraron unidades</p>
                                                                </div>
                                                            );
                                                        }

                                                        return filtered.map(v => (
                                                            <VehicleCard
                                                                key={v.idVehiculo}
                                                                vehiculo={v}
                                                                isSelected={vehiculoSeleccionado?.idVehiculo === v.idVehiculo}
                                                                isSuggested={estudianteData?.idPracticaCentral === v.idVehiculo}
                                                                onSelect={handleSeleccionarVehiculo}
                                                            />
                                                        ));
                                                    })()
                                                ) : (
                                                    <div className="col-span-full py-16 text-center opacity-30">
                                                        <p className="text-[var(--apple-text-sub)] font-black uppercase text-xs tracking-widest animate-pulse">Cargando flota...</p>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    </div>

                                    {/* Selección Instructor (Compacto) */}
                                    <div className="pt-4 px-2">
                                        <div className="flex items-center gap-3 mb-3">
                                            <div className="w-1.5 h-1.5 rounded-full bg-emerald-500"></div>
                                            <p className="text-[10px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest">Instructor Responsable</p>
                                        </div>
                                        <div className="relative group">
                                            <select
                                                value={instructorSeleccionado?.id_Instructor || ''}
                                                onChange={(e) => setInstructorSeleccionado(instructores.find(i => i.id_Instructor.toString() === e.target.value))}
                                                className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-2xl px-6 py-3 text-xs lg:text-sm font-bold text-[var(--apple-text-main)] focus:border-emerald-500 focus:bg-[var(--apple-card)] outline-none transition-all shadow-inner appearance-none cursor-pointer"
                                            >
                                                <option value="" disabled>-- SELECCIONE DOCENTE --</option>
                                                {instructores.map(i => <option key={i.id_Instructor} value={i.id_Instructor}>{i.fullName}</option>)}
                                            </select>
                                            <div className="absolute right-4 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)] pointer-events-none">
                                                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" /></svg>
                                            </div>
                                        </div>
                                    </div>

                                    {/* Barra de Acción Unificada (Reloj + Botón) */}
                                    <div className="pt-6 border-t border-[var(--apple-border)]">
                                        <div className="flex items-stretch gap-3">
                                            {/* Reloj Compacto */}
                                            <div className="flex-1 bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] px-4 lg:px-6 py-3 lg:py-4 rounded-3xl flex items-center gap-3 lg:gap-4 shadow-inner">
                                                <div className="text-[var(--apple-primary)]">
                                                    <svg className="h-5 w-5 lg:h-6 lg:w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                </div>
                                                <div className="min-w-0">
                                                    <p className="text-[8px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest leading-none mb-0.5">Hora</p>
                                                    <p className="text-xl lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter">{horaRetorno || '--:--:--'}</p>
                                                </div>
                                            </div>

                                            {/* Botón Confirmar */}
                                            <button
                                                onClick={procesarSalida}
                                                disabled={!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado}
                                                className={`flex-[2] py-3 lg:py-5 rounded-3xl text-sm lg:text-base font-black transition-all ${(!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado) ? 'bg-[var(--apple-border)] text-[var(--apple-text-sub)] cursor-not-allowed opacity-30' : 'btn-apple-primary shadow-xl shadow-blue-500/20 active:scale-95 hover:scale-[1.02]'}`}
                                            >
                                                Confirmar Salida
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ) : (
                            <div className="apple-card">
                                <div className="flex items-center justify-between mb-10">
                                    <h3 className="text-xl lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tight">Registro de Llegada</h3>
                                    <div className="flex gap-2 items-center text-xs font-black text-emerald-500 uppercase tracking-widest">
                                        <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></div>
                                        UNIDADES EN PISTA
                                    </div>
                                </div>

                                <div className="space-y-8">
                                    <div className="max-h-[440px] overflow-y-auto pr-2 custom-scrollbar -mr-2">
                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                            {clasesActivas.length > 0 ? (
                                                clasesActivas.map(c => (
                                                    <div
                                                        key={c.id_Registro}
                                                        onClick={() => setClaseSeleccionada(c)}
                                                        className={`p-4 rounded-3xl border-2 transition-all cursor-pointer ${claseSeleccionada?.id_Registro === c.id_Registro ? 'bg-[var(--apple-primary)]/10 border-[var(--apple-primary)] shadow-md ring-4 ring-blue-500/10' : 'bg-[var(--apple-bg)] border-[var(--apple-border)] hover:border-[var(--apple-text-sub)]'}`}
                                                    >
                                                        <div className="flex justify-between items-start mb-3">
                                                            <div className="px-2 py-0.5 bg-[var(--apple-card)] border border-[var(--apple-border)] rounded-lg text-[10px] font-black text-[var(--apple-text-main)]">#{c.numeroVehiculo}</div>
                                                            <StatusBadge status="En Pista" />
                                                        </div>
                                                        <h5 className="font-black text-xs text-[var(--apple-text-main)] uppercase mb-1">{c.instructor}</h5>
                                                        <p className="text-[10px] text-[var(--apple-text-sub)] truncate mb-3">Estudiante: {c.estudiante}</p>
                                                        <div className="flex items-center gap-2 text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest">
                                                            <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                            Salió: {new Date(c.salida).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                                        </div>
                                                    </div>
                                                ))
                                            ) : (
                                                <div className="col-span-full py-12 text-center opacity-40 apple-glass rounded-3xl">
                                                    <p className="text-[var(--apple-text-sub)] font-bold tracking-widest text-xs uppercase">No hay vehículos en pista</p>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {claseSeleccionada && (
                                        <div className="mt-6 border-t border-[var(--apple-border)] pt-6 animate-apple-in">
                                            <div className="flex items-stretch gap-3">
                                                <div className="flex-1 bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] px-4 lg:px-6 py-3 lg:py-4 rounded-3xl flex items-center gap-3 lg:gap-4 shadow-inner">
                                                    <div className="text-emerald-500">
                                                        <svg className="h-5 w-5 lg:h-6 lg:w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                    </div>
                                                    <div className="min-w-0">
                                                        <p className="text-[8px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest leading-none mb-0.5">Retorno Proyectado</p>
                                                        <p className="text-xl lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase">{horaRetorno || 'LIVE'}</p>
                                                    </div>
                                                </div>
                                                <button
                                                    onClick={procesarLlegada}
                                                    className="flex-[2] py-3 lg:py-5 bg-[var(--istpet-gold)] text-white rounded-3xl text-sm lg:text-base font-black shadow-xl shadow-amber-500/20 hover:scale-[1.02] active:scale-95 transition-all"
                                                >
                                                    Confirmar Retorno
                                                </button>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>

                    {/* Sidebar Widgets */}
                    <div className="lg:col-span-5 xl:col-span-4 space-y-6 animate-apple-in" style={{ animationDelay: '0.3s' }}>

                        <div className="apple-glass rounded-[2.5rem] p-8 border border-[var(--apple-border)] relative overflow-hidden group">
                            <div className="absolute top-0 right-0 w-32 h-32 bg-[var(--apple-primary)]/5 rounded-full -translate-y-12 translate-x-12 blur-3xl group-hover:bg-[var(--apple-primary)]/10 transition-colors duration-700"></div>

                            <h4 className="text-xs font-black text-[var(--apple-text-sub)] tracking-[0.2em] uppercase mb-8">Agenda SIGAFI Hoy</h4>

                            <div className="space-y-4 max-h-[500px] overflow-y-auto pr-2 custom-scrollbar">
                                {agendadosHoy.length > 0 ? (
                                    agendadosHoy.map(ag => (
                                        <div key={ag.idPractica} className="bg-[var(--apple-card)] p-4 rounded-2xl border border-[var(--apple-border)] group/item hover:border-[var(--apple-primary)]/50 transition-all shadow-sm hover:shadow-md">
                                            <div className="flex justify-between items-start mb-2">
                                                <span className="text-xs font-black text-[var(--apple-primary)]">{ag.horaSalida?.substring(0, 5)}</span>
                                                <span className="text-[9px] font-black bg-[var(--apple-bg)] text-[var(--apple-text-sub)] px-1.5 py-0.5 rounded tracking-tighter uppercase font-mono">{ag.vehiculoDetalle}</span>
                                            </div>
                                            <p className="text-[11px] font-bold text-[var(--apple-text-main)] uppercase truncate mb-1">{ag.alumnoNombre || ag.cedulaAlumno}</p>
                                            <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-tighter truncate opacity-60 italic">{ag.profesorNombre}</p>
                                            <button
                                                onClick={() => {
                                                    setSalidaCedula(ag.cedulaAlumno);
                                                    showNotification('Cédula copiada al despacho');
                                                }}
                                                className="w-full mt-3 py-2 bg-[var(--apple-primary)]/10 text-[var(--apple-primary)] rounded-xl text-[9px] font-black uppercase tracking-widest opacity-0 group-hover/item:opacity-100 transition-all hover:bg-[var(--apple-primary)] hover:text-white shadow-sm"
                                            >
                                                CARGAR CÉDULA
                                            </button>
                                        </div>
                                    ))
                                ) : agendadosLoading ? (
                                    <div className="py-10 text-center">
                                        <div className="animate-spin h-5 w-5 border-2 border-[var(--apple-primary)] border-t-transparent rounded-full mx-auto"></div>
                                    </div>
                                ) : (
                                    <div className="py-10 text-center opacity-40">
                                        <p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-widest">Sin agendas registradas</p>
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
