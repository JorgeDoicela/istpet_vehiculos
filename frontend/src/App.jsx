import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Login from './pages/Login';
import Home from './pages/Home';
import ControlOperativo from './pages/ControlOperativo';
import Reports from './pages/Reports';
import Students from './pages/Students';
import Vehicles from './pages/Vehicles';

function App() {
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
                            <ProtectedRoute allowedRoles={['admin', 'logistica', 'guardia']}>
                                <Home />
                            </ProtectedRoute>
                        } />
                        <Route path="/estudiantes" element={
                            <ProtectedRoute allowedRoles={['admin', 'logistica', 'guardia']}>
                                <Students />
                            </ProtectedRoute>
                        } />
                        <Route path="/vehiculos" element={
                            <ProtectedRoute allowedRoles={['admin', 'logistica', 'guardia']}>
                                <Vehicles />
                            </ProtectedRoute>
                        } />
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
