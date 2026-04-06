import React, { useState } from 'react';

const StudentSearch = ({ onSearch, loading }) => {
  const [cedula, setCedula] = useState('');

  const handleSubmit = (e) => {
    e.preventDefault();
    if (cedula.trim()) onSearch(cedula);
  };

  return (
    <form onSubmit={handleSubmit} className="relative group max-w-2xl">
      <div className="absolute inset-y-0 left-6 flex items-center pointer-events-none text-[var(--apple-text-sub)] group-focus-within:text-[var(--apple-primary)] transition-colors duration-500">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor" className="w-6 h-6">
          <path strokeLinecap="round" strokeLinejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
        </svg>
      </div>
      <input
        type="text"
        placeholder="Identificación del Estudiante..."
        value={cedula}
        onChange={(e) => setCedula(e.target.value)}
        className="w-full pl-16 pr-32 py-8 bg-[var(--apple-card)] apple-glass rounded-[4rem] text-xl font-bold tracking-tight text-[var(--apple-text-main)] focus:outline-none focus:ring-4 focus:ring-[var(--apple-primary)]/10 placeholder-[var(--apple-text-sub)] transition-all duration-700 hover:bg-[var(--apple-bg)] border border-[var(--apple-border)]"
      />
      <button
        type="submit"
        disabled={loading}
        className="absolute right-4 top-4 bottom-4 px-10 bg-[var(--istpet-navy)] text-white rounded-[3rem] font-black text-xs uppercase tracking-widest hover:bg-[var(--istpet-navy)]/90 transition-all active:scale-95 disabled:bg-[var(--apple-border)] shadow-xl relative overflow-hidden group/btn"
      >
        <div className="absolute inset-0 bg-white/10 translate-y-full group-hover/btn:translate-y-0 transition-transform"></div>
        <span className="relative z-10">{loading ? '...' : 'Buscar'}</span>
      </button>
    </form>
  );
};

export default StudentSearch;
