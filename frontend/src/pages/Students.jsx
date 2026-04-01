import React, { useState } from 'react';
import Layout from '../components/layout/Layout';
import StudentSearch from '../components/features/StudentSearch';
import studentService from '../services/studentService';

const Students = () => {
  const [student, setStudent] = useState(null);
  const [loading, setLoading] = useState(false);

  const handleSearchStudent = async (cedula) => {
    setLoading(true);
    setStudent(null);
    try {
      const data = await studentService.getByCedula(cedula);
      setStudent(data);
    } catch (err) {
      alert('Error: ' + err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Layout>
      <div className="space-y-12 max-w-5xl">
        <div>
          <h1 className="text-5xl font-black tracking-tighter text-slate-900 uppercase">Gestión de Alumnos</h1>
          <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-3">CONSULTA DE PERFILES ACADÉMICOS ISTPET</p>
        </div>

        <section className="animate-apple-in">
          <StudentSearch onSearch={handleSearchStudent} loading={loading} />
        </section>

        {student && (
          <div className="bg-white/80 apple-glass p-12 rounded-[3.5rem] shadow-2xl flex flex-col md:flex-row items-center justify-between border-white border-2 animate-apple-in">
            <div className="flex items-center gap-10">
              <div className="w-24 h-24 bg-slate-900 rounded-[2.5rem] flex items-center justify-center text-white shadow-2xl">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1} stroke="currentColor" className="w-12 h-12">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.501 20.118a7.5 7.5 0 0 1 14.998 0A17.933 17.933 0 0 1 12 21.75c-2.676 0-5.216-.584-7.499-1.632Z" />
                </svg>
              </div>
              <div>
                <h3 className="text-4xl font-black uppercase tracking-tighter text-slate-900">{student.nombreCompleto}</h3>
                <div className="flex gap-4 mt-2">
                  <span className="px-4 py-1 bg-blue-50 text-[var(--apple-primary)] rounded-full text-[10px] font-black uppercase tracking-widest border border-blue-100">Activo</span>
                  <p className="text-[var(--apple-text-sub)] font-mono text-xs tracking-widest self-center">ID {student.cedula}</p>
                </div>
              </div>
            </div>
            
            <div className="mt-8 md:mt-0 flex gap-4">
              <button className="btn-apple-secondary">Exportar PDF</button>
              <button className="btn-apple-primary">Ver Historial</button>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
};

export default Students;
