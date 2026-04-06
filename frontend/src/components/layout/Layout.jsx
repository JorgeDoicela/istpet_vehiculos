import React from 'react';
import Sidebar from './Sidebar';

const Layout = ({ children }) => {
  return (
    <div className="flex min-h-screen">
      {/* Sidebar Flotante */}
      <Sidebar />
      
      {/* Área de Contenido Principal */}
      <main className="flex-1 lg:ml-80 p-4 lg:p-12 transition-all duration-700 animate-apple-in">
        <header className="mb-8 lg:mb-16 flex items-center justify-between">
           <div>
              <h2 className="text-2xl lg:text-4xl font-black tracking-tight text-[var(--apple-text-main)]">Gestión Vehicular</h2>
              <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-2">ISTPET CONDUCCIÓN PROFESIONAL</p>
           </div>
           
           <div className="flex items-center gap-6">
              <button className="apple-glass p-4 rounded-3xl hover:scale-105 transition-all text-slate-800">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-6 h-6">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                </svg>
              </button>
              <div className="w-12 h-12 rounded-3xl bg-gradient-to-tr from-blue-400 to-indigo-600 shadow-xl border-4 border-white"></div>
           </div>
        </header>

        {children}
      </main>
    </div>
  );
};

export default Layout;
