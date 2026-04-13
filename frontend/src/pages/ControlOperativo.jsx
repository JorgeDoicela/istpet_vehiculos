import React, { useState, useEffect, useRef, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTheme } from '../components/common/ThemeContext';
import Layout from '../components/layout/Layout';
import { logisticaService } from '../services/logisticaService';
import dashboardService from '../services/dashboardService';
import StatusBadge from '../components/common/StatusBadge';
import VehicleCard from '../components/logistica/VehicleCard';
import {
    agendaYmdFromApi,
    ymdLocalHoy,
    fmtFechaAgenda,
    fmtUltimaCargaAgenda,
    estadoAgendaChip,
    agendaPracticaVigenteParaSugerencia
} from '../utils/agendaUi';

/**
 * Control Operativo: Absolute SIGAFI Parity Edition 2026.
 * Guaranteed 1:1 naming with central database for idAlumno, idProfesor, idVehiculo.
 */
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
    const [salidaIdAlumno, setSalidaIdAlumno] = useState('');
    const [sugerencias, setSugerencias] = useState([]);
    const [mostrarSugerencias, setMostrarSugerencias] = useState(false);
    const [salidaLoading, setSalidaLoading] = useState(false);
    const [estudianteData, setEstudianteData] = useState(null);
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
    const [agendaFuente, setAgendaFuente] = useState('sigafi');
    const [agendaObtenidoEn, setAgendaObtenidoEn] = useState(null);
    const [filtroAgenda, setFiltroAgenda] = useState('');
    const [agendadosLoading, setAgendadosLoading] = useState(false);
    const [showAgendaDrawer, setShowAgendaDrawer] = useState(false);
    const [showInstructorMenu, setShowInstructorMenu] = useState(false);
    const [filtroInstructor, setFiltroInstructor] = useState('');
    const [isSearchingInstructor, setIsSearchingInstructor] = useState(false);

    const showNotification = (message, type = 'success') => {
        setNotification({ message, type });
        setTimeout(() => setNotification(null), 4000);
    };

    const agendaFiltrada = useMemo(() => {
        const q = filtroAgenda.trim().toLowerCase().replace(/\s+/g, ' ');
        if (!q) return agendadosHoy;
        return agendadosHoy.filter((ag) => {
            const id = (ag.idalumno || '').toLowerCase();
            const nom = (ag.AlumnoNombre || '').toLowerCase();
            const prof = (ag.ProfesorNombre || '').toLowerCase();
            const veh = (ag.VehiculoDetalle || '').toLowerCase();
            return id.includes(q) || nom.includes(q) || prof.includes(q) || veh.includes(q);
        });
    }, [agendadosHoy, filtroAgenda]);

    const { agendaBloqueHoy, agendaBloqueAnteriores } = useMemo(() => {
        const todayKey = ymdLocalHoy();
        const hoy = [];
        const ant = [];
        for (const ag of agendaFiltrada) {
            if (agendaYmdFromApi(ag.fecha) === todayKey) hoy.push(ag);
            else ant.push(ag);
        }
        return { agendaBloqueHoy: hoy, agendaBloqueAnteriores: ant };
    }, [agendaFiltrada]);

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
        cargarClasesActivas();
        if (activeTab === 'salida') {
            cargarVehiculosDisponibles();
            cargarInstructores();
            cargarAgendadosHoy();
        }
    }, [activeTab]);

    const cargarAgendadosHoy = async () => {
        setAgendadosLoading(true);
        try {
            const pack = await logisticaService.getAgendadosHoy();
            setAgendadosHoy(pack.practicas || []);
            setAgendaFuente(pack.fuenteDatos || 'sigafi');
            setAgendaObtenidoEn(pack.obtenidoEn ?? null);
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

    // Autocompletado y Sugerencias
    useEffect(() => {
        const timer = setTimeout(async () => {
            if (salidaIdAlumno.length >= 3 && salidaIdAlumno.length < 10 && activeTab === 'salida' && !salidaLoading && estudianteData?.idAlumno !== salidaIdAlumno) {
                const idKey = (v) => String(v ?? '').trim();
                const q = salidaIdAlumno.toLowerCase();
                const horaDesdeAgenda = (ag) => {
                    const hs = ag?.hora_salida;
                    if (hs == null || hs === '') return '';
                    const s = String(hs);
                    return s.includes(':') ? s.split(':').slice(0, 2).join(':') : s.substring(0, 5);
                };
                const busy = (id) => clasesActivas.some((ca) => idKey(ca.idAlumno) === idKey(id));

                const filaSugerenciaDesdeAgenda = (ag, nombrePreferido) => {
                    const sid = idKey(ag.idalumno);
                    const vigente = agendaPracticaVigenteParaSugerencia(ag);
                    return {
                        idAlumno: sid,
                        nombreCompleto: (nombrePreferido || ag.AlumnoNombre || '').trim() || sid,
                        esAgendado: vigente,
                        isBusy: busy(ag.idalumno),
                        hora: vigente ? horaDesdeAgenda(ag) : '',
                        vehiculo: vigente ? ag.VehiculoDetalle : '',
                        instructor: vigente ? ag.ProfesorNombre : ''
                    };
                };

                const localeMatch = agendadosHoy
                    .filter(
                        (ag) =>
                            idKey(ag.idalumno).startsWith(salidaIdAlumno) ||
                            (ag.AlumnoNombre || '').toLowerCase().includes(q)
                    )
                    .map((ag) => filaSugerenciaDesdeAgenda(ag, null));

                const dedupePreferVigente = (items) => {
                    const m = new Map();
                    for (const s of items) {
                        const k = idKey(s.idAlumno);
                        const prev = m.get(k);
                        if (!prev) m.set(k, s);
                        else if (s.esAgendado && !prev.esAgendado) m.set(k, s);
                    }
                    return Array.from(m.values());
                };

                try {
                    const serverMatch = await logisticaService.buscarSugerencias(salidaIdAlumno);
                    let combined = dedupePreferVigente([...localeMatch]);

                    const filaDesdeServidorSigafi = (sm) => {
                        const sid = idKey(sm.idAlumno);
                        const agSrv = !!sm.esAgendado;
                        return {
                            idAlumno: sid,
                            nombreCompleto: (sm.nombreCompleto || '').trim() || sid,
                            esAgendado: agSrv,
                            isBusy: sm.isBusy || busy(sid),
                            hora: (sm.horaAgenda || sm.hora || '').toString(),
                            vehiculo: sm.vehiculoAgenda || sm.vehiculo || '',
                            instructor: sm.instructorAgenda || sm.instructor || ''
                        };
                    };

                    const mejorFilaSugerencia = (prev, next) => {
                        if (next.esAgendado && !prev.esAgendado) return next;
                        if (prev.esAgendado && !next.esAgendado) return prev;
                        return next;
                    };

                    serverMatch.forEach((sm) => {
                        const sid = idKey(sm.idAlumno);
                        const rows = agendadosHoy.filter((a) => idKey(a.idalumno) === sid);
                        const agV =
                            rows.find((a) => agendaPracticaVigenteParaSugerencia(a)) ?? rows[0];

                        let fila;
                        if (agV) {
                            fila = filaSugerenciaDesdeAgenda(agV, sm.nombreCompleto);
                            if (!fila.esAgendado && sm.esAgendado) {
                                fila = {
                                    ...fila,
                                    esAgendado: true,
                                    hora: sm.horaAgenda || fila.hora,
                                    vehiculo: sm.vehiculoAgenda || fila.vehiculo,
                                    instructor: sm.instructorAgenda || fila.instructor
                                };
                            }
                        } else {
                            fila = filaDesdeServidorSigafi(sm);
                        }

                        const idx = combined.findIndex((c) => idKey(c.idAlumno) === sid);
                        if (idx < 0) combined.push(fila);
                        else combined[idx] = mejorFilaSugerencia(combined[idx], fila);
                    });
                    combined = dedupePreferVigente(combined);

                    setSugerencias(combined.slice(0, 5));
                    setMostrarSugerencias(combined.length > 0);
                } catch (e) {
                    console.error(e);
                }
            } else {
                setSugerencias([]);
                setMostrarSugerencias(false);
            }
        }, 300);
        return () => clearTimeout(timer);
    }, [salidaIdAlumno, agendadosHoy, estudianteData, clasesActivas]);

    // Autobúsqueda definitiva
    useEffect(() => {
        const timer = setTimeout(() => {
            if (salidaIdAlumno.length === 10 && activeTab === 'salida') {
                if (estudianteData?.idAlumno === salidaIdAlumno) return;
                ejecutarBusquedaEstudiante(salidaIdAlumno, 'manual');
            }
        }, 800);
        return () => clearTimeout(timer);
    }, [salidaIdAlumno, activeTab, estudianteData]);

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

    const buildAgendaCtx = (ag) => {
        if (!ag) return null;
        return {
            idPractica: ag.idPractica, // Puede ser 0 si es solo planificado
            idVehiculo: ag.idvehiculo,
            idProfesor: ag.idProfesor,
            idAsignacionHorario: ag.idAsignacionHorario ?? ag.IdAsignacionHorario
        };
    };

    const ejecutarBusquedaEstudiante = async (idAlumnoEnviado = null, source = 'manual', agendaCtx = null) => {
        const idAlumnoABuscar = idAlumnoEnviado || salidaIdAlumno;
        if (!idAlumnoABuscar || idAlumnoABuscar.length < 10) return;

        setSalidaLoading(true);
        setEstudianteData(null);
        try {
            const data = await logisticaService.buscarEstudiante(idAlumnoABuscar, agendaCtx);
            setEstudianteData(data);
            if (data.tipoLicencia) setFiltroLicencia(data.tipoLicencia);

            const idInstr = data.idPracticaInstructor != null && String(data.idPracticaInstructor).trim() !== ''
                ? String(data.idPracticaInstructor).trim()
                : '';
            const tieneSugerencia =
                !!idInstr || data.idPracticaCentral != null;
            if (tieneSugerencia) {
                await aplicarSugerenciaManual(data);
                showNotification(
                    source === 'agenda'
                        ? 'Alumno, vehículo e instructor según la fila de agenda'
                        : 'Sugerencia de práctica aplicada (SIGAFI)'
                );
            } else {
                showNotification('Estudiante localizado');
            }
        } catch (err) {
            const d = err.response?.data;
            const apiMsg = d?.message ?? d?.Message;
            showNotification(apiMsg || err.message || 'No localizado', 'error');
        } finally {
            setSalidaLoading(false);
        }
    };

    const aplicarSugerenciaManual = async (dataOverride = null) => {
        const data = dataOverride || estudianteData;
        if (!data) return;

        const idInstr = data.idPracticaInstructor != null && String(data.idPracticaInstructor).trim() !== ''
            ? String(data.idPracticaInstructor).trim()
            : '';

        try {
            if (idInstr) {
                let sInstr = instructores.find((i) => i.idInstructor === idInstr);
                if (!sInstr) {
                    const fresh = await logisticaService.getInstructores();
                    setInstructores(fresh);
                    sInstr = fresh.find((i) => i.idInstructor === idInstr);
                }
                if (!sInstr && data.practicaInstructor) {
                    sInstr = { idInstructor: idInstr, fullName: data.practicaInstructor };
                }
                if (sInstr) setInstructorSeleccionado(sInstr);
            }

            if (data.idPracticaCentral != null) {
                const vid = Number(data.idPracticaCentral);
                let sVeh = vehiculos.find((v) => v.idVehiculo === vid);
                if (!sVeh) {
                    const freshV = await logisticaService.getVehiculosDisponibles();
                    setVehiculos(freshV);
                    sVeh = freshV.find((v) => v.idVehiculo === vid);
                }
                if (!sVeh && data.practicaVehiculo) {
                    const m = String(data.practicaVehiculo).match(/#([\w\-]+)/);
                    sVeh = {
                        idVehiculo: vid,
                        numeroVehiculo: m ? m[1] : (data.numeroVehiculo || "0"),
                        vehiculoStr: data.practicaVehiculo,
                        instructorNombre: 'DOCENTE ASIGNADO'
                    };
                }

                if (sVeh) setVehiculoSeleccionado(sVeh);
            }
        } catch (err) {
            console.error('Error aplicando sugerencia:', err);
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
            await logisticaService.registrarSalida({
                idMatricula: estudianteData.idMatricula,
                idVehiculo: vehiculoSeleccionado.idVehiculo,
                idInstructor: instructorSeleccionado.idInstructor,
                idAsignacionHorario: estudianteData.idAsignacionHorario, // 🚀 Vínculo con agenda
                observaciones: "Salida Regular Control Parity"
            });
            showNotification('¡Vehículo en pista registrado!');
            setEstudianteData(null);
            setSalidaIdAlumno('');
            setVehiculoSeleccionado(null);
            setInstructorSeleccionado(null);
            setFiltroLicencia(null);
            cargarVehiculosDisponibles();
            cargarClasesActivas(); // 🚀 Refrescar pestaña Llegada inmediatamente

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
            await logisticaService.registrarLlegada({ idPractica: claseSeleccionada.idPractica });
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
                        <div className="px-8 lg:px-10 mb-2 lg:mb-4">
                            <p className="text-[10px] lg:text-xs font-black text-[var(--istpet-gold)] uppercase tracking-[0.2em] mb-0">
                                {new Date().getHours() < 12 ? 'Buenos días' : new Date().getHours() < 19 ? 'Buenas tardes' : 'Buenas noches'}
                            </p>
                            <h2 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase leading-tight">
                                Bienvenida Operativa
                            </h2>
                        </div>

                        {activeTab === 'salida' ? (
                            <div className="apple-card">
                                <div className="mb-10 px-2 flex items-center justify-between">
                                    <h3 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tight">Registro de Salida</h3>
                                    <div className="flex flex-col items-end leading-tight">
                                        <span className="text-[8px] lg:text-[9px] text-[var(--apple-text-sub)] opacity-60 uppercase font-black tracking-widest mb-0.5">{fechaHoy}</span>
                                        <span className="text-sm lg:text-lg font-black text-[var(--apple-text-main)] tracking-tighter tabular-nums">{horaRetorno || '--:--:--'}</span>
                                    </div>
                                </div>

                                <div className="space-y-8">
                                    <div className="relative group">
                                        <label className="absolute left-0 -top-4 px-2 bg-[var(--apple-card)] backdrop-blur-md text-[9px] font-black text-[var(--apple-text-main)] tracking-[0.2em] uppercase transition-all group-focus-within:text-[var(--istpet-gold)] z-10">
                                            Identificación (idAlumno)
                                        </label>
                                        <div className="relative flex items-center gap-3">
                                            <input
                                                type="text"
                                                inputMode="numeric"
                                                pattern="[0-9]*"
                                                placeholder="CEDULA / ID"
                                                maxLength={10}
                                                value={salidaIdAlumno}
                                                 onChange={(e) => { const val = e.target.value.replace(/\D/g, ''); setSalidaIdAlumno(val); if (val.length < 10) { setEstudianteData(null); setVehiculoSeleccionado(null); setInstructorSeleccionado(null); } }}
                                                className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[1.5rem] px-5 py-3 text-base lg:text-lg font-bold text-[var(--apple-text-main)] focus:border-[var(--istpet-gold)] focus:bg-[var(--apple-card)] outline-none transition-all shadow-inner tracking-widest"
                                            />

                                            {mostrarSugerencias && (
                                                <div className="absolute left-0 right-0 top-full mt-2 bg-[var(--apple-card)] backdrop-blur-2xl border border-[var(--apple-border)] rounded-[2rem] shadow-2xl z-[100] overflow-hidden animate-apple-in p-2">
                                                    {sugerencias.map((s, idx) => (
                                                        <div
                                                            key={s.idAlumno}
                                                            onClick={() => {
                                                                setSalidaIdAlumno(s.idAlumno);
                                                                setSugerencias([]);
                                                                setMostrarSugerencias(false);
                                                                ejecutarBusquedaEstudiante(s.idAlumno);
                                                            }}
                                                            className={`p-4 cursor-pointer hover:bg-[var(--apple-primary)]/10 transition-all flex items-center justify-between rounded-2xl group/item ${idx !== sugerencias.length - 1 ? 'mb-1' : ''}`}
                                                        >
                                                            <div className="flex-1 text-left py-2">
                                                                <p className="text-[15px] font-black text-[var(--apple-text-main)] uppercase tracking-tight leading-none mb-1.5">
                                                                    {s.nombreCompleto}
                                                                </p>
                                                                <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                                                                    <p className="text-[11px] font-black text-[var(--apple-text-sub)] tracking-[0.2em]">
                                                                        {s.idAlumno}
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
                                                                            <span className={`text-[9px] font-black px-2 py-0.5 rounded-full shadow-sm animate-pulse uppercase ${s.isBusy ? 'bg-rose-500 text-white' : 'bg-emerald-500 text-white'}`}>
                                                                                {s.isBusy ? 'EN PISTA' : 'AGENDADO'}
                                                                            </span>
                                                                        </div>
                                                                    ) : (
                                                                        <div className="flex items-center gap-2">
                                                                            <span className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase">REGULAR</span>
                                                                            {s.isBusy && (
                                                                                <span className="text-[9px] font-black bg-rose-500 text-white px-2 py-0.5 rounded-full shadow-sm animate-bounce-slow uppercase">EN PISTA</span>
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

                                    {estudianteData ? (
                                        <div className="space-y-6 animate-apple-in">
                                            <div className="apple-glass rounded-[2.5rem] p-4 lg:p-5 shadow-sm hover:shadow-md transition-all group overflow-hidden relative">
                                                <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 lg:gap-8 relative z-10">
                                                    <div className="flex-1 min-w-0 text-left">
                                                        <div className="flex items-center gap-3 mb-1">
                                                            <h4 className="text-lg font-black text-[var(--apple-text-main)] uppercase tracking-tighter leading-none">
                                                                {estudianteData.nombreCompleto}
                                                            </h4>
                                                            <span className="shrink-0 px-2 py-0.5 bg-[var(--apple-bg)] border border-[var(--apple-border)] rounded-md text-[8px] font-black text-[var(--istpet-gold)] uppercase tracking-widest">
                                                                {estudianteData.idPeriodo}
                                                            </span>
                                                            {estudianteData.isBusy && (
                                                                <span className="shrink-0 px-2 py-0.5 bg-rose-500 text-white rounded-md text-[8px] font-black uppercase tracking-widest animate-pulse shadow-sm shadow-rose-500/20">
                                                                    YA EN PISTA
                                                                </span>
                                                            )}
                                                        </div>
                                                        
                                                        {/* 📅 Horario Granular Discovery Integration */}
                                                        <div className="flex flex-wrap items-center gap-2 mt-1.5">
                                                            <p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-wide opacity-60 leading-snug">
                                                                {estudianteData.detalleMatriculaSigafi?.trim()
                                                                    ? estudianteData.detalleMatriculaSigafi
                                                                    : `${estudianteData.nivel} • ${estudianteData.paralelo}`}
                                                            </p>
                                                            {estudianteData.horarioProximo && (
                                                                <div className="flex items-center gap-2 px-2 py-0.5 bg-[var(--apple-primary)]/10 rounded-full border border-[var(--apple-primary)]/20 animate-apple-in">
                                                                    <svg className="h-3 w-3 text-[var(--apple-primary)]" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                                    <span className="text-[9px] font-black text-[var(--apple-primary)] uppercase">PLAN: {estudianteData.horarioProximo}</span>
                                                                </div>
                                                            )}
                                                            {estudianteData.asistenciaHoy && (
                                                                <div className="flex items-center gap-1 px-2 py-0.5 bg-emerald-500/10 rounded-full border border-emerald-500/20">
                                                                    <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></div>
                                                                    <span className="text-[9px] font-black text-emerald-600 uppercase">ASISTENCIA OK</span>
                                                                </div>
                                                            )}
                                                        </div>
                                                    </div>

                                                    <div className="flex items-center justify-around lg:justify-end gap-5 lg:gap-7 py-2 lg:py-0 lg:pl-7">
                                                        <div className="text-center group/stat">
                                                            <span className="block text-[7px] font-black text-[var(--apple-text-sub)] uppercase mb-0.5 opacity-50 group-hover/stat:text-[var(--istpet-gold)] transition-colors">Asistencia</span>
                                                            <span className={`block text-[10px] font-black leading-none uppercase ${estudianteData.asistenciaHoy ? 'text-emerald-500' : 'text-amber-500'}`}>
                                                                {estudianteData.asistenciaHoy ? 'PRESENTE' : 'PENDIENTE'}
                                                            </span>
                                                        </div>
                                                        <div className="h-5 w-px bg-[var(--apple-border)]/30"></div>
                                                        <div className="text-center">
                                                            <span className="block text-[7px] font-black text-[var(--apple-text-sub)] uppercase mb-0.5 opacity-50">Jornada</span>
                                                            <span className="block text-[9px] font-black text-[var(--apple-text-main)] uppercase leading-none">{estudianteData.jornada}</span>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div className="absolute top-0 right-0 h-full w-24 bg-gradient-to-l from-[var(--apple-primary)]/[0.03] to-transparent skew-x-12 translate-x-12 group-hover:translate-x-8 transition-transform duration-700"></div>
                                            </div>
                                        </div>
                                    ) : !salidaLoading && salidaIdAlumno.length >= 1 && (
                                        <div className="p-8 border-2 border-dashed border-[var(--apple-border)] rounded-[2rem] text-center">
                                            <p className="text-[var(--apple-text-sub)] font-bold text-sm uppercase">Ingrese ID para validar matrícula</p>
                                        </div>
                                    )}

                                    <div className="pt-6">
                                        <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between mb-4 gap-3 px-1 group">
                                            <div className="flex-1 text-left min-w-0">
                                                <h3 className="text-[9px] font-black uppercase tracking-[0.2em] mb-2 px-2 transition-colors duration-300 group-focus-within:text-[var(--istpet-gold)] text-[var(--apple-text-main)]">Asignar Unidad</h3>
                                            </div>

                                            <div className="flex items-center gap-2 flex-[2]">
                                                <div className="relative flex-1 group/search">
                                                    <div className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)] group-focus-within/search:text-[var(--istpet-gold)] transition-colors">
                                                        <svg className="h-3 w-3 lg:h-4 lg:w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                                                    </div>
                                                    <input
                                                        type="text"
                                                        placeholder="BUSCAR VEHÍCULO..."
                                                        value={filtroVehiculo}
                                                        onChange={(e) => setFiltroVehiculo(e.target.value.toUpperCase())}
                                                        className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[1.5rem] pl-10 pr-10 py-2.5 text-[10px] lg:text-[11px] font-black tracking-widest text-[var(--apple-text-main)] placeholder:text-[var(--apple-text-sub)]/30 focus:border-[var(--istpet-gold)] shadow-inner transition-all outline-none"
                                                    />
                                                    {filtroVehiculo && (
                                                        <button onClick={() => setFiltroVehiculo('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)] hover:text-red-500 transition-colors">
                                                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                                        </button>
                                                    )}
                                                </div>

                                                <div className="flex bg-[var(--apple-bg)] p-1 rounded-[1.5rem] border border-[var(--apple-border)] shadow-sm shrink-0">
                                                    {['C', 'D', 'E'].map(lic => (
                                                        <button
                                                            key={lic}
                                                            onClick={() => setFiltroLicencia(filtroLicencia === lic ? null : lic)}
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
                                                                v.vehiculoStr?.toLowerCase().includes(term) ||
                                                                v.numeroVehiculo?.toString().includes(term);
                                                            return matchLicencia && matchFiltro;
                                                        });

                                                        if (filtered.length === 0) {
                                                            return (
                                                                <div className="col-span-full py-12 text-center apple-glass rounded-[2rem] border border-dashed border-[var(--apple-border)] shadow-inner">
                                                                     <p className="text-[10px] font-black text-[var(--apple-text-sub)] uppercase tracking-[0.2em]">SIN UNIDADES DISPONIBLES</p>
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
                                                        <p className="text-[var(--apple-text-sub)] font-black uppercase text-xs tracking-widest animate-pulse">Sincronizando flota...</p>
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    </div>

                                    <div className="pt-6 px-1 relative">
                                        <div className="mb-2 transition-all">
                                            <p className={`text-[9px] font-black uppercase tracking-[0.2em] px-2 transition-colors duration-300 ${showInstructorMenu ? 'text-[var(--istpet-gold)]' : 'text-[var(--apple-text-main)]'}`}>Instructor (idProfesor)</p>
                                        </div>

                                        <div className="relative group">
                                            <div className="relative flex items-center">
                                                <input
                                                    ref={instructorInputRef}
                                                    type="text"
                                                    readOnly={!isSearchingInstructor}
                                                    value={isSearchingInstructor && showInstructorMenu ? filtroInstructor : (instructorSeleccionado?.fullName || '')}
                                                    onChange={(e) => {
                                                        const val = e.target.value.toUpperCase();
                                                        setFiltroInstructor(val);
                                                        if (!showInstructorMenu) setShowInstructorMenu(true);
                                                    }}
                                                    onClick={() => setShowInstructorMenu(true)}
                                                    className={`w-full bg-[var(--apple-bg)] border-2 rounded-[1.5rem] pl-6 pr-24 py-3 text-xs font-bold text-[var(--apple-text-main)] transition-all shadow-inner outline-none uppercase placeholder:text-[var(--apple-text-sub)]/30 ${showInstructorMenu ? 'border-[var(--istpet-gold)]' : 'border-[var(--apple-border)]'}`}
                                                />
                                                <button onClick={() => setIsSearchingInstructor(!isSearchingInstructor)} className="absolute right-4 top-1/2 -translate-y-1/2 text-[var(--apple-text-sub)] hover:text-[var(--istpet-gold)] p-2">
                                                    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="4"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                                                </button>
                                            </div>

                                            {showInstructorMenu && (
                                                <>
                                                    <div className="fixed inset-0 z-[140]" onClick={() => setShowInstructorMenu(false)} />
                                                    <div className="absolute left-0 right-0 top-full mt-3 bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[2.2rem] shadow-2xl z-[150] overflow-hidden animate-apple-in p-3 max-h-[300px] overflow-y-auto">
                                                        {instructores.filter(i => !filtroInstructor || i.fullName.toUpperCase().includes(filtroInstructor)).map((i) => (
                                                            <div
                                                                key={i.idInstructor}
                                                                onClick={() => {
                                                                    setInstructorSeleccionado(i);
                                                                    setShowInstructorMenu(false);
                                                                    setFiltroInstructor('');
                                                                }}
                                                                className={`p-4 cursor-pointer hover:bg-[var(--apple-primary)]/10 transition-all rounded-2xl ${instructorSeleccionado?.idInstructor === i.idInstructor ? 'bg-[var(--apple-primary)]/10 text-[var(--istpet-gold)]' : 'text-[var(--apple-text-main)]'}`}
                                                            >
                                                                <p className="text-[11px] font-black uppercase tracking-tight">{i.fullName}</p>
                                                            </div>
                                                        ))}
                                                    </div>
                                                </>
                                            )}
                                        </div>
                                    </div>

                                    <div className="pt-6 border-t border-[var(--apple-border)] mt-4">
                                        <button
                                            onClick={procesarSalida}
                                            disabled={!estudianteData || estudianteData.isBusy || !vehiculoSeleccionado || !instructorSeleccionado}
                                            className={`w-full py-4 rounded-full text-sm font-black transition-all ${(!estudianteData || estudianteData.isBusy || !vehiculoSeleccionado || !instructorSeleccionado) ? 'bg-[var(--apple-border)] text-[var(--apple-text-sub)] cursor-not-allowed opacity-30' : 'btn-apple-primary shadow-xl shadow-[var(--istpet-gold)]/20 hover:scale-[1.01]'}`}
                                        >
                                            {!estudianteData ? 'VALIDAR ID ALUMNO' :
                                                estudianteData.isBusy ? 'ESTUDIANTE EN PISTA' :
                                                    !vehiculoSeleccionado ? 'SELECCIONAR UNIDAD' :
                                                        !instructorSeleccionado ? 'SELECCIONAR INSTRUCTOR' :
                                                            '✓ REGISTRAR SALIDA'}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ) : (
                            <div className="apple-card">
                                <div className="flex items-center justify-between mb-5">
                                    <h3 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tight">Registro de Llegada</h3>
                                    <div className="flex items-center gap-2">
                                        <span className="text-sm lg:text-lg font-black text-[var(--apple-text-main)] tracking-tighter tabular-nums">{horaRetorno || '--:--:--'}</span>
                                        <div className="flex gap-1.5 items-center text-[9px] font-black text-emerald-500 uppercase tracking-widest">
                                            <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></div>
                                            EN PISTA
                                        </div>
                                    </div>
                                </div>

                                <div className="space-y-8">
                                     <div className="max-h-[440px] overflow-y-auto pr-2 custom-scrollbar">
                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                            {clasesActivas.length > 0 ? (
                                                clasesActivas.map(c => (
                                                    <div
                                                        key={c.idPractica}
                                                        onClick={() => setClaseSeleccionada(c)}
                                                        className={`p-4 rounded-[2.5rem] border-2 transition-all cursor-pointer ${claseSeleccionada?.idPractica === c.idPractica ? 'bg-[var(--istpet-gold)]/10 border-[var(--istpet-gold)] shadow-lg' : 'bg-[var(--apple-bg)] border-[var(--apple-border)] hover:border-[var(--apple-text-sub)]'}`}
                                                    >
                                                        <div className="flex justify-between items-start mb-3">
                                                            <div className="px-2 py-0.5 bg-[var(--apple-card)] border border-[var(--apple-border)] rounded-lg text-[10px] font-black text-[var(--apple-text-main)]">#{c.numeroVehiculo}</div>
                                                            <StatusBadge status="Pista" />
                                                        </div>
                                                        <h5 className="font-black text-xs text-[var(--apple-text-main)] uppercase mb-1">{c.instructor}</h5>
                                                        <p className="text-[10px] text-[var(--apple-text-sub)] truncate mb-3">Estudiante: {c.estudiante}</p>
                                                        <div className="flex items-center gap-2 text-[9px] font-black text-[var(--apple-text-sub)] uppercase">
                                                            <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                            Salida: {new Date(c.salida).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                                        </div>
                                                    </div>
                                                ))
                                            ) : (
                                                <div className="col-span-full py-12 text-center opacity-40 apple-glass rounded-3xl">
                                                    <p className="text-[var(--apple-text-sub)] font-bold tracking-widest text-xs uppercase text-center">Sin registros activos</p>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {claseSeleccionada && (
                                        <div className="mt-6 border-t border-[var(--apple-border)] pt-6 animate-apple-in">
                                            <button
                                                onClick={procesarLlegada}
                                                className="w-full py-4 bg-[var(--istpet-gold)] text-white rounded-full text-base font-black shadow-xl shadow-amber-500/20 hover:scale-[1.01]"
                                            >
                                                Confirmar Llegada: {claseSeleccionada.idAlumno}
                                            </button>
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>

                    <div className="hidden lg:block lg:col-span-5 xl:col-span-4 space-y-6 animate-apple-in" style={{ animationDelay: '0.3s' }}>
                        <div className="apple-glass rounded-[2rem] p-5 border border-[var(--apple-border)] relative overflow-hidden group">
                            <div className="flex flex-col gap-3 mb-4">
                                <div className="flex items-start justify-between gap-2">
                                    <div className="min-w-0">
                                        <h4 className="text-[9px] font-black text-[var(--apple-text-main)] uppercase tracking-[0.2em]">Agenda SIGAFI</h4>
                                        <p className="text-[8px] font-bold text-[var(--apple-text-sub)] uppercase tracking-wider mt-1 flex flex-wrap items-center gap-x-2 gap-y-0.5">
                                            <span className={agendaFuente === 'local' ? 'text-amber-700' : ''}>{agendaFuente === 'local' ? 'Origen: espejo local' : 'Origen: SIGAFI'}</span>
                                            {agendaObtenidoEn ? <span>· Actualizado {fmtUltimaCargaAgenda(agendaObtenidoEn)}</span> : null}
                                        </p>
                                    </div>
                                    <button
                                        type="button"
                                        title="Actualizar agenda"
                                        disabled={agendadosLoading}
                                        onClick={() => cargarAgendadosHoy()}
                                        className="shrink-0 h-9 w-9 flex items-center justify-center rounded-xl bg-[var(--apple-primary)]/10 text-[var(--apple-primary)] hover:bg-[var(--apple-primary)] hover:text-white transition-colors disabled:opacity-50"
                                    >
                                        <svg className={`h-4 w-4 ${agendadosLoading ? 'animate-spin' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.2">
                                            <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                        </svg>
                                    </button>
                                </div>
                                <input
                                    type="search"
                                    value={filtroAgenda}
                                    onChange={(e) => setFiltroAgenda(e.target.value)}
                                    placeholder="Buscar cédula, nombre, instructor o vehículo…"
                                    className="w-full bg-[var(--apple-bg)] border border-[var(--apple-border)] rounded-xl px-3 py-2 text-[11px] font-semibold text-[var(--apple-text-main)] placeholder:text-[var(--apple-text-sub)]/50 outline-none focus:border-[var(--istpet-gold)]"
                                />
                                <div className="flex flex-wrap items-center gap-2 text-[8px] font-black uppercase tracking-wider text-[var(--apple-text-sub)]">
                                    <span className="text-[var(--apple-primary)] bg-[var(--apple-primary)]/10 px-2 py-0.5 rounded-full">{agendaFiltrada.length} vista</span>
                                    <span>{agendadosHoy.length} cargados</span>
                                    <span>· {agendaBloqueHoy.length} hoy</span>
                                </div>
                            </div>

                            <div className="space-y-4 max-h-[500px] overflow-y-auto pr-2 custom-scrollbar">
                                {agendadosHoy.length === 0 && !agendadosLoading ? (
                                    <div className="py-10 text-center opacity-40">
                                        <p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-widest">Sin prácticas recientes</p>
                                    </div>
                                ) : null}
                                {agendadosHoy.length > 0 && agendaFiltrada.length === 0 ? (
                                    <div className="py-10 text-center opacity-50">
                                        <p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-widest">Sin coincidencias con el filtro</p>
                                    </div>
                                ) : null}
                                {agendaBloqueHoy.length > 0 ? (
                                    <>
                                        <p className="text-[9px] font-black text-[var(--apple-primary)] uppercase tracking-[0.2em]">Hoy</p>
                                        {agendaBloqueHoy.map((ag) => {
                                            const chip = estadoAgendaChip(ag.estadoOperativo);
                                            return (
                                                <div key={ag.idPractica} className="bg-[var(--apple-card)] p-4 rounded-2xl border border-[var(--apple-border)] group/item hover:border-[var(--apple-primary)]/50 transition-all shadow-sm">
                                                    <div className="flex justify-between items-start mb-2 gap-2 flex-wrap">
                                                        <span className="text-[10px] font-black text-[var(--apple-primary)] leading-tight">
                                                            <span className="block text-[9px] text-[var(--apple-text-sub)] uppercase tracking-tight">{fmtFechaAgenda(ag.fecha)}</span>
                                                            {ag.estadoOperativo === 'pendiente' && ag.EsPlanificado && ag.HoraPlanificadaInicio
                                                                ? <span className="text-[var(--istpet-gold)] font-black text-[9px] block mt-0.5">{ag.HoraPlanificadaInicio} - {ag.HoraPlanificadaFin}</span>
                                                                : (ag.hora_salida != null ? String(ag.hora_salida).substring(0, 5) : '—')}
                                                        </span>
                                                        <div className="flex flex-wrap items-center gap-1.5 justify-end">
                                                            <span className={`text-[8px] font-black uppercase px-2 py-0.5 rounded-full tracking-tight ${chip.cls}`}>{chip.label}</span>
                                                            <span className="text-[9px] font-black bg-[var(--apple-bg)] text-[var(--apple-text-sub)] px-1.5 py-0.5 rounded tracking-tighter uppercase font-mono shrink-0">{ag.VehiculoDetalle}</span>
                                                        </div>
                                                    </div>
                                                    <p className="text-[11px] font-bold text-[var(--apple-text-main)] uppercase truncate mb-1">{ag.AlumnoNombre || ag.idalumno}</p>
                                                    <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-tighter truncate opacity-60 italic">{ag.ProfesorNombre}</p>
                                                    <button
                                                        type="button"
                                                        onClick={() => {
                                                            setSalidaIdAlumno(ag.idalumno);
                                                            ejecutarBusquedaEstudiante(ag.idalumno, 'agenda', buildAgendaCtx(ag));
                                                        }}
                                                        className="w-full mt-3 py-2 bg-[var(--apple-primary)]/10 text-[var(--apple-primary)] rounded-xl text-[9px] font-black uppercase tracking-widest opacity-0 group-hover/item:opacity-100 transition-all hover:bg-[var(--apple-primary)] hover:text-white shadow-sm"
                                                    >
                                                        Cargar alumno
                                                    </button>
                                                </div>
                                            );
                                        })}
                                    </>
                                ) : null}
                                {agendaBloqueAnteriores.length > 0 ? (
                                    <>
                                        <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-[0.2em] pt-1">Anteriores</p>
                                        {agendaBloqueAnteriores.map((ag) => {
                                            const chip = estadoAgendaChip(ag.estadoOperativo);
                                            return (
                                                <div key={ag.idPractica} className="bg-[var(--apple-card)] p-4 rounded-2xl border border-[var(--apple-border)] group/item hover:border-[var(--apple-primary)]/50 transition-all shadow-sm opacity-95">
                                                    <div className="flex justify-between items-start mb-2 gap-2 flex-wrap">
                                                        <span className="text-[10px] font-black text-[var(--apple-primary)] leading-tight">
                                                            <span className="block text-[9px] text-[var(--apple-text-sub)] uppercase tracking-tight">{fmtFechaAgenda(ag.fecha)}</span>
                                                            {ag.estadoOperativo === 'pendiente' && ag.EsPlanificado && ag.HoraPlanificadaInicio
                                                                ? <span className="text-[var(--istpet-gold)] font-black text-[9px] block mt-0.5">{ag.HoraPlanificadaInicio} - {ag.HoraPlanificadaFin}</span>
                                                                : (ag.hora_salida != null ? String(ag.hora_salida).substring(0, 5) : '—')}
                                                        </span>
                                                        <div className="flex flex-wrap items-center gap-1.5 justify-end">
                                                            <span className={`text-[8px] font-black uppercase px-2 py-0.5 rounded-full tracking-tight ${chip.cls}`}>{chip.label}</span>
                                                            <span className="text-[9px] font-black bg-[var(--apple-bg)] text-[var(--apple-text-sub)] px-1.5 py-0.5 rounded tracking-tighter uppercase font-mono shrink-0">{ag.VehiculoDetalle}</span>
                                                        </div>
                                                    </div>
                                                    <p className="text-[11px] font-bold text-[var(--apple-text-main)] uppercase truncate mb-1">{ag.AlumnoNombre || ag.idalumno}</p>
                                                    <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-tighter truncate opacity-60 italic">{ag.ProfesorNombre}</p>
                                                    <button
                                                        type="button"
                                                        onClick={() => {
                                                            setSalidaIdAlumno(ag.idalumno);
                                                            ejecutarBusquedaEstudiante(ag.idalumno, 'agenda', buildAgendaCtx(ag));
                                                        }}
                                                        className="w-full mt-3 py-2 bg-[var(--apple-primary)]/10 text-[var(--apple-primary)] rounded-xl text-[9px] font-black uppercase tracking-widest opacity-0 group-hover/item:opacity-100 transition-all hover:bg-[var(--apple-primary)] hover:text-white shadow-sm"
                                                    >
                                                        Cargar alumno
                                                    </button>
                                                </div>
                                            );
                                        })}
                                    </>
                                ) : null}
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {activeTab === 'salida' && (
                <button
                    onClick={() => setShowAgendaDrawer(true)}
                    className="lg:hidden fixed bottom-20 right-4 z-50 h-14 w-14 flex items-center justify-center rounded-[1.4rem] bg-[var(--istpet-navy)] text-white shadow-2xl"
                >
                    <svg className="h-6 w-6 text-[var(--istpet-gold)]" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                    </svg>
                    {agendadosHoy.length > 0 && <span className="absolute -top-2.5 -right-2.5 text-[13px] font-black text-[var(--istpet-gold)]">{agendadosHoy.length}</span>}
                </button>
            )}

            {showAgendaDrawer && (
                <div className="lg:hidden fixed inset-0 z-[200] flex flex-col justify-end animate-apple-in">
                    <div className="absolute inset-0 bg-black/30 backdrop-blur-md" onClick={() => setShowAgendaDrawer(false)} />
                    <div className="relative bg-[var(--apple-bg)] rounded-t-[2.5rem] max-h-[85vh] flex flex-col shadow-2xl overflow-hidden">
                        <div className="flex justify-center pt-3 pb-1"><div className="w-10 h-1 rounded-full bg-[var(--apple-border)]" /></div>
                        <div className="flex items-center justify-between px-5 py-4 gap-3 border-b border-[var(--apple-border)]/40">
                            <div className="min-w-0 flex-1">
                                <h3 className="text-sm font-black text-[var(--apple-text-main)] uppercase tracking-[0.15em]">Agenda</h3>
                                <p className="text-[8px] font-bold text-[var(--apple-text-sub)] uppercase tracking-wider mt-0.5 truncate">
                                    {agendaFuente === 'local' ? 'Espejo local' : 'SIGAFI'}
                                    {agendaObtenidoEn ? ` · ${fmtUltimaCargaAgenda(agendaObtenidoEn)}` : ''}
                                </p>
                            </div>
                            <button
                                type="button"
                                title="Actualizar"
                                disabled={agendadosLoading}
                                onClick={() => cargarAgendadosHoy()}
                                className="h-9 w-9 shrink-0 flex items-center justify-center rounded-xl bg-[var(--apple-primary)]/10 text-[var(--apple-primary)] disabled:opacity-50"
                            >
                                <svg className={`h-4 w-4 ${agendadosLoading ? 'animate-spin' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.2">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                </svg>
                            </button>
                            <button type="button" onClick={() => setShowAgendaDrawer(false)} className="h-8 w-8 shrink-0 flex items-center justify-center rounded-full bg-[var(--apple-border)]/40 text-[var(--apple-text-sub)]"><svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg></button>
                        </div>
                        <div className="px-5 pt-3 pb-2 space-y-2 shrink-0">
                            <input
                                type="search"
                                value={filtroAgenda}
                                onChange={(e) => setFiltroAgenda(e.target.value)}
                                placeholder="Buscar cédula, nombre…"
                                className="w-full bg-[var(--apple-card)] border border-[var(--apple-border)] rounded-xl px-3 py-2.5 text-[13px] font-semibold text-[var(--apple-text-main)] outline-none focus:border-[var(--istpet-gold)]"
                            />
                            <p className="text-[8px] font-black uppercase text-[var(--apple-text-sub)] tracking-wider">{agendaFiltrada.length} mostrados · {agendaBloqueHoy.length} hoy</p>
                        </div>
                        <div className="overflow-y-auto flex-1 custom-scrollbar pb-10 min-h-0">
                            {agendadosHoy.length === 0 && !agendadosLoading ? (
                                <div className="py-16 text-center opacity-40 px-6"><p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-[0.2em]">Sin prácticas recientes</p></div>
                            ) : null}
                            {agendadosHoy.length > 0 && agendaFiltrada.length === 0 ? (
                                <div className="py-16 text-center opacity-50 px-6"><p className="text-[10px] font-bold text-[var(--apple-text-sub)] uppercase tracking-[0.2em]">Sin coincidencias</p></div>
                            ) : null}
                            {agendaBloqueHoy.length > 0 ? (
                                <div className="px-5 pt-2">
                                    <p className="text-[9px] font-black text-[var(--apple-primary)] uppercase tracking-widest mb-2">Hoy</p>
                                    {agendaBloqueHoy.map((ag, idx) => {
                                        const chip = estadoAgendaChip(ag.estadoOperativo);
                                        return (
                                            <div key={ag.idPractica} className={`group px-1 ${idx !== 0 ? 'border-t border-[var(--apple-border)]/40 pt-4 mt-4' : ''}`}>
                                                <div className="flex items-start gap-3">
                                                    <div className="flex-1 min-w-0">
                                                        <div className="flex flex-wrap items-center gap-2 mb-1">
                                                            <span className={`text-[8px] font-black uppercase px-2 py-0.5 rounded-full ${chip.cls}`}>{chip.label}</span>
                                                        </div>
                                                        <p className="text-[12px] font-black text-[var(--apple-text-main)] uppercase truncate">{ag.AlumnoNombre || ag.idalumno}</p>
                                                        <div className="flex flex-wrap items-center gap-2 mt-2">
                                                            <span className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase px-2 py-0.5 rounded-md bg-[var(--apple-border)]/30">{fmtFechaAgenda(ag.fecha)}</span>
                                                            <span className="text-[10px] font-black text-[var(--istpet-gold)] bg-[var(--istpet-gold)]/10 px-2 py-0.5 rounded-md">{ag.hora_salida != null ? String(ag.hora_salida).substring(0, 5) : '—'}</span>
                                                            <span className="px-2 py-0.5 bg-[var(--apple-border)]/40 rounded-md text-[9px] font-bold text-[var(--apple-text-main)] uppercase truncate max-w-[9rem]">{ag.VehiculoDetalle}</span>
                                                        </div>
                                                    </div>
                                                    <button type="button" onClick={() => { setSalidaIdAlumno(ag.idalumno); setShowAgendaDrawer(false); ejecutarBusquedaEstudiante(ag.idalumno, 'agenda', buildAgendaCtx(ag)); }} className="px-4 py-3 bg-[var(--istpet-navy)] text-white rounded-2xl text-[9px] font-black uppercase tracking-widest shrink-0">Cargar</button>
                                                </div>
                                            </div>
                                        );
                                    })}
                                </div>
                            ) : null}
                            {agendaBloqueAnteriores.length > 0 ? (
                                <div className="px-5 pt-6">
                                    <p className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest mb-2">Anteriores</p>
                                    {agendaBloqueAnteriores.map((ag, idx) => {
                                        const chip = estadoAgendaChip(ag.estadoOperativo);
                                        return (
                                            <div key={ag.idPractica} className={`group px-1 ${idx !== 0 ? 'border-t border-[var(--apple-border)]/40 pt-4 mt-4' : ''}`}>
                                                <div className="flex items-start gap-3">
                                                    <div className="flex-1 min-w-0">
                                                        <div className="flex flex-wrap items-center gap-2 mb-1">
                                                            <span className={`text-[8px] font-black uppercase px-2 py-0.5 rounded-full ${chip.cls}`}>{chip.label}</span>
                                                        </div>
                                                        <p className="text-[12px] font-black text-[var(--apple-text-main)] uppercase truncate">{ag.AlumnoNombre || ag.idalumno}</p>
                                                        <div className="flex flex-wrap items-center gap-2 mt-2">
                                                            <span className="text-[9px] font-black text-[var(--apple-text-sub)] uppercase px-2 py-0.5 rounded-md bg-[var(--apple-border)]/30">{fmtFechaAgenda(ag.fecha)}</span>
                                                            <span className="text-[10px] font-black text-[var(--istpet-gold)] bg-[var(--istpet-gold)]/10 px-2 py-0.5 rounded-md">{ag.hora_salida != null ? String(ag.hora_salida).substring(0, 5) : '—'}</span>
                                                            <span className="px-2 py-0.5 bg-[var(--apple-border)]/40 rounded-md text-[9px] font-bold text-[var(--apple-text-main)] uppercase truncate max-w-[9rem]">{ag.VehiculoDetalle}</span>
                                                        </div>
                                                    </div>
                                                    <button type="button" onClick={() => { setSalidaIdAlumno(ag.idalumno); setShowAgendaDrawer(false); ejecutarBusquedaEstudiante(ag.idalumno, 'agenda', buildAgendaCtx(ag)); }} className="px-4 py-3 bg-[var(--istpet-navy)] text-white rounded-2xl text-[9px] font-black uppercase tracking-widest shrink-0">Cargar</button>
                                                </div>
                                            </div>
                                        );
                                    })}
                                </div>
                            ) : null}
                        </div>
                    </div>
                </div>
            )}
        </Layout>
    );
};

export default ControlOperativo;
