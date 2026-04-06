import React from 'react';

const StatusBadge = ({ status }) => {
  const getStyles = () => {
    switch (status.toLowerCase()) {
      case 'disponible':
        return 'bg-emerald-100 text-emerald-700 border-emerald-200';
      case 'en pista':
        return 'bg-blue-100 text-blue-700 border-blue-200';
      case 'mantenimiento':
        return 'bg-rose-100 text-rose-700 border-rose-200';
      default:
        return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  };

  return (
    <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider border ${getStyles()}`}>
      {status}
    </span>
  );
};

export default StatusBadge;
