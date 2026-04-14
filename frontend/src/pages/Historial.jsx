import React, { useState } from 'react';
import Layout from '../components/layout/Layout';
import HistorialPanel from '../components/features/HistorialPanel';
import LogsPanel from '../components/features/LogsPanel';

const TABS = [
  {
    id: 'historial',
    label: 'Registros',
    icon: (
      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
        <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6l4 2M12 2a10 10 0 1 1 0 20A10 10 0 0 1 12 2Z" />
      </svg>
    ),
    sub: 'Prácticas registradas',
  },
  {
    id: 'logs',
    label: 'Logs',
    icon: (
      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-6 4h6m2 4H7a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h5.586a1 1 0 0 1 .707.293l5.414 5.414A1 1 0 0 1 19 10.414V18a2 2 0 0 1-2 2Z" />
      </svg>
    ),
    sub: 'Auditoría del sistema',
  },
];

const Historial = () => {
  const [tab, setTab] = useState('historial');

  return (
    <Layout>
      <div className="space-y-5">

        {/* Header */}
        <div className="px-1 flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
          <div>
            <p className="text-[10px] lg:text-xs font-black text-[var(--istpet-gold)] uppercase tracking-[0.2em] mb-0">
              Registros del sistema
            </p>
            <h1 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase leading-tight">
              {TABS.find(t => t.id === tab)?.label}
            </h1>
          </div>

          {/* Tabs — pastillas estilo Control Operativo */}
          <div className="flex items-center gap-2 p-1 rounded-2xl bg-[var(--apple-bg)] border border-[var(--apple-border)] self-start sm:self-auto">
            {TABS.map(t => (
              <button
                key={t.id}
                onClick={() => setTab(t.id)}
                className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-black uppercase tracking-widest transition-all duration-200 ${
                  tab === t.id
                    ? 'bg-[var(--apple-card)] text-[var(--apple-text-main)] shadow-sm border border-[var(--apple-border)]'
                    : 'text-[var(--apple-text-sub)] hover:text-[var(--apple-text-main)] opacity-60 hover:opacity-100'
                }`}
              >
                {t.icon}
                {t.label}
              </button>
            ))}
          </div>
        </div>

        {/* Contenido de la pestaña activa */}
        {tab === 'historial' && <HistorialPanel />}
        {tab === 'logs' && <LogsPanel />}

      </div>
    </Layout>
  );
};

export default Historial;
