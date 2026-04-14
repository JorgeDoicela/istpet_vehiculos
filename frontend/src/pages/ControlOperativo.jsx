import React, { useState, useEffect, useRef, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTheme } from '../components/common/ThemeContext';
import Layout from '../components/layout/Layout';
import { logisticaService } from '../services/logisticaService';
import dashboardService from '../services/dashboardService';
import StatusBadge from '../components/common/StatusBadge';
import ConfirmModal from '../components/common/ConfirmModal';
import VehicleCard from '../components/logistica/VehicleCard';
import { useOperativeAlerts } from '../context/OperativeAlertsContext';
import {
    agendaYmdFromApi,
    ymdLocalHoy,
    fmtFechaAgenda,
    fmtUltimaCargaAgenda,
    estadoAgendaChip,
    agendaPracticaVigenteParaSugerencia,
    fmtTimeSpan,
    salidaToMinutes,
    fmtTiempoEnRuta
} from '../utils/agendaUi';

/**
 * Control Operativo: Absolute SIGAFI Parity Edition 2026.
 * Guaranteed 1:1 naming with central database for idAlumno, idProfesor, idVehiculo.
 */
const ControlOperativo = () => {
    const { theme } = useTheme();
    const { publishClasesActivas } = useOperativeAlerts();
    const [searchParams] = useSearchParams();
    const [activeTab, setActiveTab] = useState(searchParams.get('tab') || 'salida');
    const [notification, setNotification] = useState(null);
    const instructorInputRef = useRef(null);
    const agendaSheetTouchStartY = useRef(0);

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
    const [filtroLlegada, setFiltroLlegada] = useState('');
    const [llegadaSubmitting, setLlegadaSubmitting] = useState(false);
    const [confirmState, setConfirmState] = useState({
        isOpen: false,
        title: '',
        message: '',
        confirmText: 'Continuar',
        onConfirm: () => { },
        type: 'info'
    });

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

    // Auto-scroll al vehículo seleccionado (especialmente útil para carga desde Agenda)
    useEffect(() => {
        if (vehiculoSeleccionado && activeTab === 'salida') {
            const timer = setTimeout(() => {
                const el = document.getElementById(`veh-card-${vehiculoSeleccionado.idVehiculo}`);
                if (el) {
                    el.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'start' });
                }
            }, 350);
            return () => clearTimeout(timer);
        }
    }, [vehiculoSeleccionado, activeTab]);

    // Auto-scroll a la ficha cuando se carga un estudiante (solo si no es visible)
    useEffect(() => {
        if (estudianteData && activeTab === 'salida') {
            const timer = setTimeout(() => {
                const el = document.getElementById('ficha-salida');
                if (el) {
                    el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                }
            }, 600);
            return () => clearTimeout(timer);
        }
    }, [estudianteData, activeTab]);

    const clasesActivasParaLlegada = useMemo(() => {
        const q = filtroLlegada.trim().toLowerCase().replace(/\s+/g, ' ');
        let list = Array.isArray(clasesActivas) ? [...clasesActivas] : [];
        if (q) {
            list = list.filter((c) => {
                const id = (c.idAlumno || '').toLowerCase();
                const est = (c.estudiante || '').toLowerCase();
                const ins = (c.instructor || '').toLowerCase();
                const num = String(c.numeroVehiculo ?? '').toLowerCase();
                const placa = (c.placa || '').toLowerCase();
                const idPr = String(c.idPractica ?? '');
                return id.includes(q) || est.includes(q) || ins.includes(q) || num.includes(q) || placa.includes(q) || idPr.includes(q);
            });
        }
        list.sort((a, b) => {
            const ma = salidaToMinutes(a.salida);
            const mb = salidaToMinutes(b.salida);
            // Antiguos primero: menor hora de salida va arriba.
            if (Number.isNaN(ma) && Number.isNaN(mb)) return (a.idPractica ?? 0) - (b.idPractica ?? 0);
            if (Number.isNaN(ma)) return 1;
            if (Number.isNaN(mb)) return -1;
            const byHora = ma - mb;
            if (byHora !== 0) return byHora;
            return (a.idPractica ?? 0) - (b.idPractica ?? 0);
        });
        return list;
    }, [clasesActivas, filtroLlegada]);

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

    useEffect(() => {
        publishClasesActivas(clasesActivas);
        return () => publishClasesActivas([]);
    }, [clasesActivas, publishClasesActivas]);

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
                source === 'agenda' && (!!idInstr || data.idPracticaCentral != null);

            if (tieneSugerencia) {
                await aplicarSugerenciaManual(data);
                showNotification('Alumno, vehículo e instructor cargados desde agenda');
            } else {
                showNotification(source === 'agenda' ? 'Datos de agenda cargados' : 'Estudiante localizado');
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
                        vehiculoStr: data.practicaVehiculo
                    };
                }

                if (sVeh) setVehiculoSeleccionado(sVeh);
            }
        } catch (err) {
            console.error('Error aplicando sugerencia:', err);
        }
    };

    const handleSeleccionarVehiculo = (veh) => {
        setVehiculoSeleccionado((prev) =>
            prev?.idVehiculo === veh?.idVehiculo ? null : veh
        );
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
                idsAsignacionHorario: estudianteData.idsAsignacionHorario, // 🚀 Vínculo masivo con agenda
                observaciones: "Salida Regular Control Parity"
            });
            showNotification('¡Vehículo en pista registrado!');
            setEstudianteData(null);
            setSalidaIdAlumno('');
            setVehiculoSeleccionado(null);
            setInstructorSeleccionado(null);
            setFiltroLicencia(null);
            cargarVehiculosDisponibles();
            cargarClasesActivas(); // Refrescar pestaña Llegada inmediatamente
            cargarAgendadosHoy(); // Refrescar Agenda (ahora filtrado por asiste=1)

        } catch (err) {
            const apiMsg = err.response?.data?.message || err.message;
            showNotification(apiMsg, 'error');
        }
    };

    const handleProcesarSalida = () => {
        if (!estudianteData || !vehiculoSeleccionado || !instructorSeleccionado) return;

        setConfirmState({
            isOpen: true,
            title: 'Confirmar Salida',
            message: `¿Iniciar práctica con ${estudianteData.nombreCompleto} en el vehículo #${vehiculoSeleccionado.numeroVehiculo}?`,
            confirmText: 'Sí, Registrar Salida',
            onConfirm: procesarSalida,
            type: 'info'
        });
    };

    const procesarLlegada = async () => {
        if (!claseSeleccionada) {
            showNotification('Seleccione un vehículo en pista', 'error');
            return;
        }
        if (llegadaSubmitting) return;
        setLlegadaSubmitting(true);
        try {
            await logisticaService.registrarLlegada({ idPractica: claseSeleccionada.idPractica });
            showNotification('¡Llegada confirmada!');
            setClaseSeleccionada(null);
            cargarClasesActivas();
        } catch (err) {
            const apiMsg = err.response?.data?.message || err.message;
            showNotification(apiMsg, 'error');
        } finally {
            setLlegadaSubmitting(false);
        }
    };

    const handleProcesarLlegada = () => {
        if (!claseSeleccionada) return;

        setConfirmState({
            isOpen: true,
            title: 'Confirmar Llegada',
            message: `¿Registrar el retorno de ${claseSeleccionada.estudiante}? El vehículo quedará disponible inmediatamente.`,
            confirmText: 'Sí, Registrar Llegada',
            onConfirm: procesarLlegada,
            type: 'info'
        });
    };

    const handleEliminarSalida = async () => {
        if (!claseSeleccionada) return;

        setLlegadaSubmitting(true);
        try {
            await logisticaService.eliminarSalida(claseSeleccionada.idPractica);
            showNotification('Registro eliminado y agenda liberada');
            setClaseSeleccionada(null);
            cargarClasesActivas();
            cargarAgendadosHoy();
        } catch (err) {
            const apiMsg = err.response?.data?.message || err.message;
            showNotification(apiMsg, 'error');
        } finally {
            setTimeout(() => setLlegadaSubmitting(false), 500);
        }
    };

    const handleEliminarSalidaConfirm = () => {
        if (!claseSeleccionada) return;

        setConfirmState({
            isOpen: true,
            title: '¿Eliminar registro?',
            message: `Esta acción revertirá la salida de ${claseSeleccionada.estudiante} y liberará su cita en la agenda. Use esto solo para corregir errores.`,
            confirmText: 'Eliminar permanentemente',
            onConfirm: handleEliminarSalida,
            type: 'danger'
        });
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
                                Bienvenido
                            </h2>
                        </div>

                        {activeTab === 'salida' ? (
                            <div id="ficha-salida" className="apple-card !pt-9 lg:!pt-12 pb-20 lg:pb-10 scroll-mt-20">
                                <div className="mb-6 px-2 flex items-start justify-between">
                                    <h3 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tight">Registro de Salida</h3>
                                    <div className="flex flex-col items-end leading-tight">
                                        <span className="text-[8px] lg:text-[9px] text-[var(--apple-text-sub)] opacity-60 uppercase font-black tracking-widest mb-0.5">{fechaHoy}</span>
                                        <span className="text-sm lg:text-lg font-black text-[var(--apple-text-main)] tracking-tighter tabular-nums">{horaRetorno || '--:--:--'}</span>
                                    </div>
                                </div>

                                <div className="space-y-4">
                                    <div className="relative group">
                                        <label className="absolute left-0 -top-4 px-2 bg-[var(--apple-card)] backdrop-blur-md text-[8px] font-black text-[var(--apple-text-main)] uppercase tracking-[0.2em] transition-colors duration-300 group-focus-within:text-[var(--istpet-gold)] z-10">
                                            Identificación del estudiante
                                        </label>
                                        <div className="relative flex items-center gap-3">
                                            <div className="relative flex-1 group/input">
                                                <input
                                                    type="text"
                                                    inputMode="numeric"
                                                    pattern="[0-9]*"
                                                    placeholder="CÉDULA"
                                                    maxLength={10}
                                                    value={salidaIdAlumno}
                                                    onChange={(e) => { const val = e.target.value.replace(/\D/g, ''); setSalidaIdAlumno(val); if (val.length < 10) { setEstudianteData(null); setVehiculoSeleccionado(null); setInstructorSeleccionado(null); } }}
                                                    className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[1.5rem] px-4 pr-16 py-2.5 text-sm lg:text-base placeholder:text-[11px] lg:placeholder:text-xs placeholder:font-bold placeholder:text-[var(--apple-text-sub)]/30 font-black text-[var(--apple-text-main)] focus:border-[var(--istpet-gold)] focus:bg-[var(--apple-card)] outline-none transition-all shadow-inner tracking-widest"
                                                />
                                                {salidaIdAlumno && !salidaLoading && (
                                                    <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center z-10 transition-all">
                                                        <button
                                                            onClick={() => {
                                                                setSalidaIdAlumno('');
                                                                setEstudianteData(null);
                                                                setVehiculoSeleccionado(null);
                                                                setInstructorSeleccionado(null);
                                                            }}
                                                            className="p-1.5 text-[var(--apple-text-sub)] hover:text-rose-500 transition-all"
                                                            title="Limpiar"
                                                        >
                                                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                                        </button>
                                                    </div>
                                                )}
                                            </div>

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
                                                                            <div className="flex items-center gap-2.5 mt-1 bg-[var(--apple-primary)]/5 px-2.5 py-1.5 rounded-xl border border-[var(--apple-primary)]/10">
                                                                                <span className="text-[10px] font-black text-[var(--apple-primary)] flex items-center gap-1.5">
                                                                                    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                                                                                    {s.horaAgenda || 'Consultando Hora...'}
                                                                                </span>
                                                                                {s.vehiculoAgenda ? (
                                                                                    <span className="text-[9px] font-black text-white bg-slate-800 px-2 py-0.5 rounded border border-slate-700 uppercase tracking-tighter">
                                                                                        {s.vehiculoAgenda}
                                                                                    </span>
                                                                                ) : s.esAgendado && (
                                                                                    <span className="text-[9px] font-bold text-[var(--apple-text-sub)] border-l border-[var(--apple-border)] pl-2.5 uppercase tracking-tighter">
                                                                                        S/V
                                                                                    </span>
                                                                                )}

                                                                                <span className={`text-[8px] font-black px-2 py-0.5 rounded-full shadow-sm animate-pulse uppercase ${s.isBusy ? 'bg-rose-500 text-white' : 'bg-emerald-500 text-white'}`}>
                                                                                    {s.isBusy ? 'EN PISTA' : 'AGENDADO'}
                                                                                </span>
                                                                            </div>


                                                                        </div>
                                                                    ) : (
                                                                        <div className="flex items-center gap-2">
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
                                            <div className="apple-glass rounded-[2.5rem] px-6 py-5 sm:px-7 sm:py-6 shadow-sm hover:shadow-md transition-all group overflow-hidden relative">
                                                <div className="flex items-start justify-between gap-6 relative z-10">

                                                    {/* Bloque principal */}
                                                    <div className="flex-1 min-w-0 space-y-2.5">

                                                        {/* Nombre + badges */}
                                                        <div className="flex flex-wrap items-center gap-2">
                                                            <h4 className="text-base lg:text-lg font-black text-[var(--apple-text-main)] uppercase tracking-tight leading-none">
                                                                {estudianteData.nombreCompleto}
                                                            </h4>
                                                            <span className="px-2 py-0.5 bg-[var(--apple-bg)] border border-[var(--apple-border)] rounded-md text-[8px] font-black text-[var(--istpet-gold)] uppercase tracking-widest">
                                                                {estudianteData.idPeriodo}
                                                            </span>
                                                            {estudianteData.isBusy && (
                                                                <span className="px-2 py-0.5 bg-rose-500 text-white rounded-md text-[8px] font-black uppercase tracking-widest animate-pulse shadow-sm shadow-rose-500/20">
                                                                    YA EN PISTA
                                                                </span>
                                                            )}
                                                        </div>

                                                        {/* Carrera */}
                                                        {estudianteData.carrera?.trim() && (
                                                            <p className="text-[11px] font-black text-[var(--apple-text-main)] uppercase tracking-wide opacity-70 leading-none">
                                                                {estudianteData.carrera}
                                                            </p>
                                                        )}

                                                        {/* Nivel + Paralelo como chips — alineados al borde izquierdo */}
                                                        <div className="flex items-center gap-2">
                                                            <span className="px-2 py-0.5 bg-[var(--apple-border)]/20 rounded-md text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-wider">
                                                                Nivel: {estudianteData.nivel ?? ''}
                                                            </span>
                                                            <span className="px-2 py-0.5 bg-[var(--apple-border)]/20 rounded-md text-[9px] font-black text-[var(--apple-text-sub)] uppercase tracking-wider">
                                                                Paralelo: {estudianteData.paralelo ?? ''}
                                                            </span>
                                                        </div>

                                                        {/* Planificación */}
                                                        {(estudianteData.horarioProximo || estudianteData.vehiculoPlanificado || estudianteData.instructorPlanificado) && (
                                                            <div className="flex flex-col gap-0.5 animate-apple-in">
                                                                <p className="text-[8px] font-black text-[var(--apple-primary)] uppercase tracking-[0.2em] leading-none">Planificación</p>
                                                                {estudianteData.horarioProximo && (
                                                                    <p className="text-[10px] font-black text-[var(--apple-primary)] uppercase leading-snug">
                                                                        {estudianteData.horarioFecha && `${estudianteData.horarioFecha} · `}{estudianteData.horarioProximo}
                                                                    </p>
                                                                )}
                                                                {(estudianteData.vehiculoPlanificado || estudianteData.instructorPlanificado) && (
                                                                    <p className="text-[10px] font-bold text-[var(--apple-primary)]/65 uppercase leading-snug">
                                                                        {estudianteData.vehiculoPlanificado}{estudianteData.instructorPlanificado && ` · ${estudianteData.instructorPlanificado}`}
                                                                    </p>
                                                                )}
                                                            </div>
                                                        )}
                                                    </div>

                                                    {/* Jornada */}
                                                    <div className="shrink-0 text-right pt-0.5">
                                                        <span className="block text-[7px] font-black text-[var(--apple-text-sub)] uppercase tracking-widest opacity-50 mb-1">Jornada</span>
                                                        <span className="block text-[10px] font-black text-[var(--apple-text-main)] uppercase leading-none">{estudianteData.jornada}</span>
                                                    </div>
                                                </div>
                                                <div className="absolute top-0 right-0 h-full w-24 bg-gradient-to-l from-[var(--apple-primary)]/[0.03] to-transparent skew-x-12 translate-x-12 group-hover:translate-x-8 transition-transform duration-700"></div>
                                            </div>
                                        </div>
                                    ) : !salidaLoading && salidaIdAlumno.length >= 1 && (
                                        <div className="p-8 border-2 border-dashed border-[var(--apple-border)] rounded-[2rem] text-center">
                                            <p className="text-[var(--apple-text-sub)] font-bold text-sm uppercase">Ingrese la cédula para identificar al estudiante</p>
                                        </div>
                                    )}

                                    <div className="pt-6">
                                        <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between mb-4 gap-y-1 gap-x-3 lg:gap-x-6 px-1 group">
                                            <div className="flex-1 text-left min-w-0">
                                                <h3 className="text-[8px] font-black uppercase tracking-[0.2em] mb-0 px-2 transition-colors duration-300 group-focus-within:text-[var(--istpet-gold)] text-[var(--apple-text-main)]">Asignar Vehículo</h3>
                                            </div>

                                            <div className="flex items-center gap-2 flex-[2]">
                                                <div className="relative flex-1 group/search">
                                                    <input
                                                        type="text"
                                                        placeholder="BUSCAR VEHÍCULO..."
                                                        value={filtroVehiculo}
                                                        onChange={(e) => setFiltroVehiculo(e.target.value.toUpperCase())}
                                                        className="w-full bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[1.5rem] pl-6 pr-24 py-2.5 text-sm lg:text-base placeholder:text-[11px] lg:placeholder:text-xs font-black tracking-widest text-[var(--apple-text-main)] placeholder:text-[var(--apple-text-sub)]/30 focus:border-[var(--istpet-gold)] shadow-inner transition-all outline-none"
                                                    />
                                                    <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1 z-10 transition-all">
                                                        {filtroVehiculo && (
                                                            <button
                                                                onClick={() => setFiltroVehiculo('')}
                                                                className="p-1.5 text-[var(--apple-text-sub)] hover:text-rose-500 transition-all"
                                                                title="Limpiar"
                                                            >
                                                                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                                            </button>
                                                        )}
                                                        <div className={`p-1.5 transition-all ${filtroVehiculo ? 'text-[var(--istpet-gold)]' : 'text-[var(--apple-text-sub)]'}`}>
                                                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                                                        </div>
                                                    </div>
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

                                        <div className="max-h-[215px] overflow-y-auto pr-2 custom-scrollbar -mr-2 mt-2">
                                            <div className="grid grid-cols-3 gap-2">
                                                {vehiculos.length > 0 ? (
                                                    (() => {
                                                        const licIdMap = { 'C': [1, 3], 'D': [4], 'E': [5] };
                                                        const filtered = vehiculos.filter(v => {
                                                            const term = filtroVehiculo.toLowerCase().trim();
                                                            const matchLicencia = filtroLicencia
                                                                ? licIdMap[filtroLicencia].includes(v.idTipoLicencia)
                                                                : true;
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

                                    <div id="seccion-instructor" className="pt-6 px-1 relative">
                                        <div className="mb-2 transition-all">
                                            <p className={`text-[8px] font-black uppercase tracking-[0.2em] px-2 transition-colors duration-300 ${showInstructorMenu ? 'text-[var(--istpet-gold)]' : 'text-[var(--apple-text-main)]'}`}>Instructor</p>
                                        </div>

                                        <div className="relative group">
                                            <div className="relative flex items-center">
                                                <input
                                                    ref={instructorInputRef}
                                                    type="text"
                                                    readOnly={!isSearchingInstructor}
                                                    placeholder={isSearchingInstructor ? "BUSCAR..." : "VER LISTA..."}
                                                    value={isSearchingInstructor && showInstructorMenu ? filtroInstructor : (instructorSeleccionado?.fullName || '')}
                                                    onChange={(e) => {
                                                        const val = e.target.value.toUpperCase();
                                                        setFiltroInstructor(val);
                                                        if (!showInstructorMenu) setShowInstructorMenu(true);
                                                    }}
                                                    onClick={() => setShowInstructorMenu(true)}
                                                    className={`w-full bg-[var(--apple-bg)] border-2 rounded-[1.5rem] pl-6 pr-28 py-2.5 text-sm lg:text-base font-black text-[var(--apple-text-main)] transition-all shadow-inner outline-none uppercase tracking-widest placeholder:text-[11px] lg:placeholder:text-xs placeholder:font-bold placeholder:text-[var(--apple-text-sub)]/30 z-[150] relative ${showInstructorMenu ? 'border-[var(--istpet-gold)]' : 'border-[var(--apple-border)]'}`}
                                                />
                                                <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1 z-[160] transition-all">
                                                    <button
                                                        onClick={() => {
                                                            if (isSearchingInstructor) {
                                                                setIsSearchingInstructor(false);
                                                                setFiltroInstructor('');
                                                                setShowInstructorMenu(true);
                                                            } else {
                                                                setShowInstructorMenu(!showInstructorMenu);
                                                            }
                                                        }}
                                                        className={`p-1.5 transition-all ${(!isSearchingInstructor && showInstructorMenu) ? 'text-[var(--istpet-gold)]' : 'text-[var(--apple-text-sub)] hover:text-[var(--istpet-gold)]'}`}
                                                        title="Ver lista"
                                                    >
                                                        {showInstructorMenu && !isSearchingInstructor ? (
                                                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M5 15l7-7 7 7" /></svg>
                                                        ) : (
                                                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" /></svg>
                                                        )}
                                                    </button>
                                                    <button
                                                        onClick={() => {
                                                            setIsSearchingInstructor(true);
                                                            setShowInstructorMenu(true);
                                                            setTimeout(() => instructorInputRef.current?.focus(), 50);
                                                        }}
                                                        className={`p-1.5 transition-all ${isSearchingInstructor ? 'text-[var(--istpet-gold)]' : 'text-[var(--apple-text-sub)] hover:text-[var(--istpet-gold)]'}`}
                                                        title="Buscar"
                                                    >
                                                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                                                    </button>
                                                </div>
                                            </div>

                                            {showInstructorMenu && (isSearchingInstructor ? filtroInstructor.length > 0 : true) && (
                                                <>
                                                    <div className="fixed inset-0 z-[140]" onClick={() => setShowInstructorMenu(false)} />
                                                    <div className="absolute left-0 right-0 top-full mt-3 bg-[var(--apple-bg)] border-2 border-[var(--apple-border)] rounded-[2.2rem] shadow-2xl z-[150] overflow-hidden animate-apple-in p-3 max-h-[300px] overflow-y-auto">
                                                        {(() => {
                                                            const filtered = instructores.filter(i =>
                                                                !isSearchingInstructor || i.fullName.toUpperCase().includes(filtroInstructor)
                                                            );

                                                            if (filtered.length === 0 && isSearchingInstructor) {
                                                                return (
                                                                    <div className="p-8 text-center opacity-40">
                                                                        <p className="text-[10px] font-black uppercase tracking-widest text-[var(--apple-text-sub)]">Sin coincidencias</p>
                                                                    </div>
                                                                );
                                                            }

                                                            return filtered.map((i) => (
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
                                                            ));
                                                        })()}
                                                    </div>
                                                </>
                                            )}
                                        </div>
                                    </div>

                                    <div className="pt-6 border-t border-[var(--apple-border)] mt-4">
                                        <button
                                            id="btn-registrar-salida"
                                            onClick={handleProcesarSalida}
                                            disabled={!estudianteData || estudianteData.isBusy || !vehiculoSeleccionado || !instructorSeleccionado}
                                            className={`w-full py-4 rounded-full text-sm font-black transition-all ${(!estudianteData || estudianteData.isBusy || !vehiculoSeleccionado || !instructorSeleccionado) ? 'bg-[var(--apple-border)] text-[var(--apple-text-sub)] cursor-not-allowed opacity-30' : 'btn-apple-primary shadow-xl shadow-[var(--istpet-gold)]/20 hover:scale-[1.01]'}`}
                                        >
                                            {!estudianteData ? 'INGRESAR CÉDULA DEL ALUMNO' :
                                                estudianteData.isBusy ? 'ESTUDIANTE EN PISTA' :
                                                    !vehiculoSeleccionado ? 'SELECCIONAR VEHÍCULO' :
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
                                    <div className="flex flex-col items-end leading-tight">
                                        <span className="text-sm lg:text-lg font-black text-[var(--apple-text-main)] tracking-tighter tabular-nums">{horaRetorno || '--:--:--'}</span>
                                        <div className="mt-1 flex gap-1.5 items-center text-[9px] font-black text-emerald-500 uppercase tracking-widest">
                                            <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></div>
                                            EN PISTA
                                        </div>
                                    </div>
                                </div>

                                <div className="mb-4">
                                    <div className="relative">
                                        <svg className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-[var(--apple-text-sub)] opacity-40 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5"><circle cx="11" cy="11" r="8" /><path strokeLinecap="round" d="m21 21-4.35-4.35" /></svg>
                                        <input
                                            type="search"
                                            value={filtroLlegada}
                                            onChange={(e) => setFiltroLlegada(e.target.value)}
                                            placeholder="Buscar estudiante, cédula, vehículo o instructor…"
                                            className="w-full bg-[var(--apple-bg)] border border-[var(--apple-border)] rounded-full pl-10 pr-4 py-2.5 text-xs font-semibold text-[var(--apple-text-main)] placeholder:text-[var(--apple-text-sub)]/40 outline-none transition-colors shadow-[inset_0_1px_1px_rgba(0,0,0,0.05)] focus:border-[var(--istpet-gold)] focus:ring-1 focus:ring-[var(--istpet-gold)]/20"
                                        />
                                    </div>
                                    <p className="mt-2 text-[9px] font-bold text-[var(--apple-text-sub)] uppercase tracking-[0.1em] px-1 tabular-nums">
                                        {clasesActivas.length} vehículo{clasesActivas.length !== 1 ? 's' : ''} en pista
                                    </p>
                                </div>

                                <div>
                                    <div className="max-h-[480px] overflow-y-auto pr-1 custom-scrollbar">
                                        <div className="space-y-3">
                                            {clasesActivas.length > 0 && clasesActivasParaLlegada.length === 0 ? (
                                                <div className="py-12 text-center opacity-50 apple-glass rounded-3xl border border-dashed border-[var(--apple-border)]">
                                                    <p className="text-[var(--apple-text-sub)] font-bold tracking-widest text-xs uppercase text-center">Sin coincidencias con el filtro</p>
                                                </div>
                                            ) : null}
                                            {clasesActivasParaLlegada.length > 0 ? (
                                                clasesActivasParaLlegada.map(c => {
                                                    const sel = claseSeleccionada?.idPractica === c.idPractica;
                                                    const enRuta = fmtTiempoEnRuta(c.salida);
                                                    const placaTxt = (c.placa || '').trim();
                                                    return (
                                                        <div
                                                            key={c.idPractica}
                                                            onClick={() => {
                                                                if (llegadaSubmitting) return;
                                                                setClaseSeleccionada(prev =>
                                                                    prev?.idPractica === c.idPractica ? null : c
                                                                );
                                                            }}
                                                            className={`group relative rounded-[1.65rem] border px-5 py-4 transition-all duration-200 shadow-[inset_0_1px_0_rgba(255,255,255,0.5)] ${llegadaSubmitting ? 'opacity-60 pointer-events-none' : 'cursor-pointer active:scale-[0.99]'} ${sel ? 'bg-[var(--istpet-gold)]/[0.04] border-[var(--istpet-gold)] shadow-sm' : 'bg-[var(--apple-card)] border-[var(--apple-border)] hover:border-[var(--apple-text-sub)]/50'}`}
                                                        >
                                                            {/* Fila 1: Estudiante + Hora */}
                                                            <div className="flex items-start justify-between gap-4 mb-3">
                                                                <div className="min-w-0 flex-1">
                                                                    <p className="text-sm font-black text-[var(--apple-text-main)] uppercase tracking-tight leading-snug line-clamp-1">{c.estudiante}</p>
                                                                    <p className="text-[10px] font-bold text-[var(--apple-text-sub)] tracking-[0.08em] mt-1">
                                                                        {c.idAlumno}
                                                                        {enRuta ? <span className="ml-2 text-emerald-600 font-black">{enRuta}</span> : null}
                                                                    </p>
                                                                </div>
                                                                <div className="text-right shrink-0 flex flex-col items-end gap-0.5">
                                                                    <p className="text-lg font-black text-[var(--istpet-gold)] tabular-nums leading-none">{fmtTimeSpan(c.salida)}</p>
                                                                    <p className="text-[7px] font-black text-[var(--apple-text-sub)] uppercase tracking-[0.15em]">Salida</p>
                                                                </div>
                                                            </div>

                                                            {/* Divisor */}
                                                            <div className="h-px bg-[var(--apple-border)]/40 mb-3"></div>

                                                            {/* Fila 2: Vehículo */}
                                                            <div className="flex items-center justify-between gap-3 mb-2">
                                                                <div className="flex items-center gap-2 min-w-0">
                                                                    <svg className="h-4 w-4 text-[var(--istpet-gold)] shrink-0" viewBox="0 0 24 24" fill="currentColor"><path d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z" /></svg>
                                                                    <span className="text-xs font-black text-[var(--apple-text-main)] uppercase tracking-tight">
                                                                        #{c.numeroVehiculo}{placaTxt ? ` · ${placaTxt}` : ''}
                                                                    </span>
                                                                </div>
                                                                {sel ? (
                                                                    <div className="h-6 w-6 rounded-full bg-[var(--istpet-gold)] text-white flex items-center justify-center shadow-sm shrink-0" aria-hidden>
                                                                        <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" /></svg>
                                                                    </div>
                                                                ) : null}
                                                            </div>

                                                            {/* Fila 3: Instructor */}
                                                            <div className="flex items-center gap-1.5">
                                                                <span className="text-[8px] font-black text-[var(--apple-text-sub)] uppercase tracking-[0.12em] opacity-50 shrink-0">Instructor</span>
                                                                <span className="text-[10px] font-black text-[var(--apple-text-main)] uppercase tracking-tight truncate">{c.instructor}</span>
                                                            </div>
                                                        </div>
                                                    );
                                                })
                                            ) : clasesActivas.length === 0 ? (
                                                <div className="py-12 text-center opacity-40 apple-glass rounded-3xl">
                                                    <p className="text-[var(--apple-text-sub)] font-bold tracking-widest text-xs uppercase text-center">Sin registros activos</p>
                                                </div>
                                            ) : null}
                                        </div>
                                    </div>

                                    {claseSeleccionada && (
                                        <div className="mt-4 animate-apple-in">
                                            <div className="bg-[var(--apple-bg)] border border-[var(--apple-border)] rounded-[1.65rem] px-4 py-3 space-y-2.5 shadow-[inset_0_1px_1px_rgba(0,0,0,0.04)]">
                                                <div className="text-center space-y-0.5">
                                                    <p className="text-[8px] font-black text-[var(--apple-text-sub)] uppercase tracking-[0.15em]">Registrar llegada de</p>
                                                    <p className="text-xs font-black text-[var(--apple-text-main)] uppercase leading-tight tracking-tight">{claseSeleccionada.estudiante || '—'}</p>
                                                    <p className="text-[9px] text-[var(--apple-text-sub)] font-bold tabular-nums leading-snug">
                                                        {claseSeleccionada.idAlumno} · #{claseSeleccionada.numeroVehiculo}
                                                        {(claseSeleccionada.placa || '').trim() ? ` · ${(claseSeleccionada.placa || '').trim()}` : ''}
                                                    </p>
                                                </div>
                                                <div className="grid grid-cols-5 gap-2 items-stretch">
                                                    <button
                                                        type="button"
                                                        onClick={handleEliminarSalidaConfirm}
                                                        disabled={llegadaSubmitting}
                                                        title="Eliminar por error"
                                                        className={`col-span-1 flex items-center justify-center p-2 rounded-full border-2 transition-all ${llegadaSubmitting ? 'border-slate-100 text-slate-200 cursor-not-allowed' : 'border-rose-100 text-rose-400 hover:bg-rose-50 hover:border-rose-200 active:scale-95'}`}
                                                    >
                                                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                                                            <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                                        </svg>
                                                    </button>

                                                    <button
                                                        type="button"
                                                        onClick={handleProcesarLlegada}
                                                        disabled={llegadaSubmitting}
                                                        className={`col-span-4 py-2.5 rounded-full text-[11px] font-black shadow-md transition-all duration-200 flex items-center justify-center gap-2 min-h-0 ${llegadaSubmitting ? 'bg-[var(--apple-border)] text-[var(--apple-text-sub)] cursor-wait opacity-80' : 'bg-[var(--istpet-gold)] text-white hover:brightness-105 shadow-amber-500/15'}`}
                                                    >
                                                        {llegadaSubmitting ? (
                                                            <>
                                                                <svg className="animate-spin h-3.5 w-3.5 shrink-0" viewBox="0 0 24 24" aria-hidden><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" /></svg>
                                                                <span>Registrando…</span>
                                                            </>
                                                        ) : (
                                                            <span className="truncate px-1">Confirmar llegada</span>
                                                        )}
                                                    </button>
                                                </div>
                                            </div>
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
                                <div className="relative">
                                    <input
                                        type="text"
                                        inputMode="search"
                                        autoComplete="off"
                                        value={filtroAgenda}
                                        onChange={(e) => setFiltroAgenda(e.target.value)}
                                        placeholder="Buscar cédula, nombre, instructor o vehículo…"
                                        className="w-full rounded-full border border-[var(--apple-border)] bg-[var(--apple-card)] backdrop-blur-md px-5 py-2.5 pr-12 text-[11px] font-black tracking-wide text-[var(--apple-text-main)] shadow-[0_2px_10px_rgba(26,37,68,0.08)] outline-none transition-all placeholder:font-bold placeholder:text-[var(--apple-text-sub)]/35 focus:border-[var(--istpet-gold)] focus:shadow-[0_2px_14px_rgba(212,166,74,0.14)]"
                                    />
                                    {filtroAgenda ? (
                                        <button
                                            type="button"
                                            onClick={() => setFiltroAgenda('')}
                                            className="absolute right-2.5 top-1/2 -translate-y-1/2 p-1.5 text-[var(--apple-text-sub)] hover:text-rose-500 transition-colors"
                                            title="Limpiar"
                                            aria-label="Limpiar búsqueda de agenda"
                                        >
                                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                        </button>
                                    ) : null}
                                </div>
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
                                            // Usar una llave compuesta para evitar el error de 'key=0' en agendados
                                            const itemKey = ag.idPractica > 0 ? `p-${ag.idPractica}` : `h-${ag.idAsignacionHorario}`;
                                            return (
                                                <div key={itemKey} className="bg-[var(--apple-card)] p-4 rounded-2xl border border-[var(--apple-border)] group/item hover:border-[var(--apple-primary)]/50 transition-all shadow-sm">

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
                                            const itemKey = ag.idPractica > 0 ? `p-${ag.idPractica}` : `h-${ag.idAsignacionHorario}`;
                                            return (
                                                <div key={itemKey} className="bg-[var(--apple-card)] p-4 rounded-2xl border border-[var(--apple-border)] group/item hover:border-[var(--apple-primary)]/50 transition-all shadow-sm opacity-95">
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
                    className="lg:hidden fixed bottom-20 right-4 z-50 h-14 w-14 flex items-center justify-center rounded-[1.4rem] bg-white border-2 border-slate-100 text-[var(--apple-text-main)] shadow-xl active:scale-95 transition-all"
                >
                    <svg className="h-6 w-6 text-[var(--istpet-gold)]" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                    </svg>
                    {agendadosHoy.length > 0 && (
                        <span className="absolute top-1 right-1.5 text-[10px] font-black text-[var(--istpet-gold)]">
                            {agendadosHoy.length}
                        </span>
                    )}
                </button>
            )}

            {showAgendaDrawer && (
                <div className="lg:hidden fixed inset-0 z-[200] flex flex-col justify-end animate-apple-in">
                    <div className="absolute inset-0 bg-black/30 backdrop-blur-md" onClick={() => setShowAgendaDrawer(false)} />
                    <div className="relative bg-[var(--apple-bg)] rounded-t-[2.5rem] max-h-[85vh] flex flex-col shadow-2xl overflow-hidden">
                        <button
                            type="button"
                            className="flex w-full items-center justify-center py-3 touch-manipulation shrink-0 cursor-pointer active:opacity-70"
                            aria-label="Cerrar agenda"
                            onClick={() => setShowAgendaDrawer(false)}
                            onTouchStart={(e) => {
                                agendaSheetTouchStartY.current = e.touches[0].clientY;
                            }}
                            onTouchEnd={(e) => {
                                const dy = e.changedTouches[0].clientY - agendaSheetTouchStartY.current;
                                if (dy > 48) setShowAgendaDrawer(false);
                            }}
                        >
                            <span className="w-10 h-1 rounded-full bg-[var(--apple-border)]" />
                        </button>
                        <div className="flex items-center justify-between px-5 pt-0 pb-1 gap-3">
                            <div className="min-w-0 flex-1">
                                <h3 className="text-sm font-black text-[var(--apple-text-main)] uppercase tracking-[0.15em]">Agenda</h3>
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
                        </div>
                        <div className="px-5 pt-0 pb-3 shrink-0">
                            <div className="relative">
                                <input
                                    type="text"
                                    inputMode="search"
                                    autoComplete="off"
                                    value={filtroAgenda}
                                    onChange={(e) => setFiltroAgenda(e.target.value)}
                                    placeholder="Buscar cédula, nombre…"
                                    className="w-full rounded-full border border-[var(--apple-border)] bg-[var(--apple-card)] backdrop-blur-md px-5 py-3 pr-12 text-[13px] font-black tracking-wide text-[var(--apple-text-main)] shadow-[0_2px_10px_rgba(26,37,68,0.08)] outline-none transition-all placeholder:font-bold placeholder:text-[var(--apple-text-sub)]/35 focus:border-[var(--istpet-gold)] focus:shadow-[0_2px_14px_rgba(212,166,74,0.14)]"
                                />
                                {filtroAgenda ? (
                                    <button
                                        type="button"
                                        onClick={() => setFiltroAgenda('')}
                                        className="absolute right-2.5 top-1/2 -translate-y-1/2 p-1.5 text-[var(--apple-text-sub)] hover:text-rose-500 transition-colors"
                                        title="Limpiar"
                                        aria-label="Limpiar búsqueda de agenda"
                                    >
                                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="3"><path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                                    </button>
                                ) : null}
                            </div>
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
                                    <div className="flex items-baseline gap-2 mb-2">
                                        <p className="text-[9px] font-black text-[var(--apple-primary)] uppercase tracking-widest">Hoy</p>
                                        <span className="text-[10px] font-black tabular-nums text-[var(--istpet-gold)]">{agendaBloqueHoy.length}</span>
                                    </div>
                                    <div className="border-b border-[var(--apple-border)]/50 mb-3" aria-hidden />
                                    {agendaBloqueHoy.map((ag, idx) => {
                                        const chip = estadoAgendaChip(ag.estadoOperativo);
                                        const itemKey = ag.idPractica > 0 ? `p-${ag.idPractica}` : `h-${ag.idAsignacionHorario}`;
                                        return (
                                            <div key={itemKey} className={`group px-1 ${idx !== 0 ? 'border-t border-[var(--apple-border)]/40 pt-4 mt-4' : ''}`}>
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
                                                        {ag.ProfesorNombre && (
                                                            <div className="flex items-center gap-1 mt-1.5">
                                                                <svg className="w-3 h-3 text-[var(--apple-text-sub)] shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" /></svg>
                                                                <span className="text-[9px] font-bold text-[var(--apple-text-sub)] uppercase truncate">{ag.ProfesorNombre}</span>
                                                            </div>
                                                        )}
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
                                        const itemKey = ag.idPractica > 0 ? `p-${ag.idPractica}` : `h-${ag.idAsignacionHorario}`;
                                        return (
                                            <div key={itemKey} className={`group px-1 ${idx !== 0 ? 'border-t border-[var(--apple-border)]/40 pt-4 mt-4' : ''}`}>
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
                                                        {ag.ProfesorNombre && (
                                                            <div className="flex items-center gap-1 mt-1.5">
                                                                <svg className="w-3 h-3 text-[var(--apple-text-sub)] shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}><path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" /></svg>
                                                                <span className="text-[9px] font-bold text-[var(--apple-text-sub)] uppercase truncate">{ag.ProfesorNombre}</span>
                                                            </div>
                                                        )}
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

            <ConfirmModal
                isOpen={confirmState.isOpen}
                onClose={() => setConfirmState(prev => ({ ...prev, isOpen: false }))}
                onConfirm={confirmState.onConfirm}
                title={confirmState.title}
                message={confirmState.message}
                confirmText={confirmState.confirmText}
                type={confirmState.type}
            />
        </Layout>
    );
};

export default ControlOperativo;
