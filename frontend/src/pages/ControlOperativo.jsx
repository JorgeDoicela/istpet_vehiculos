import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import logisticaService from '../services/logisticaService';
import dashboardService from '../services/dashboardService';

const ControlOperativo = () => {
  const [activeTab, setActiveTab] = useState('salida'); // salida | llegada
  const [notification, setNotification] = useState(null);

  // --- Estado Salida ---
  const [salidaCedula, setSalidaCedula] = useState('');
  const [salidaLoading, setSalidaLoading] = useState(false);
  const [estudianteData, setEstudianteData] = useState(null); // name, curso, etc
  const [vehiculos, setVehiculos] = useState([]);
  const [vehiculoSeleccionado, setVehiculoSeleccionado] = useState(null);
  
  // --- Estado Llegada ---
  const [clasesActivas, setClasesActivas] = useState([]);
  const [claseSeleccionada, setClaseSeleccionada] = useState(null);
  const [kmLlegada, setKmLlegada] = useState('');
  const [horaRetorno, setHoraRetorno] = useState('');

  const showNotification = (message, type = 'success') => {
      setNotification({ message, type });
      setTimeout(() => setNotification(null), 4000);
  };

  useEffect(() => {
    // Current time clock
    const clockInt = setInterval(() => {
        const now = new Date();
        const timeStr = now.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'});
        setHoraRetorno(timeStr);
    }, 1000);
    return () => clearInterval(clockInt);
  }, []);

  useEffect(() => {
     if(activeTab === 'salida') {
         cargarVehiculosDisponibles();
     } else {
         cargarClasesActivas();
     }
  }, [activeTab]);

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
       } catch(e) {
           showNotification('Error cargando vehículos en pista', 'error');
       }
  };

  // --- Funciones Salida ---
  const handleBuscarEstudiante = async (e) => {
      e.preventDefault();
      if(!salidaCedula) return;
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

  const handleSalidaChangeVehiculo = (e) => {
      const vid = e.target.value;
      const veh = vehiculos.find(x => x.idVehiculo.toString() === vid);
      setVehiculoSeleccionado(veh);
  };

  const procesarSalida = async () => {
      if(!estudianteData || !vehiculoSeleccionado) {
          showNotification('Faltan datos por seleccionar', 'error');
          return;
      }
      try {
          await logisticaService.registrarSalida(
              estudianteData.idMatricula, 
              vehiculoSeleccionado.idVehiculo, 
              vehiculoSeleccionado.idInstructorFijo
          );
          showNotification('¡Vehículo en pista registrado!');
          setEstudianteData(null);
          setSalidaCedula('');
          setVehiculoSeleccionado(null);
          cargarVehiculosDisponibles();
      } catch (err) {
          showNotification(err.message, 'error');
      }
  };

  // --- Funciones Llegada ---
  const handleLlegadaChangeVehiculo = (e) => {
      const classId = e.target.value;
      const c = clasesActivas.find(x => x.id_Registro.toString() === classId);
      setClaseSeleccionada(c);
  };

  const procesarLlegada = async () => {
      if(!claseSeleccionada || !kmLlegada) {
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
                <div className={`w-3 h-12 rounded-full ${notification.type === 'error' ? 'bg-rose-500' : 'bg-[var(--apple-primary)]'}`}></div>
                <p className="text-sm font-bold text-slate-800">{notification.message}</p>
            </div>
        )}

        <div className="max-w-5xl mx-auto mt-6">
            
            {/* Nav Estilo Legacy / Coordinador */}
            <div className="flex bg-slate-300 border border-slate-400 p-1 mb-4 gap-1 text-sm font-semibold">
                <button 
                  onClick={() => setActiveTab('salida')} 
                  className={`flex-1 py-1 px-4 border ${activeTab === 'salida' ? 'bg-slate-100 text-blue-700 border-b-2 border-b-blue-600' : 'bg-slate-200 text-blue-800 hover:bg-slate-100'}`}>
                    Salida Vehiculo
                </button>
                <div className="w-px bg-slate-400 my-1"></div>
                <button 
                  onClick={() => setActiveTab('llegada')} 
                  className={`flex-1 py-1 px-4 border ${activeTab === 'llegada' ? 'bg-slate-100 text-blue-700 border-b-2 border-b-blue-600' : 'bg-slate-200 text-blue-800 hover:bg-slate-100'}`}>
                    Llegada Vehiculos
                </button>
                <div className="w-px bg-slate-400 my-1"></div>
                <button className="flex-1 py-1 px-4 border bg-slate-200 text-blue-800 hover:bg-slate-100 cursor-not-allowed opacity-70">
                    Cerrar Sesion
                </button>
            </div>

            {/* Banner Azul */}
            <div className="bg-blue-500 text-white text-center font-bold py-1 mb-8 uppercase tracking-widest text-sm shadow-md">
                 {activeTab === 'salida' ? 'SALIDA DE VEHICULOS' : 'LLEGADA DE VEHICULOS'}
            </div>


            {/* ------ FORM SALIDA ------ */}
            {activeTab === 'salida' && (
                <div className="apple-card p-10 bg-white/60 animate-apple-in max-w-3xl">
                    <form onSubmit={handleBuscarEstudiante} className="flex gap-4 items-center mb-6">
                        <label className="font-bold text-slate-800 w-32">Cedula:</label>
                        <input 
                           type="text" 
                           value={salidaCedula}
                           onChange={(e) => setSalidaCedula(e.target.value)}
                           className="border border-slate-300 rounded px-2 py-1 w-48 shadow-inner" 
                        />
                        <button type="submit" disabled={salidaLoading} className="bg-slate-200 border border-slate-400 px-4 py-1 text-sm hover:bg-slate-300 active:bg-slate-400 shadow-sm text-black min-w-[100px]">
                            {salidaLoading ? (
                                <span className="flex items-center gap-1">
                                    <svg className="animate-spin h-3 w-3 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                                    Buscando...
                                </span>
                            ) : 'Buscar'}
                        </button>
                    </form>


                    <div className="mb-6 ml-36 flex flex-col gap-2">
                        <input type="text" readOnly className="border border-slate-300 bg-slate-100 rounded px-2 py-1 w-full max-w-sm text-sm" 
                               value={estudianteData?.estudianteNombre || ''} placeholder="-" />
                        
                        <div className="flex gap-2">
                            <input type="text" readOnly className="border border-slate-300 bg-slate-100 rounded px-2 py-1 w-80 text-xs" 
                                value={estudianteData?.cursoDetalle || ''} placeholder="-" />
                            <input type="text" readOnly className="border border-slate-300 bg-slate-100 rounded px-2 py-1 w-32 text-xs" 
                                value={estudianteData?.periodo || ''} placeholder="-" />
                        </div>
                    </div>

                    <div className="flex items-center mb-6 mt-12">
                        <label className="font-bold text-slate-800 w-32">Vehiculo No:</label>
                        <select 
                           onChange={handleSalidaChangeVehiculo} 
                           value={vehiculoSeleccionado?.idVehiculo || ''}
                           className="border-2 border-slate-800 rounded px-2 py-1 w-64 shadow-md bg-white text-sm font-bold">
                            <option value="">-- SELECCIONE --</option>
                            {vehiculos.map(v => (
                                <option key={v.idVehiculo} value={v.idVehiculo}>{v.vehiculoStr}</option>
                            ))}
                        </select>
                    </div>

                    <div className="mb-8 ml-36">
                        <input type="text" readOnly className="border border-blue-200 bg-blue-50 text-blue-900 font-semibold rounded px-2 py-1 w-full max-w-md text-sm shadow-inner" 
                               value={vehiculoSeleccionado ? vehiculoSeleccionado.instructorNombre.toUpperCase() : ''} placeholder="Instructor asignado..." />
                    </div>

                    <div className="flex items-center mb-6 mt-8">
                        <label className="font-bold text-slate-800 w-32">Hora Salida:</label>
                        <div className="flex gap-2">
                           <input type="text" readOnly value={new Date().toLocaleDateString('es-ES', { weekday: 'long' })} className="border border-slate-300 rounded px-2 py-1 w-24 text-center capitalize" />
                           <input type="text" readOnly value={new Date().toLocaleDateString('es-ES')} className="border border-slate-300 rounded px-2 py-1 w-28 text-center" />
                           <input type="text" readOnly value={horaRetorno} className="border border-slate-300 rounded px-2 py-1 w-20 text-center" />
                        </div>
                    </div>

                    <div className="ml-36">
                       <button onClick={procesarSalida} className="bg-slate-200 border border-slate-400 px-8 py-1 shadow text-black font-semibold hover:bg-slate-300 active:bg-slate-400">
                           Salida
                       </button>
                    </div>
                </div>
            )}

            {/* ------ FORM LLEGADA ------ */}
            {activeTab === 'llegada' && (
                <div className="apple-card p-10 bg-white/60 animate-apple-in max-w-3xl">
                     <div className="flex items-center mb-6">
                        <label className="font-bold text-slate-800 w-32">Licencia tipo:</label>
                        <select className="border border-slate-400 rounded px-2 py-1 w-48 bg-white text-sm">
                            <option>LICENCIA TIPO C</option>
                            <option>LICENCIA TIPO D</option>
                            <option>LICENCIA TIPO E</option>
                        </select>
                    </div>

                    <div className="flex items-center mb-6">
                        <label className="font-bold text-slate-800 w-32">Vehiculo No:</label>
                        <select 
                          onChange={handleLlegadaChangeVehiculo}
                          value={claseSeleccionada ? claseSeleccionada.id_Registro : ''}
                          className="border border-slate-400 rounded px-2 py-1 w-64 bg-white text-sm">
                             <option value="">-- SELECCIONE EN PISTA --</option>
                             {clasesActivas.map(c => (
                                 <option key={c.id_Registro} value={c.id_Registro}>#{c.numero_vehiculo} ({c.placa}) Pista</option>
                             ))}
                        </select>
                    </div>

                    <div className="mb-8 ml-36">
                        <input type="text" readOnly className="border border-blue-200 bg-blue-50 text-blue-900 font-semibold rounded px-2 py-1 w-full max-w-md text-sm shadow-inner" 
                               value={claseSeleccionada ? claseSeleccionada.instructor.toUpperCase() : ''} placeholder="Instructor..." />
                    </div>

                    <div className="flex items-center mb-4 mt-8">
                        <label className="font-bold text-slate-800 w-32">Hora Salida:</label>
                        <input type="text" readOnly className="border border-slate-300 bg-slate-50 rounded px-2 py-1 w-24 text-center" 
                               value={claseSeleccionada ? new Date(claseSeleccionada.salida).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) : ''} />
                    </div>

                    <div className="flex items-center mb-6">
                        <label className="font-bold text-slate-800 w-32">Hora Retorno:</label>
                        <input type="text" readOnly className="border border-slate-300 rounded px-2 py-1 w-24 text-center bg-white" 
                               value={horaRetorno} />
                    </div>

                    <div className="flex items-center mb-8 bg-amber-50 rounded-xl p-4 border border-amber-200 shadow-sm w-96 ml-32">
                        <label className="font-black text-amber-900 w-32 text-xs uppercase tracking-widest leading-tight">Kilometraje Llegada:</label>
                        <div className="relative">
                            <input type="number" className="border-2 border-amber-400 bg-white rounded-lg px-3 py-2 w-32 text-center text-lg font-bold shadow-inner" 
                                value={kmLlegada} onChange={(e)=>setKmLlegada(e.target.value)} placeholder="0" />
                            <span className="absolute right-3 top-3 text-xs font-bold text-amber-500">KM</span>
                        </div>
                    </div>

                    <div className="ml-36">
                       <button onClick={procesarLlegada} className="bg-slate-200 border border-slate-400 px-8 py-1 shadow text-black font-semibold hover:bg-slate-300 active:bg-slate-400">
                           Llegada
                       </button>
                    </div>
                </div>
            )}
            
        </div>
    </Layout>
  );
};

export default ControlOperativo;
