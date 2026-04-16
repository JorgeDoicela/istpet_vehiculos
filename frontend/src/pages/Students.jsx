import React, { useState } from 'react';
import Layout from '../components/layout/Layout';
import StudentSearch from '../components/features/StudentSearch';
import SkeletonLoader from '../components/features/SkeletonLoader';
import { studentService } from '../services/studentService';
import { useToast } from '../context/ToastContext';

const Students = () => {
    const { success: toastSuccess, error: toastError } = useToast();
    const [student, setStudent] = useState(null);
    const [loading, setLoading] = useState(false);

    const handleSearchStudent = async (idAlumno) => {
        setLoading(true);
        setStudent(null);
        try {
            const data = await studentService.getByIdAlumno(idAlumno);
            setStudent(data);
            toastSuccess('Estudiante localizado con éxito');
        } catch {
            toastError('No se encontró ningún estudiante con esa identificación');
        } finally {
            setLoading(false);
        }
    };

    return (
        <Layout>

            <div className="space-y-12 max-w-5xl">
                <div className="animate-apple-in">
                    <h1 className="text-4xl lg:text-6xl font-black tracking-tighter text-[var(--apple-text-main)] uppercase bg-clip-text text-transparent bg-gradient-to-b from-[var(--apple-text-main)] to-[var(--apple-text-sub)]">
                        Gestión Estudiantil
                    </h1>
                    <p className="text-[var(--apple-text-sub)] font-bold text-[10px] uppercase tracking-[0.25em] mt-3">CONSULTA DE PERFILES ACADÉMICOS ISTPET 2026</p>
                </div>

                <section className="animate-apple-in" style={{ animationDelay: '0.1s' }}>
                    <StudentSearch onSearch={handleSearchStudent} loading={loading} />
                </section>

                {loading && (
                    <div className="apple-card p-12 bg-[var(--apple-card)] border-2 border-[var(--apple-border)] animate-apple-in">
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
                    <div className="apple-card p-12 flex flex-col md:flex-row items-center justify-between animate-apple-in">
                        <div className="flex items-center gap-10">
                            <div className="w-24 h-24 bg-[var(--istpet-navy)] rounded-[2.5rem] flex items-center justify-center text-white shadow-2xl relative overflow-hidden group">
                                <div className="absolute inset-0 bg-gradient-to-tr from-blue-600/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity"></div>
                                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1} stroke="currentColor" className="w-12 h-12 text-[var(--apple-primary)] relative z-10">
                                    <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.501 20.118a7.5 7.5 0 0 1 14.998 0A17.933 17.933 0 0 1 12 21.75c-2.676 0-5.216-.584-7.499-1.632Z" />
                                </svg>
                            </div>
                            <div>
                                <h3 className="text-3xl lg:text-4xl font-black uppercase tracking-tighter text-[var(--apple-text-main)] leading-tight">{student.nombreCompleto}</h3>
                                <div className="flex gap-4 mt-2">
                                    <span className="px-4 py-1 bg-emerald-500/10 text-emerald-500 rounded-full text-[10px] font-black uppercase tracking-widest border border-emerald-500/20">Matriculado</span>
                                    <p className="text-[var(--apple-text-sub)] font-mono text-xs tracking-[0.2em] self-center opacity-60">ID {student.idAlumno}</p>
                                </div>
                            </div>
                        </div>

                        <div className="mt-8 md:mt-0 flex gap-4">
                            <button className="px-6 py-3 bg-[var(--apple-bg)] text-[var(--apple-text-sub)] border border-[var(--apple-border)] rounded-2xl text-[10px] font-black uppercase tracking-widest hover:bg-[var(--apple-card)] transition-all">Reporte PDF</button>
                            <button className="btn-apple-primary px-8 py-3 rounded-2xl text-[10px] font-black uppercase tracking-widest">Perfil </button>
                        </div>
                    </div>
                )}
            </div>
        </Layout>
    );
};

export default Students;
