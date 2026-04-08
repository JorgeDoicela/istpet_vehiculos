import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTheme } from '../components/common/ThemeContext';
import Layout from '../components/layout/Layout';
import logisticaService from '../services/logisticaService';
import dashboardService from '../services/dashboardService';
import StatusBadge from '../components/common/StatusBadge';
import VehicleCard from '../components/logistica/VehicleCard';

const ControlOperativo = () => {
    const { theme } = useTheme();
    const [searchParams] = useSearchParams();
    const [activeTab, setActiveTab] = useState(searchParams.get('tab') || 'salida');
    const [notification, setNotification] = useState(null);
    const instructorInputRef = useRef(null);

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
    const [mostrarBannerSugerencia, setMostrarBannerSugerencia] = useState(false);
    const [vehiculos, setVehiculos] = useState([]);
    const [filtroVehiculo, setFiltroVehiculo] = useState('');
    const [filtroLicencia, setFiltroLicencia] = useState(null);
    const [vehiculoSeleccionado, setVehiculoSeleccionado] = useState(null);
    const [instructores, setInstructores] = useState([]);
    const [instructorSeleccionado, setInstructorSeleccionado] = useState(null);

    // --- Estado Llegada ---
    const [clasesActivas, setClasesActivas] = useState([]);
    const [claseSeleccionada, setClaseSeleccionada] = useState(null);
    const [horaRetorno, setHoraRetorno] = useState('');
    const [fechaHoy, setFechaHoy] = useState('');
    const [agendadosHoy, setAgendadosHoy] = useState([]);
    const [agendadosLoading, setAgendadosLoading] = useState(false);
    const [showAgendaDrawer, setShowAgendaDrawer] = useState(false);
    const [showInstructorMenu, setShowInstructorMenu] = useState(false);
    const [filtroInstructor, setFiltroInstructor] = useState('');
    const [isSearchingInstructor, setIsSearchingInstructor] = useState(false);

    const showNotification = (message, type = 'success') => {
        setNotification({ message, type });
        setTimeout(() => setNotification(null), 4000);
    };

    // Reloj para Hora Retorno (Llegada)
    useEffect(() => {
        const clockInt = setInterval(() => {
            const now = new Date();
            const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
            const dateStr = now.toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long' }).toUpperCase();
            setHoraRetorno(timeStr);
            setFechaHoy(dateStr);
        }, 1000);
        return () => clearInterval(clockInt);
    }, []);

    // Carga de datos base
    useEffect(() => {
        cargarClasesActivas(); // Cargamos siempre para saber quién está en pista
        if (activeTab === 'salida') {
            cargarVehiculosDisponibles();
            cargarInstructores();
            cargarAgendadosHoy();
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
            // Evitar mostrar sugerencias si ya estamos en proceso de búsqueda (salidaLoading) o si ya completamos 10 dígitos (búsqueda definitiva)
            if (salidaCedula.length >= 3 && salidaCedula.length < 10 && activeTab === 'salida' && !salidaLoading && estudianteData?.cedula !== salidaCedula) {
                // 1. Filtrado local ultra-rápido de agendados hoy
                const localeMatch = agendadosHoy.filter(ag =>
                    ag.cedulaAlumno.startsWith(salidaCedula) ||
                    ag.alumnoNombre?.toLowerCase().includes(salidaCedula.toLowerCase())
                ).map(ag => ({
                    cedula: ag.cedulaAlumno,
                    nombreCompleto: ag.alumnoNombre,
                    esAgendado: true,
                    isBusy: clasesActivas.some(ca => ca.cedula === ag.cedulaAlumno),
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
                            combined.push({
                                ...sm,
                                isBusy: sm.isBusy || clasesActivas.some(ca => ca.cedula === sm.cedula)
                            });
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
                ejecutarBusquedaEstudiante(salidaCedula, 'manual');
            }
        }, 800);
        return () => clearTimeout(timer);
    }, [salidaCedula, activeTab, estudianteData]);

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

    const ejecutarBusquedaEstudiante = async (cedulaEnviada = null, source = 'manual') => {
        const cedulaABuscar = cedulaEnviada || salidaCedula;
        if (!cedulaABuscar || cedulaABuscar.length < 10) return;

        setSalidaLoading(true);
        setEstudianteData(null);
        setMostrarBannerSugerencia(false);

        try {
            const data = await logisticaService.buscarEstudiante(cedulaABuscar);
            setEstudianteData(data);
            if (data.tipoLicencia) setFiltroLicencia(data.tipoLicencia);
            
            // --- CEREBRO DE AUTOSUGERENCIA ---
            // Si el estudiante tiene una práctica agendada (Instructor o Vehículo), la aplicamos automáticamente
            if (data.idPracticaInstructor || data.idPracticaCentral) {
                await aplicarSugerenciaManual(data);
                
                if (source === 'agenda') {
                    showNotification('Sugerencia de agenda aplicada');
                } else {
                    setMostrarBannerSugerencia(true); // Mostramos el banner informativo en búsqueda manual
                    showNotification('¡Agenda detectada y aplicada automáticamente!');
                }
            } else {
                showNotification('Estudiante localizado');
            }

        } catch (err) {
            showNotification(err.message || 'No localizado', 'error');
        } finally {
            setSalidaLoading(false);
        }
    };

    const aplicarSugerenciaManual = async (dataOverride = null) => {
        const data = dataOverride || estudianteData;
        if (!data) return;
        
        try {
            // 1. Instructor
            if (data.idPracticaInstructor) {
                let sInstr = instructores.find(i => (i.id_Instructor || i.idInstructor) === data.idPracticaInstructor);
                if (!sInstr) {
                    const fresh = await logisticaService.getInstructores();
                    setInstructores(fresh);
                    sInstr = fresh.find(i => (i.id_Instructor || i.idInstructor) === data.idPracticaInstructor);
                }
                if (sInstr) setInstructorSeleccionado(sInstr);
            }

            // 2. Vehículo
            if (data.idPracticaCentral) {
                const sVeh = vehiculos.find(v => v.idVehiculo === data.idPracticaCentral);
                if (sVeh) setVehiculoSeleccionado(sVeh);
            }

            setMostrarBannerSugerencia(false);
        } catch (err) {
            console.error("Error aplicando sugerencia:", err);
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
            setFiltroLicencia(null);
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

            <div className="max-w-6xl mx-auto pt-2 lg:pt-6 pb-24 px-3 lg:px-6">
                <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 lg:gap-8 items-start">
                    {/* Panel Principal */}
                    <div className="lg:col-span-7 xl:col-span-8 space-y-6 lg:space-y-8 animate-apple-in" style={{ animationDelay: '0.2s' }}>
                        {/* Saludo Simplificado */}
                        <div className="px-8 lg:px-10 mb-2 lg:mb-4">
                            <p className="text-[10px] lg:text-xs font-black text-[var(--istpet-gold)] uppercase tracking-[0.2em] mb-0">
                                {new Date().getHours() < 12 ? 'Buenos días' : new Date().getHours() < 19 ? 'Buenas tardes' : 'Buenas noches'}
                            </p>
                            <h2 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase leading-tight">
                                Bienvenido
                            </h2>
                        </div>

                        {activeTab === 'salida' ? (
                            <div className="apple-card">
                                <div className="mb-10 px-2 flex items-center justify-between">
                                    <h3 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tight">Registro de Salida</h3>
                                    {/* Reloj compacto integrado en el header */}
                                    <div className="flex flex-col items-end leading-tight">
                                        <span className="text-[8px] lg:text-[9px] text-[var(--apple-text-sub)] opacity-60 uppercase font-black tracking-widest mb-0.5">{fechaHoy}</span>
                                        <span className="text-sm lg:text-lg font-black text-[var(--apple-text-main)] tracking-tighter tabular-nums">{horaRetorno || '--:--:--'}</span>
                                    </div>
                                </div>

                                <div className="space-y-8">
                                    {/* Campo Cédula - Más compacto */}
                                    <div className="relative group">
                                        <label className="absolute left-0 -top-4 px-2 bg-[var(--apple-card)] backdrop-blur-md text-[9px] font-black text-[var(--apple-text-main)] tracking-[0.2em] uppercase transition-all group-focus-within:text-[var(--istpet-gold)] z-10">
                                            Cédula del Estudiante
                                        </label>
                                        <div className="relative flex items-center gap-3">
                                            <input
                                                type="text"
                                                inputMode="numeric"
                                                pattern="[0-9]*"
                                                placeholder="Ej. 172..."
                                                maxLength={10}
                                                value={salidaCedula}
                                                onChange={(e) => setSalidaCedula(e.target.value.replace(/\D/g, ''))}
                                                className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[1.5rem] px-5 py-3 text-base lg:text-lg font-bold text-[var(--apple-text-main)] focus:border-[var(--istpet-gold)] focus:bg-[var(--apple-card)] outline-none transition-all shadow-inner tracking-widest"
                                            />

                                            {/* GESTIÓN CONDUCCIÓN AUTO-COMPLETE POPUP */}
                                            {mostrarSugerencias && (
                                                <div className="absolute left-0 right-0 top-full mt-2 bg-[var(--apple-card)] backdrop-blur-2xl border border-[var(--apple-border)] rounded-[2rem] shadow-2xl z-[100] overflow-hidden animate-apple-in p-2">
                                                    {sugerencias.map((s, idx) => (
                                                        <div
                                                            key={s.cedula}
                                                            onClick={() => {
                                                                setSalidaCedula(s.cedula);
                                                                setSugerencias([]); // Limpiar estado de sugerencias inmediatamente
                                                                setMostrarSugerencias(false);
                                                                ejecutarBusquedaEstudiante(s.cedula);
                                                            }}
                                                            className={`p-4 cursor-pointer hover:bg-[var(--apple-primary)]/10 transition-all flex items-center justify-between rounded-2xl group/item ${idx !== sugerencias.length - 1 ? 'mb-1' : ''}`}
                                                        >
                                                            <div className="flex-1 text-left py-2">
                                                                <p className="text-[15px] font-black text-[var(--apple-text-main)] uppercase tracking-tight leading-none mb-1.5">
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
                                                                            {s.isBusy ? (
                                                                                <span className="text-[9px] font-black bg-rose-500 text-white px-2 py-0.5 rounded-full shadow-sm animate-pulse uppercase">
                                                                                    EN PISTA
                                                                                </span>
                                                                            ) : (
                                                                                <span className="text-[9px] font-black bg-emerald-500 text-white px-2 py-0.5 rounded-full shadow-sm animate-pulse">
                                                                                    HOY
                                                                                </span>
                                                                            )}
                                                                        </div>
                                                                    ) : (
                                                                        <div className="flex items-center gap-2">
                                                                            <span className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase">
                                                                                {s.carrera || 'ESTUDIANTE REGULAR'}
                                                                            </span>
                                                                            {s.isBusy && (
                                                                                <span className="text-[9px] font-black bg-rose-500 text-white px-2 py-0.5 rounded-full shadow-sm animate-bounce-slow uppercase">
                                                                                    EN PISTA
                                                                                </span>
                                                                            )}
                                                                        </div>
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
                                            <div className="apple-glass rounded-[2.5rem] p-4 lg:p-5 shadow-sm hover:shadow-md transition-all group overflow-hidden relative">
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
                                                            {estudianteData.isBusy && (
                                                                <span className="shrink-0 px-2 py-0.5 bg-rose-500 text-white rounded-md text-[8px] font-black uppercase tracking-widest animate-pulse shadow-sm shadow-rose-500/20">
                                                                    Ya en pista
                                                                </span>
                                                            )}
                                                        </div>
                                                        <p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-wide opacity-60">
                                                            {estudianteData.cursoDetalle}
                                                        </p>
                                                    </div>

                                                    {/* Stats Compactas */}
                                                    <div className="flex items-center justify-around lg:justify-end gap-5 lg:gap-7 py-2 lg:py-0 lg:pl-7">
                                                        <div className="text-center">
                                                            <span className="block text-[7px] font-black text-[var(--apple-text-sub)] uppercase mb-0.5 opacity-50">Licencia</span>
                                                            <span className="block text-sm font-black text-[var(--apple-text-main)] leading-none">{estudianteData.tipoLicencia}</span>
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

                                                {/* Banner informativo ya no requiere botón de confirmación si ya se aplicó */}
                                                {mostrarBannerSugerencia && estudianteData.tienePracticaHoy && (
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
                                                            onClick={() => setMostrarBannerSugerencia(false)}
                                                            className="relative z-10 px-4 py-2 bg-white/10 hover:bg-white/20 text-white rounded-full text-[9px] font-black uppercase transition-all shrink-0"
                                                        >
                                                            Ocultar
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
                                        <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between mb-4 gap-3 px-1 group">
                                            <div className="flex-1 text-left min-w-0">
                                                <h3 className="text-[9px] font-black uppercase tracking-[0.2em] mb-2 px-2 transition-colors duration-300 group-focus-within:text-[var(--istpet-gold)] text-[var(--apple-text-main)]">Selecciona Vehículo</h3>
                                            </div>

                                            <div className="flex items-center gap-2 flex-[2]">
                                                {/* Buscador Rápido (Compacto) */}
                                                <div className="relative flex-1 group/search">
                                                    <div className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)] group-focus-within/search:text-[var(--istpet-gold)] transition-colors">
                                                        <svg className="h-3 w-3 lg:h-4 lg:w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                                                    </div>
                                                    <input
                                                        type="text"
                                                        placeholder="BUSCAR UNIDAD..."
                                                        value={filtroVehiculo}
                                                        onChange={(e) => setFiltroVehiculo(e.target.value.toUpperCase())}
                                                        className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[1.5rem] pl-10 pr-10 py-2.5 text-[10px] lg:text-[11px] font-black tracking-widest text-[var(--apple-text-main)] placeholder:text-[var(--apple-text-sub)]/30 focus:border-[var(--istpet-gold)] shadow-inner transition-all outline-none"
                                                    />
                                                    {filtroVehiculo && (
                                                        <button
                                                            onClick={() => setFiltroVehiculo('')}
                                                            className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)] hover:text-red-500 transition-colors"
                                                        >
                                                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                                        </button>
                                                    )}
                                                </div>

                                                {/* Indicadores de Licencia Interactivos y Flexibles */}
                                                <div className="flex bg-[var(--apple-bg)] p-1 rounded-[1.5rem] border border-[var(--apple-border)] shadow-sm shrink-0">
                                                    {['C', 'D', 'E'].map(lic => (
                                                        <button
                                                            key={lic}
                                                            onClick={() => setFiltroLicencia(filtroLicencia === lic ? null : lic)}
                                                            title={`Filtrar por tipo ${lic}`}
                                                            className={`w-8 lg:w-10 h-7 lg:h-9 flex items-center justify-center rounded-[0.8rem] text-[9px] lg:text-[10px] font-black transition-all duration-300 select-none cursor-pointer
                                                                ${filtroLicencia === lic 
                                                                    ? 'bg-[var(--istpet-gold)] text-white shadow-sm shadow-[var(--istpet-gold)]/30' 
                                                                    : 'text-[var(--apple-text-sub)] opacity-50 hover:opacity-100 hover:bg-[var(--apple-border)]/50'}
                                                            `}
                                                        >
                                                            {lic}
                                                        </button>
                                                    ))}
                                                </div>
                                            </div>
                                        </div>

                                        <div className="max-h-[380px] overflow-y-auto pr-2 custom-scrollbar -mr-2 mt-4">
                                            <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-3">
                                                {vehiculos.length > 0 ? (
                                                    (() => {
                                                        const licIdMap = { 'C': 1, 'D': 2, 'E': 3 };
                                                        const filtered = vehiculos.filter(v => {
                                                            const term = filtroVehiculo.toLowerCase().trim();
                                                            const matchLicencia = filtroLicencia 
                                                                ? v.idTipoLicencia === licIdMap[filtroLicencia] 
                                                                : (!estudianteData || v.idTipoLicencia <= (estudianteData.idTipoLicencia || 3));
                                                            const matchFiltro = !term ||
                                                                v.placa?.toLowerCase().includes(term) ||
                                                                v.numero_vehiculo?.toString().includes(term) ||
                                                                v.numeroVehiculo?.toString().includes(term);
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
                                                                isSuggested={mostrarBannerSugerencia && estudianteData?.idPracticaCentral === v.idVehiculo}
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

                                    {/* Selección Instructor - Integrated Combobox Search */}
                                    <div className="pt-6 px-1 relative">
                                        <div className="mb-2 transition-all">
                                            <p className={`text-[9px] font-black uppercase tracking-[0.2em] px-2 transition-colors duration-300 ${showInstructorMenu ? 'text-[var(--istpet-gold)]' : 'text-[var(--apple-text-main)]'}`}>Instructor Responsable</p>
                                        </div>

                                        <div className="relative group">
                                            <div className="relative flex items-center">
                                                <input
                                                    ref={instructorInputRef}
                                                    type="text"
                                                    readOnly={!isSearchingInstructor}
                                                    value={isSearchingInstructor && showInstructorMenu ? filtroInstructor : (instructorSeleccionado?.fullName || '')}
                                                    onChange={(e) => {
                                                        if (!isSearchingInstructor) return;
                                                        const val = e.target.value.toUpperCase();
                                                        setFiltroInstructor(val);
                                                        if (!showInstructorMenu) setShowInstructorMenu(true);
                                                    }}
                                                    onClick={() => {
                                                        if (!showInstructorMenu) {
                                                            setShowInstructorMenu(true);
                                                            if (isSearchingInstructor) setFiltroInstructor('');
                                                        }
                                                    }}
                                                    placeholder={isSearchingInstructor ? "ESCRIBA PARA BUSCAR..." : "-- SELECCIONE DOCENTE --"}
                                                    className={`w-full bg-[var(--apple-bg)] border-2 rounded-[1.5rem] pl-6 pr-24 py-3 text-xs font-bold text-[var(--apple-text-main)] transition-all shadow-inner outline-none uppercase placeholder:text-[var(--apple-text-sub)]/30 ${showInstructorMenu ? 'border-[var(--istpet-gold)] shadow-[0_0_20px_rgba(181,148,74,0.15)]' : 'border-[var(--apple-border)]'} ${!isSearchingInstructor ? 'cursor-pointer hover:bg-[var(--apple-card)]' : 'cursor-text'}`}
                                                />

                                                {/* Botón Seleccionar (Flecha) - Activa modo selector */}
                                                <button
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        setIsSearchingInstructor(false);
                                                        setFiltroInstructor('');
                                                        setShowInstructorMenu(true);
                                                    }}
                                                    className={`absolute right-16 top-1/2 -translate-y-1/2 z-20 p-1 rounded-lg transition-all ${!isSearchingInstructor ? 'text-[var(--istpet-gold)] scale-110' : 'text-[var(--apple-text-sub)] hover:bg-[var(--apple-border)]/50'}`}
                                                    title="Modo Selector"
                                                >
                                                    <svg className={`h-4 w-4 transition-transform duration-500 ease-in-out ${showInstructorMenu ? 'rotate-180' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" /></svg>
                                                </button>

                                                {/* Botón Buscador (Lupa) - Activa modo escritura */}
                                                <button
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        setIsSearchingInstructor(true);
                                                        setShowInstructorMenu(true);
                                                        setFiltroInstructor('');
                                                        setTimeout(() => instructorInputRef.current?.focus(), 50);
                                                    }}
                                                    className={`absolute right-4 top-1/2 -translate-y-1/2 z-20 p-2 rounded-lg transition-all ${isSearchingInstructor ? 'text-[var(--istpet-gold)] scale-125' : 'text-[var(--apple-text-sub)] hover:bg-[var(--apple-border)]/50'}`}
                                                    title="Modo Buscador"
                                                >
                                                    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="4">
                                                        <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                                                    </svg>
                                                </button>
                                            </div>

                                            {showInstructorMenu && (
                                                <>
                                                    {/* Backdrop invisible para cerrar al hacer clic fuera */}
                                                    <div className="fixed inset-0 z-[140]" onClick={() => {
                                                        setShowInstructorMenu(false);
                                                        setFiltroInstructor('');
                                                    }} />

                                                    {/* Menú Flotante Estilo Apple (Compatible con Temas) */}
                                                    <div className="absolute left-0 right-0 top-full mt-3 bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[2.2rem] shadow-[0_25px_60px_-15px_rgba(0,0,0,0.3)] z-[150] overflow-hidden animate-apple-in flex flex-col max-h-[300px] ring-1 ring-black/5">
                                                        {/* Lista de Instructores con Scroll Optimizado */}
                                                        <div className="flex-1 overflow-y-auto p-3 custom-scrollbar space-y-1">
                                                            {instructores.filter(i =>
                                                                !filtroInstructor || i.fullName.toUpperCase().includes(filtroInstructor)
                                                            ).map((i) => (
                                                                <div
                                                                    key={i.id_Instructor}
                                                                    onClick={() => {
                                                                        setInstructorSeleccionado(i);
                                                                        setShowInstructorMenu(false);
                                                                        setFiltroInstructor('');
                                                                    }}
                                                                    className={`p-4 cursor-pointer hover:bg-[var(--apple-primary)]/10 transition-all flex items-center justify-between rounded-2xl group/item ${instructorSeleccionado?.id_Instructor === i.id_Instructor ? 'bg-[var(--apple-primary)]/10' : ''}`}
                                                                >
                                                                    <p className={`text-[11px] font-black uppercase tracking-tight transition-all group-hover/item:translate-x-1 ${instructorSeleccionado?.id_Instructor === i.id_Instructor ? 'text-[var(--istpet-gold)]' : 'text-[var(--apple-text-main)]'}`}>
                                                                        {i.fullName}
                                                                    </p>
                                                                    {instructorSeleccionado?.id_Instructor === i.id_Instructor && (
                                                                        <div className="h-1.5 w-1.5 rounded-full bg-[var(--istpet-gold)] shadow-[0_0_8px_var(--istpet-gold)]" />
                                                                    )}
                                                                </div>
                                                            ))}
                                                            {instructores.filter(i => !filtroInstructor || i.fullName.toUpperCase().includes(filtroInstructor)).length === 0 && (
                                                                <div className="py-12 text-center opacity-30">
                                                                    <p className="text-[10px] font-black uppercase tracking-widest">Sin resultados</p>
                                                                </div>
                                                            )}
                                                        </div>
                                                        <div className="h-4 bg-gradient-to-t from-[var(--apple-bg)] to-transparent pointer-events-none absolute bottom-0 left-0 right-0"></div>
                                                    </div>
                                                </>
                                            )}
                                        </div>
                                    </div>

                                    {/* Barra de Acción Unificada */}
                                    <div className="pt-6 border-t border-[var(--apple-border)] mt-4">
                                        <button
                                            onClick={procesarSalida}
                                            disabled={!estudianteData || estudianteData.isBusy || !vehiculoSeleccionado || !instructorSeleccionado}
                                            className={`w-full py-4 rounded-full text-sm font-black transition-all ${(!estudianteData || estudianteData.isBusy || !vehiculoSeleccionado || !instructorSeleccionado) ? 'bg-[var(--apple-border)] text-[var(--apple-text-sub)] cursor-not-allowed opacity-30' : 'btn-apple-primary shadow-xl shadow-[var(--istpet-gold)]/20 active:scale-95 hover:scale-[1.01]'}`}
                                        >
                                            {!estudianteData ? 'Ingrese cédula del estudiante' :
                                                estudianteData.isBusy ? 'Estudiante ya en pista' :
                                                    !vehiculoSeleccionado ? 'Seleccione un vehículo' :
                                                        !instructorSeleccionado ? 'Seleccione un instructor' :
                                                            '✓ Confirmar Salida'}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ) : (
                            <div className="apple-card">
                                <div className="flex items-center justify-between mb-5">
                                    <h3 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tight">Registro de Llegada</h3>
                                    <div className="flex items-center gap-2">
                                        <div className="flex flex-col items-end leading-tight">
                                            <span className="text-[8px] lg:text-[9px] text-[var(--apple-text-sub)] opacity-60 uppercase font-black tracking-widest mb-0.5">{fechaHoy}</span>
                                            <span className="text-sm lg:text-lg font-black text-[var(--apple-text-main)] tracking-tighter tabular-nums">{horaRetorno || '--:--:--'}</span>
                                        </div>
                                        <div className="flex gap-1.5 items-center text-[9px] font-black text-emerald-500 uppercase tracking-widest">
                                            <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></div>
                                            EN PISTA
                                        </div>
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
                                                        className={`p-4 rounded-[2.5rem] border-2 transition-all cursor-pointer ${claseSeleccionada?.id_Registro === c.id_Registro ? 'bg-[var(--istpet-gold)]/10 border-[var(--istpet-gold)] shadow-lg shadow-[var(--istpet-gold)]/20' : 'bg-[var(--apple-bg)] border-[var(--apple-border)] hover:border-[var(--apple-text-sub)]'}`}
                                                    >
                                                        <div className="flex justify-between items-start mb-3 px-2 pt-1">
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
                                                        <p className="text-[9px] font-black text-[var(--apple-text-main)] uppercase tracking-[0.2em] leading-none mb-1.5">Retorno Proyectado</p>
                                                        <p className="text-xl lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase">{horaRetorno || 'LIVE'}</p>
                                                    </div>
                                                </div>
                                                <button
                                                    onClick={procesarLlegada}
                                                    className="flex-[2] py-3 lg:py-5 bg-[var(--istpet-gold)] text-white rounded-full text-sm lg:text-base font-black shadow-xl shadow-amber-500/20 hover:scale-[1.02] active:scale-95 transition-all"
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

                    {/* Sidebar Widgets - Oculto en móvil, visible en desktop */}
                    <div className="hidden lg:block lg:col-span-5 xl:col-span-4 space-y-6 animate-apple-in" style={{ animationDelay: '0.3s' }}>

                        <div className="apple-glass rounded-[2rem] p-5 border border-[var(--apple-border)] relative overflow-hidden group">
                            <div className="absolute top-0 right-0 w-24 h-24 bg-[var(--apple-primary)]/5 rounded-full -translate-y-10 translate-x-10 blur-3xl group-hover:bg-[var(--apple-primary)]/10 transition-colors duration-700"></div>

                            <div className="flex items-center justify-between mb-4">
                                <h4 className="text-[9px] font-black text-[var(--apple-text-main)] uppercase tracking-[0.2em]">Agenda SIGAFI Hoy</h4>
                                <span className="text-[9px] font-black text-[var(--apple-primary)] bg-[var(--apple-primary)]/10 px-2 py-0.5 rounded-full">{agendadosHoy.length} pendientes</span>
                            </div>

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
                                                    ejecutarBusquedaEstudiante(ag.cedulaAlumno, 'agenda');
                                                    showNotification('Cédula cargada de la agenda');
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

            {/* === MOBILE AGENDA DRAWER === */}
            {/* Botón flotante para abrir en móvil - Minimalista / Adaptable al Tema */}
            {activeTab === 'salida' && (
                <button
                    onClick={() => setShowAgendaDrawer(true)}
                    className={`lg:hidden fixed bottom-20 right-4 z-50 h-14 w-14 flex items-center justify-center rounded-[1.4rem] transition-all duration-500 active:scale-90 animate-apple-in shadow-2xl
                        ${theme === 'light' 
                            ? 'bg-white/80 backdrop-blur-xl border border-slate-200/60 text-[var(--istpet-navy)] shadow-slate-200/50' 
                            : 'bg-[var(--istpet-navy)] text-white border border-white/10 shadow-black/40'}
                    `}
                >
                    <div className="relative">
                        <svg className={`h-6 w-6 transition-colors duration-500 ${theme === 'light' ? 'text-[var(--istpet-gold)]' : 'text-[var(--istpet-gold)]'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                        
                        {agendadosHoy.length > 0 && (
                            <span className="absolute -top-2.5 -right-2.5 flex items-center justify-center text-[13px] font-black text-[var(--istpet-gold)] drop-shadow-sm">
                                {agendadosHoy.length}
                            </span>
                        )}
                    </div>
                </button>
            )}

            {/* Drawer de Agenda - Móvil */}
            {showAgendaDrawer && (
                <div className="lg:hidden fixed inset-0 z-[200] flex flex-col justify-end animate-apple-in">
                    {/* Overlay */}
                    <div className="absolute inset-0 bg-black/30 backdrop-blur-md" onClick={() => setShowAgendaDrawer(false)} />

                    {/* Panel */}
                    <div className="relative bg-[var(--apple-bg)] rounded-t-[2.5rem] max-h-[80vh] flex flex-col shadow-[0_-20px_60px_rgba(0,0,0,0.15)] overflow-hidden">
                        
                        {/* Handle */}
                        <div className="flex justify-center pt-3 pb-1">
                            <div className="w-10 h-1 rounded-full bg-[var(--apple-border)]" />
                        </div>

                        {/* Header Premium */}
                        <div className="flex items-center justify-between px-6 py-5">
                            <div>
                                <h3 className="text-sm font-black text-[var(--apple-text-main)] uppercase tracking-[0.15em]">Agenda Hoy</h3>
                                <p className="text-[10px] font-bold text-[var(--apple-text-sub)] mt-0.5">
                                    <span className="text-[var(--istpet-gold)] font-black">{agendadosHoy.length}</span> estudiantes programados
                                </p>
                            </div>
                            <button
                                onClick={() => setShowAgendaDrawer(false)}
                                className="h-8 w-8 flex items-center justify-center rounded-full bg-[var(--apple-border)]/40 text-[var(--apple-text-sub)] hover:bg-[var(--apple-border)] transition-all"
                            >
                                <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                            </button>
                        </div>

                        {/* Lista con Estética iOS (Separadores Parciales) */}
                        <div className="overflow-y-auto flex-1 custom-scrollbar pb-10">
                            {agendadosHoy.length > 0 ? agendadosHoy.map((ag, idx) => (
                                <div
                                    key={ag.idPractica}
                                    className="px-6 group active:bg-[var(--apple-border)]/20 transition-colors"
                                    style={{ animationDelay: `${idx * 0.05}s` }}
                                >
                                    <div className={`flex items-start gap-3 py-5 ${idx !== 0 ? 'border-t border-[var(--apple-border)]/40' : ''}`}>
                                        {/* Número de Orden */}
                                        <div className="shrink-0 w-8 flex items-center justify-center -mt-[2px]">
                                            <span className="text-[11px] font-black text-[var(--istpet-gold)]">{String(idx + 1).padStart(2,'0')}</span>
                                        </div>

                                        {/* Info */}
                                        <div className="flex-1 min-w-0">
                                            <p className="text-[12px] font-black text-[var(--apple-text-main)] uppercase truncate leading-tight">
                                                {ag.alumnoNombre || ag.cedulaAlumno}
                                            </p>
                                            <div className="flex items-center gap-2 mt-2">
                                                <span className="flex items-center gap-1 text-[10px] font-black text-[var(--istpet-gold)] bg-[var(--istpet-gold)]/10 px-2 py-0.5 rounded-md">
                                                    <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                    {ag.horaSalida?.substring(0, 5)}
                                                </span>
                                                <span className="px-2 py-0.5 bg-[var(--apple-border)]/40 rounded-md text-[9px] font-bold text-[var(--apple-text-main)] uppercase tracking-tight">
                                                    #{ag.vehiculoDetalle}
                                                </span>
                                            </div>
                                        </div>

                                        {/* Botón Cargar - Optimizado */}
                                        <button
                                            onClick={() => {
                                                setSalidaCedula(ag.cedulaAlumno);
                                                setShowAgendaDrawer(false);
                                                ejecutarBusquedaEstudiante(ag.cedulaAlumno, 'agenda');
                                                showNotification('Cédula cargada de la agenda');
                                            }}
                                            className="shrink-0 px-5 py-3 bg-[var(--istpet-navy)] text-white rounded-2xl text-[9px] font-black uppercase tracking-widest active:scale-95 transition-all shadow-md active:shadow-none"
                                        >
                                            Cargar
                                        </button>
                                    </div>
                                </div>
                            )) : (
                                <div className="py-20 text-center opacity-40">
                                    <div className="mb-3">
                                        <svg className="h-10 w-10 mx-auto text-[var(--apple-text-sub)]" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="1.5">
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                                        </svg>
                                    </div>
                                    <p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-[0.2em]">Sin agendas para hoy</p>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            )}

        </Layout>
    );
};

export default ControlOperativo;
