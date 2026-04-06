import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import VehicleList from '../components/features/VehicleList';
import SkeletonLoader from '../components/features/SkeletonLoader';
import vehicleService from '../services/vehicleService';
import dashboardService from '../services/dashboardService';

const Vehicles = () => {
  const [vehicles, setVehicles] = useState([]);
  const [maintenanceAlerts, setMaintenanceAlerts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [vData, aData] = await Promise.all([
        vehicleService.getAll(),
        dashboardService.getAlertasMantenimiento()
      ]);
      
      setVehicles(Array.isArray(vData) ? vData : []);
      setMaintenanceAlerts(Array.isArray(aData) ? aData : []);
    } catch (err) {
      console.error('[VEHICLE ERROR]', err.message);
      setError("Error de conexión con el sistema ISTPET");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Layout>
      <div className="space-y-12 mb-20 animate-apple-in">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-8">
          <div>
            <h1 className="text-4xl lg:text-6xl font-black tracking-tighter text-[var(--apple-text-main)] uppercase bg-clip-text text-transparent bg-gradient-to-b from-[var(--apple-text-main)] to-[var(--apple-text-sub)]">
                Flota ISTPET
            </h1>
            <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-3 tracking-widest">SISTEMA DE CONTROL LOGISTICO 2026</p>
          </div>
          
          <div className="apple-card p-6 md:p-8 px-10 flex items-center justify-around md:justify-start gap-6 md:gap-10 border-[var(--apple-border)]">
            <div className="text-center md:text-left">
              <p className="text-[9px] font-black uppercase text-[var(--apple-text-sub)] tracking-widest mb-1">Total Unidades</p>
              {loading ? <SkeletonLoader type="text" className="w-10 h-6 mt-1" /> : <p className="text-2xl font-black text-[var(--apple-text-main)]">{(vehicles || []).length}</p>}
            </div>
            <div className="w-px h-10 bg-[var(--apple-border)] hidden md:block"></div>
            <div className="text-center md:text-left">
              <p className="text-[9px] font-black uppercase text-rose-500/70 tracking-widest mb-1">En Taller</p>
              {loading ? <SkeletonLoader type="text" className="w-10 h-6 mt-1" /> : <p className="text-2xl font-black text-rose-500">{(maintenanceAlerts || []).length}</p>}
            </div>
          </div>
        </div>

        {error && (
          <div className="p-8 bg-rose-500/10 border-2 border-rose-500/20 rounded-[2.5rem] text-rose-500 font-bold text-center animate-apple-in text-sm uppercase tracking-widest">
            {error}
          </div>
        )}

        {loading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
            <SkeletonLoader type="card" className="rounded-[2.5rem]" />
            <SkeletonLoader type="card" className="rounded-[2.5rem]" />
            <SkeletonLoader type="card" className="rounded-[2.5rem]" />
            <SkeletonLoader type="card" className="rounded-[2.5rem]" />
          </div>
        ) : (
          <>
            {(maintenanceAlerts || []).length > 0 && (
              <section className="bg-rose-500/[0.03] apple-glass p-10 rounded-[3.5rem] border-rose-500/10 border-2 animate-apple-in">
                 <div className="flex flex-col sm:flex-row items-center gap-8 text-center sm:text-left">
                    <div className="p-5 bg-rose-500 rounded-[2rem] text-white shadow-xl shadow-rose-500/20">
                       <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-8 h-8">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
                       </svg>
                    </div>
                    <div>
                      <h4 className="text-xl font-black uppercase text-rose-500 tracking-tighter">Mantenimiento Requerido</h4>
                      <p className="text-[10px] sm:text-sm font-bold text-rose-500/60 mt-1 uppercase tracking-[0.2em]">Protocolo de taller activo para {(maintenanceAlerts || []).length} unidades en revisión</p>
                    </div>
                 </div>
              </section>
            )}

            <section className="animate-apple-in" style={{ animationDelay: '0.2s' }}>
              <VehicleList vehicles={vehicles || []} />
            </section>
          </>
        )}
      </div>
    </Layout>
  );
};

export default Vehicles;
