import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const ProtectedRoute = ({ children, allowedRoles }) => {
    const { isAuthenticated, loading, isAuthorized } = useAuth();
    const location = useLocation();

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-screen bg-[#050a15] text-white">
                <div className="spinner"></div>
                <p className="ml-4 font-black uppercase tracking-widest text-xs">Validando Acceso...</p>
            </div>
        );
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    if (allowedRoles && !isAuthorized(allowedRoles)) {
        // Si no está autorizado para esta ruta, redirigir al home o dashboard principal
        return <Navigate to="/control-operativo" replace />;
    }

    return children;
};

export default ProtectedRoute;
