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

            <div className="max-w-6xl mx-auto pt-10 pb-20 px-6">
                {/* Header Seccion */}
                <div className="flex items-center justify-center gap-3 lg:gap-6 mb-6 lg:mb-10 text-center animate-apple-in">
                    <div className="hidden sm:block h-[1px] w-12 lg:w-24 bg-gradient-to-r from-transparent to-[var(--apple-text-main)] opacity-10"></div>
                    <h1 className="text-[10px] lg:text-xs font-black tracking-[0.4em] text-[var(--apple-text-main)] opacity-80 uppercase whitespace-nowrap">
                        Gestión y Control de Unidades
                    </h1>
                    <div className="hidden sm:block h-[1px] w-12 lg:w-24 bg-gradient-to-l from-transparent to-[var(--apple-text-main)] opacity-10"></div>
                </div>

                {/* El selector de pestañas interno ha sido eliminado por redundancia con el Sidebar/Tab-Bar global */}
                <div className="mt-8 lg:mt-12"></div>

                <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-8 items-start">
                    {/* Panel Principal */}
                    <div className="lg:col-span-7 xl:col-span-8 space-y-6 lg:space-y-8 animate-apple-in" style={{ animationDelay: '0.2s' }}>

                        {activeTab === 'salida' ? (
                            <div className="apple-card overflow-hidden">
                                <div className="flex items-center justify-between mb-10">
                                    <h3 className="text-2xl font-black text-[var(--apple-text-main)] tracking-tight">Registro de Salida</h3>
                                </div>

                                <div className="space-y-8">
                                    {/* Campo Cédula */}
                                    <div className="relative group">
                                        <label className="absolute left-6 -top-3 px-2 bg-[var(--apple-card)] backdrop-blur-md text-[10px] font-black text-[var(--istpet-gold)] tracking-[0.2em] uppercase transition-all">
                                            Cédula del Estudiante
                                        </label>
                                        <div className="flex items-center gap-4">
                                            <input
                                                type="text"
                                                placeholder="Ej. 1725555377"
                                                maxLength={10}
                                                value={salidaCedula}
                                                onChange={(e) => setSalidaCedula(e.target.value)}
                                                className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-3xl px-8 py-5 text-xl font-bold text-[var(--apple-text-main)] focus:border-[var(--istpet-gold)] focus:bg-[var(--apple-card)] outline-none transition-all shadow-inner"
                                            />
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
                                            <div className="bg-[var(--apple-primary)]/5 border border-[var(--apple-primary)]/10 rounded-[2rem] p-8">
                                                <div className="flex flex-col lg:flex-row items-center lg:items-start gap-4 lg:gap-6 text-center lg:text-left mb-6">
                                                    {estudianteData.fotoBase64 ? (
                                                        <div className="h-24 w-24 rounded-2xl overflow-hidden shadow-lg border-2 border-white/20 flex-shrink-0">
                                                            <img src={`data:image/jpeg;base64,${estudianteData.fotoBase64}`} className="w-full h-full object-cover" alt="Alumno" />
                                                        </div>
                                                    ) : (
                                                        <div className="h-20 w-20 rounded-2xl bg-[var(--apple-primary)] flex items-center justify-center text-white text-3xl font-black shadow-lg">
                                                            {estudianteData.estudianteNombre?.[0]}
                                                        </div>
                                                    )}
                                                    <div className="flex-1">
                                                        <h4 className="text-xl font-black text-[var(--apple-text-main)] mb-1">{estudianteData.estudianteNombre}</h4>
                                                        <div className="flex items-center gap-2 flex-wrap justify-center lg:justify-start">
                                                            <span className="text-[10px] font-black bg-[var(--istpet-gold)]/20 text-[var(--istpet-gold)] px-2 py-0.5 rounded-md uppercase tracking-widest">{estudianteData.cursoDetalle}</span>
                                                            <span className="text-[10px] font-black bg-[var(--apple-text-sub)]/10 text-[var(--apple-text-sub)] px-2 py-0.5 rounded-md uppercase tracking-widest">PERIODO: {estudianteData.periodo}</span>
                                                        </div>
                                                    </div>
                                                </div>

                                                <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                                                    <div className="bg-[var(--apple-card)] p-4 rounded-2xl border border-[var(--apple-border)]">
                                                        <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase mb-1 tracking-widest">Licencia</p>
                                                        <p className="text-sm font-black text-[var(--apple-primary)]">TIPO {estudianteData.tipoLicencia}</p>
                                                    </div>
                                                    <div className="bg-[var(--apple-card)] p-4 rounded-2xl border border-[var(--apple-border)]">
                                                        <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase mb-1 tracking-widest">Paralelo</p>
                                                        <p className="text-sm font-black text-[var(--apple-text-main)]">"{estudianteData.paralelo}"</p>
                                                    </div>
                                                    <div className="bg-[var(--apple-card)] p-4 rounded-2xl border border-[var(--apple-border)] col-span-2 md:col-span-1">
                                                        <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase mb-1 tracking-widest">Jornada</p>
                                                        <p className="text-sm font-black text-[var(--apple-text-main)]">{estudianteData.jornada}</p>
                                                    </div>
                                                </div>

                                                {estudianteData.tienePracticaHoy && (
                                                    <div className="mt-6 bg-gradient-to-r from-[var(--apple-primary)] to-indigo-600 p-6 rounded-2xl text-white shadow-xl shadow-blue-600/10 flex flex-col md:flex-row justify-between items-center gap-4">
                                                        <div className="flex items-center gap-4">
                                                            <div className="h-10 w-10 rounded-xl bg-white/20 flex items-center justify-center">
                                                                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                            </div>
                                                            <div>
                                                                <p className="text-[10px] font-black uppercase text-white/70 tracking-widest">Agenda SIGAFI</p>
                                                                <p className="text-sm font-bold">{estudianteData.practicaVehiculo} • {estudianteData.practicaHora}</p>
                                                            </div>
                                                        </div>
                                                        <button
                                                            onClick={() => {
                                                                const veh = vehiculos.find(v => v.idVehiculo === estudianteData.idPracticaCentral);
                                                                const inst = instructores.find(i => i.fullName === estudianteData.practicaInstructor);
                                                                if (veh) setVehiculoSeleccionado(veh);
                                                                if (inst) setInstructorSeleccionado(inst);
                                                                showNotification('Agenda cargada');
                                                            }}
                                                            className="px-6 py-2 bg-white text-[var(--apple-primary)] rounded-xl text-xs font-black uppercase hover:scale-105 transition-all shadow-lg shadow-black/10"
                                                        >
                                                            CARGAR DATOS
                                                        </button>
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
                                        <div className="flex flex-col md:flex-row items-center justify-between mb-8 gap-4 px-2">
                                            <div>
                                                <h3 className="text-xl font-black text-[var(--apple-text-main)] mb-1">Selección de Vehículo</h3>
                                                <p className="text-[10px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest">Unidades operativas disponibles</p>
                                            </div>
                                            <div className="flex bg-[var(--apple-bg)] p-1 rounded-2xl border border-[var(--apple-border)]">
                                                {['C', 'D', 'E'].map(lic => (
                                                    <button key={lic} disabled className={`px-4 py-1.5 rounded-xl text-[10px] font-black transition-all ${estudianteData?.tipoLicencia === lic ? 'bg-[var(--apple-card)] text-[var(--istpet-gold)] shadow-sm' : 'text-[var(--apple-text-sub)] opacity-20'}`}>
                                                        TIPO {lic}
                                                    </button>
                                                ))}
                                            </div>
                                        </div>

                                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
                                            {vehiculos.length > 0 ? (
                                                vehiculos.filter(v => !estudianteData || v.idTipoLicencia <= (estudianteData.idTipoLicencia || 3)).map(v => (
                                                    <VehicleCard
                                                        key={v.idVehiculo}
                                                        vehiculo={v}
                                                        isSelected={vehiculoSeleccionado?.idVehiculo === v.idVehiculo}
                                                        onSelect={handleSeleccionarVehiculo}
                                                    />
                                                ))
                                            ) : (
                                                <div className="col-span-full py-20 text-center opacity-30">
                                                    <p className="text-[var(--apple-text-sub)] font-black uppercase text-xs tracking-widest">Cargando flota...</p>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {/* Selección Instructor */}
                                    <div className="pt-6">
                                        <h3 className="text-xl font-black text-[var(--apple-text-main)] mb-6 px-2">Asignación de Instructor</h3>
                                        <div className="relative group">
                                            <label className="absolute left-6 -top-3 px-2 bg-[var(--apple-card)] backdrop-blur-md text-[9px] font-black text-emerald-600 tracking-[0.2em] uppercase z-10">
                                                Docente Responsable
                                            </label>
                                            <select
                                                value={instructorSeleccionado?.id_Instructor || ''}
                                                onChange={(e) => setInstructorSeleccionado(instructores.find(i => i.id_Instructor.toString() === e.target.value))}
                                                className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-2xl px-8 py-5 text-sm font-bold text-[var(--apple-text-main)] focus:border-emerald-500 focus:bg-[var(--apple-card)] outline-none transition-all shadow-inner appearance-none cursor-pointer"
                                            >
                                                <option value="" disabled>-- SELECCIONE DOCENTE --</option>
                                                {instructores.map(i => <option key={i.id_Instructor} value={i.id_Instructor}>{i.fullName}</option>)}
                                            </select>
                                        </div>
                                    </div>

                                    {/* Botón Final Salida */}
                                    <div className="pt-10 flex flex-col md:flex-row items-stretch justify-between gap-4 border-t border-[var(--apple-border)]">
                                        <div className="flex-1 bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] px-8 py-4 rounded-[2.5rem] flex items-center gap-6 shadow-inner">
                                            <div className="h-12 w-12 rounded-2xl bg-[var(--apple-primary)] flex items-center justify-center text-white shadow-lg shadow-blue-500/20">
                                                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                            </div>
                                            <div>
                                                <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest">Hora Actual</p>
                                                <p className="text-3xl font-black text-[var(--apple-text-main)] tracking-tighter">{horaRetorno || '--:--'}</p>
                                            </div>
                                        </div>
                                        <button
                                            onClick={procesarSalida}
                                            disabled={!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado}
                                            className={`px-12 py-6 rounded-[2.5rem] text-xl font-black transition-all ${(!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado) ? 'bg-[var(--apple-border)] text-[var(--apple-text-sub)] cursor-not-allowed opacity-30' : 'btn-apple-primary shadow-xl shadow-blue-500/20 active:scale-95 hover:scale-[1.02]'}`}
                                        >
                                            Confirmar Salida
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ) : (
                            <div className="apple-card">
                                <div className="flex items-center justify-between mb-10">
                                    <h3 className="text-2xl font-black text-[var(--apple-text-main)] tracking-tight">Registro de Llegada</h3>
                                    <div className="flex gap-2 items-center text-xs font-black text-emerald-500 uppercase tracking-widest">
                                        <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></div>
                                        UNIDADES EN PISTA
                                    </div>
                                </div>

                                <div className="space-y-8">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                        {clasesActivas.length > 0 ? (
                                            clasesActivas.map(c => (
                                                <div
                                                    key={c.id_Registro}
                                                    onClick={() => setClaseSeleccionada(c)}
                                                    className={`p-6 rounded-3xl border-2 transition-all cursor-pointer ${claseSeleccionada?.id_Registro === c.id_Registro ? 'bg-[var(--apple-primary)]/10 border-[var(--apple-primary)] shadow-md ring-4 ring-blue-500/10' : 'bg-[var(--apple-bg)] border-[var(--apple-border)] hover:border-[var(--apple-text-sub)]'}`}
                                                >
                                                    <div className="flex justify-between items-start mb-4">
                                                        <div className="px-2 py-1 bg-[var(--apple-card)] border border-[var(--apple-border)] rounded-lg text-xs font-black text-[var(--apple-text-main)]">#{c.numeroVehiculo}</div>
                                                        <StatusBadge status="En Pista" />
                                                    </div>
                                                    <h5 className="font-bold text-[var(--apple-text-main)] uppercase mb-1">{c.instructor}</h5>
                                                    <p className="text-xs text-[var(--apple-text-sub)] truncate mb-4">Estudiante: {c.estudiante}</p>
                                                    <div className="flex items-center gap-3 text-[10px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest">
                                                        <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                        Salió: {new Date(c.salida).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                                    </div>
                                                </div>
                                            ))
                                        ) : (
                                            <div className="col-span-full py-16 text-center opacity-40 apple-glass rounded-3xl">
                                                <p className="text-[var(--apple-text-sub)] font-bold tracking-widest text-xs uppercase">No hay vehículos en pista actualmente</p>
                                            </div>
                                        )}
                                    </div>

                                    {claseSeleccionada && (
                                        <div className="mt-8 p-8 bg-[var(--apple-primary)]/5 border border-[var(--apple-primary)]/20 rounded-[2.5rem] animate-apple-in">
                                            <div className="flex flex-col md:flex-row items-stretch justify-between gap-4">
                                                <div className="flex-1 bg-[var(--apple-card)] border-2 border-[var(--apple-border)] px-8 py-4 rounded-[2.5rem] flex items-center gap-6 shadow-inner">
                                                    <div className="h-12 w-12 rounded-2xl bg-emerald-500 flex items-center justify-center text-white shadow-lg shadow-emerald-500/20">
                                                        <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                    </div>
                                                    <div>
                                                        <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest">Retorno Proyectado</p>
                                                        <p className="text-3xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase">{horaRetorno || 'LIVE'}</p>
                                                    </div>
                                                </div>
                                                <button
                                                    onClick={procesarLlegada}
                                                    className="px-12 py-6 bg-[var(--istpet-gold)] text-white rounded-[2.5rem] text-xl font-black shadow-xl shadow-amber-500/20 hover:scale-[1.02] active:scale-95 transition-all"
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

                        <div className="apple-card p-8 border border-[var(--apple-border)]">
                            <h4 className="text-xs font-black text-[var(--apple-text-sub)] tracking-[0.2em] uppercase mb-4">Estado de Red</h4>
                            <div className="flex items-center gap-3">
                                <div className="w-2.5 h-2.5 rounded-full bg-emerald-500 animate-pulse"></div>
                                <span className="text-xs font-black text-[var(--apple-text-main)] uppercase tracking-widest">Servidores Operativos</span>
                            </div>
                        </div>

                    </div>
                </div>
            </div>
        </Layout>
    );
};

export default ControlOperativo;
