import React from 'react';
import Layout from '../components/layout/Layout';
import HistorialPanel from '../components/features/HistorialPanel';

const Historial = () => {
    return (
        <Layout>
            <div className="space-y-5">
                <div className="px-1">
                    <h1 className="text-lg lg:text-2xl font-black text-[var(--apple-text-main)] tracking-tighter uppercase leading-tight">
                        Historial
                    </h1>
                </div>
                <HistorialPanel />
            </div>
        </Layout>
    );
};

export default Historial;
