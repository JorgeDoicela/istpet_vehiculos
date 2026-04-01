import React, { useState } from 'react';
import Layout from '../components/layout/Layout';
import StudentSearch from '../components/features/StudentSearch';
import SkeletonLoader from '../components/features/SkeletonLoader';
import studentService from '../services/studentService';

const Students = () => {
    const [student, setStudent] = useState(null);
    const [loading, setLoading] = useState(false);
    const [notification, setNotification] = useState(null);

    const showNotification = (message, type = 'success') => {
        setNotification({ message, type });
        setTimeout(() => setNotification(null), 4000);
    };

    const handleSearchStudent = async (cedula) => {
        setLoading(true);
        setStudent(null);
        try {
            const data = await studentService.getByCedula(cedula);
            setStudent(data);
            showNotification('Estudiante localizado con éxito');
        } catch (err) {
            showNotification('No se encontró ningún estudiante con esa identificación', 'error');
        } finally {
            setLoading(false);
        }
    };

    return (
        <Layout>
            {notification && (
                <div className="apple-toast border-white border animate-apple-in">
                    <div className={`w-3 h-12 rounded-full ${notification.type === 'error' ? 'bg-rose-500' : 'bg-blue-500'}`}></div>
                    <p className="text-sm font-bold text-slate-800">{notification.message}</p>
                </div>
            )}

            <div className="space-y-12 max-w-5xl">
                <div className="animate-apple-in">
                    <h1 className="text-5xl font-black tracking-tighter text-slate-900 uppercase">Gestión Estudiantil</h1>
                    <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-3">CONSULTA DE PERFILES ACADÉMICOS ISTPET 2026</p>
                </div>

                <section className="animate-apple-in" style={{ animationDelay: '0.1s' }}>
                    <StudentSearch onSearch={handleSearchStudent} loading={loading} />
                </section>

                {loading && (
                    <div className="apple-card p-12 bg-white/40 border-2 border-white animate-apple-in">
                        <div className="flex items-center gap-10">
                            <SkeletonLoader type="circle" className="w-24 h-24" />
                            <div className="flex-1 space-y-4">
                                <SkeletonLoader type="title" />
                                <SkeletonLoader type="text" className="w-1/3" />
                            </div>
                        </div>
                    </div>
                )}

                {student && (
                    <div className="bg-white/80 apple-glass p-12 rounded-[4rem] shadow-2xl flex flex-col md:flex-row items-center justify-between border-white border-2 animate-apple-in">
                        <div className="flex items-center gap-10">
                            <div className="w-24 h-24 bg-slate-900 rounded-[2.5rem] flex items-center justify-center text-white shadow-2xl">
                                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1} stroke="currentColor" className="w-12 h-12 text-blue-400">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.501 20.118a7.5 7.5 0 0 1 14.998 0A17.933 17.933 0 0 1 12 21.75c-2.676 0-5.216-.584-7.499-1.632Z" />
                                </svg>
                            </div>
                            <div>
                                <h3 className="text-4xl font-black uppercase tracking-tighter text-slate-900 leading-tight">{student.nombreCompleto}</h3>
                                <div className="flex gap-4 mt-2">
                                    <span className="px-4 py-1 bg-emerald-50 text-emerald-600 rounded-full text-[10px] font-black uppercase tracking-widest border border-emerald-100">Matriculado</span>
                                    <p className="text-[var(--apple-text-sub)] font-mono text-xs tracking-[0.2em] self-center">ID {student.cedula}</p>
                                </div>
                            </div>
                        </div>

                        <div className="mt-8 md:mt-0 flex gap-4">
                            <button className="btn-apple-secondary shadow-sm">Reporte PDF</button>
                            <button className="btn-apple-primary shadow-blue-500/10">Perfil </button>
                        </div>
                    </div>
                )}
            </div>
        </Layout>
    );
};

export default Students;
