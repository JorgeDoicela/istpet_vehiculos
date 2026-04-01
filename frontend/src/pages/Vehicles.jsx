import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import VehicleList from '../components/features/VehicleList';
import vehicleService from '../services/vehicleService';
import dashboardService from '../services/dashboardService';

const Vehicles = () => {
  const [vehicles, setVehicles] = useState([]);
  const [maintenanceAlerts, setMaintenanceAlerts] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      const [vData, aData] = await Promise.all([
        vehicleService.getAll(),
        dashboardService.getAlertasMantenimiento()
      ]);
      setVehicles(vData);
      setMaintenanceAlerts(aData);
    } catch (err) {
      console.error('Vehicle Error:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Layout>
      <div className="space-y-12">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-5xl font-black tracking-tighter text-slate-900 uppercase">Flota ISTPET</h1>
            <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-3">CONTROL MECÁNICO Y LOGÍSTICO DE UNIDADES</p>
          </div>
          
          <div className="apple-glass p-8 rounded-[2.5rem] flex items-center gap-6 border-white border-2">
            <div className="text-right">
              <p className="text-[10px] font-black uppercase text-slate-400">Total Flota</p>
              <p className="text-2xl font-black text-slate-900">{vehicles.length}</p>
            </div>
            <div className="w-px h-10 bg-slate-200"></div>
            <div className="text-right">
              <p className="text-[10px] font-black uppercase text-rose-400">En Taller</p>
              <p className="text-2xl font-black text-rose-500">{maintenanceAlerts.length}</p>
            </div>
          </div>
        </div>

        {maintenanceAlerts.length > 0 && (
          <section className="bg-rose-50/80 apple-glass p-8 rounded-[3rem] border-rose-100 border-2 animate-apple-in">
             <div className="flex items-center gap-6">
                <div className="p-4 bg-rose-500 rounded-3xl text-white shadow-xl shadow-rose-500/20">
                   <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="w-6 h-6">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
                   </svg>
                </div>
                <div>
                  <h4 className="text-sm font-black uppercase text-rose-700 tracking-widest">Alerta de Mantenimiento</h4>
                  <p className="text-xs font-bold text-rose-600 mt-1">Hay {maintenanceAlerts.length} unidades que requieren atención mecánica inmediata.</p>
                </div>
             </div>
          </section>
        )}

        <section className="animate-apple-in">
          <VehicleList vehicles={vehicles} />
        </section>
      </div>
    </Layout>
  );
};

export default Vehicles;
