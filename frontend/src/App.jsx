import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Login from './pages/Login';
import Home from './pages/Home';
import ControlOperativo from './pages/ControlOperativo';
import Reports from './pages/Reports';

function App() {
    console.log('[APP] Inicializando sistema de seguridad Zenith ISTPET 2026...');

    return (
        <AuthProvider>
            <BrowserRouter>
                <div className="min-h-screen w-full overflow-x-hidden bg-[var(--apple-bg)]">
                    <Routes>
                        {/* Ruta Pública */}
                        <Route path="/login" element={<Login />} />

                        {/* Rutas Protegidas */}
                        <Route path="/" element={
                            <ProtectedRoute allowedRoles={['admin', 'logistica', 'guardia']}>
                                <ControlOperativo />
                            </ProtectedRoute>
                        } />
                        <Route path="/control-operativo" element={
                            <ProtectedRoute allowedRoles={['admin', 'logistica', 'guardia']}>
                                <ControlOperativo />
                            </ProtectedRoute>
                        } />
                        <Route path="/monitoreo" element={
                            <ProtectedRoute allowedRoles={['admin', 'logistica']}>
                                <Home />
                            </ProtectedRoute>
                        } />
                        <Route path="/estudiantes" element={<Navigate to="/control-operativo" replace />} />
                        <Route path="/vehiculos" element={<Navigate to="/control-operativo" replace />} />
                        <Route path="/reportes" element={
                            <ProtectedRoute allowedRoles={['admin']}>
                                <Reports />
                            </ProtectedRoute>
                        } />

                        {/* Redirección por defecto */}
                        <Route path="*" element={<Navigate to="/control-operativo" replace />} />
                    </Routes>
                </div>
            </BrowserRouter>
        </AuthProvider>
    );
}

export default App;
