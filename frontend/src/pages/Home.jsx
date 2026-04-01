import React, { useState, useEffect } from 'react';
import Layout from '../components/layout/Layout';
import ActiveClasses from '../components/features/ActiveClasses';
import dashboardService from '../services/dashboardService';

const Home = () => {
  const [activeClasses, setActiveClasses] = useState([]);
  const [syncResult, setSyncResult] = useState(null);
  const [syncing, setSyncing] = useState(false);

  useEffect(() => {
    fetchInitialData();
  }, []);

  const fetchInitialData = async () => {
    try {
      const cData = await dashboardService.getClasesActivas();
      setActiveClasses(cData);
    } catch (err) {
      console.error('Dashboard Error:', err);
    }
  };

  const handleSyncData = async () => {
    setSyncing(true);
    setSyncResult(null);
    try {
      const log = await dashboardService.syncStudents();
      setSyncResult(log);
    } catch (err) {
      alert('Error de Red: ' + err);
    } finally {
      setSyncing(false);
    }
  };

  return (
    <Layout>
      <div className="space-y-12">
        <div className="max-w-4xl">
          <h1 className="text-5xl font-black tracking-tighter text-slate-900 uppercase">Dashboard Principal</h1>
          <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-3">CONTROL MAESTRO Y SINCRONIZACIÓN ISTPET 2026</p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
          {/* LADO IZQUIERDO: CONTROL DE INTEGRIDAD */}
          <div className="lg:col-span-2 space-y-12 animate-apple-in">
            <div className="apple-card p-12 bg-white/60 border-2 border-white">
                <div className="flex items-center gap-6 mb-12">
                    <div className={`p-6 bg-[var(--apple-primary)] rounded-[2.5rem] shadow-2xl text-white ${syncing ? 'animate-spin' : ''}`}>
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" className="w-10 h-10">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0 3.181 3.183a8.25 8.25 0 0 0 13.803-3.7M4.031 9.865a8.25 8.25 0 0 1 13.803-3.7l3.181 3.182m0-4.991v4.99" />
                        </svg>
                    </div>
                    <div>
                        <h3 className="text-3xl font-black tracking-tighter uppercase text-slate-900">Integrity Center</h3>
                        <p className="text-[var(--apple-text-sub)] text-[10px] font-black uppercase tracking-widest mt-1">Sincronización Masiva Protegida</p>
                    </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-8 items-center">
                    <div>
                        <p className="text-sm text-slate-500 font-bold mb-8 leading-relaxed">
                          Inicia la "Aduana Digital" para importar datos externos de forma segura. El sistema validará cada registro antes de persistirlo.
                        </p>
                        <button 
                            onClick={handleSyncData}
                            disabled={syncing}
                            className="btn-apple-primary w-full shadow-2xl"
                        >
                            {syncing ? 'Validando Ecosistema...' : 'Sincronizar Datos Externos'}
                        </button>
                    </div>

                    <div className={`p-10 rounded-[3rem] border-2 transition-all duration-1000 min-h-[220px] flex flex-col justify-center ${
                        !syncResult ? 'bg-white/30 border-dashed border-slate-300/40 opacity-50' : 
                        syncResult.estado === 'OK' ? 'bg-emerald-50/50 border-emerald-100 shadow-xl' : 'bg-amber-50/50 border-amber-100 shadow-xl'
                    }`}>
                        {!syncResult ? (
                            <div className="text-center space-y-4">
                                <p className="text-slate-400 font-black text-[10px] uppercase tracking-widest px-8">Audit System Offline</p>
                            </div>
                        ) : (
                            <div className="space-y-6">
                                <div className="flex items-center justify-between">
                                    <span className="text-[10px] font-black uppercase text-slate-400 tracking-[0.2em]">Auditoría Logística</span>
                                    <span className={`px-4 py-1 rounded-full text-[9px] font-black uppercase text-white ${
                                        syncResult.estado === 'OK' ? 'bg-emerald-500' : 'bg-amber-500'
                                    }`}>{syncResult.estado}</span>
                                </div>
                                <div className="grid grid-cols-2 gap-8">
                                    <div>
                                        <p className="text-[10px] font-black uppercase text-slate-400">Éxito</p>
                                        <p className="text-5xl font-black text-emerald-600 drop-shadow-sm">{syncResult.registrosProcesados}</p>
                                    </div>
                                    <div>
                                        <p className="text-[10px] font-black uppercase text-slate-400">Auditados</p>
                                        <p className="text-5xl font-black text-amber-500 drop-shadow-sm">{syncResult.registrosFallidos}</p>
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
               <div className="apple-card bg-indigo-50/50 border-none">
                  <h4 className="text-xl font-black text-indigo-900 tracking-tighter uppercase">Estado Global</h4>
                  <p className="mt-2 text-indigo-700/60 font-bold text-xs uppercase tracking-widest">Sistemas 100% operativos</p>
               </div>
               <div className="apple-card bg-slate-900 text-white border-none">
                  <h4 className="text-xl font-black tracking-tighter uppercase">Mantenimiento</h4>
                  <p className="mt-2 text-slate-400 font-bold text-xs uppercase tracking-widest">Próxima auditoría en 48h</p>
               </div>
            </div>
          </div>

          {/* LADO DERECHO: MONITOREO DE CLASES */}
          <div className="animate-apple-in">
            <ActiveClasses classes={activeClasses} />
          </div>
        </div>
      </div>
    </Layout>
  );
};

export default Home;
