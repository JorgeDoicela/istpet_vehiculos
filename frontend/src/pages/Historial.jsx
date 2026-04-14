import React from 'react';
import Layout from '../components/layout/Layout';
import HistorialPanel from '../components/features/HistorialPanel';

const Historial = () => {
    return (
        <Layout>
            <div className="space-y-5">
                <div className="px-1">
                    <h1 className="text-3xl lg:text-5xl font-black tracking-tighter text-[var(--apple-text-main)] uppercase bg-clip-text text-transparent bg-gradient-to-b from-[var(--apple-text-main)] to-[var(--apple-text-sub)]">
                        Historial
                    </h1>
                    <p className="text-[var(--apple-text-sub)] font-bold text-[10px] lg:text-sm uppercase tracking-widest opacity-70 mt-1">
                        Prácticas · Estudiantes · Instructores · Vehículos
                    </p>
                </div>
                <HistorialPanel />
            </div>
        </Layout>
    );
};

export default Historial;
