import React, { createContext, useContext, useState, useEffect } from 'react';
import api from '../services/api';

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Cargar sesión persistente al iniciar
        const savedUser = localStorage.getItem('istpet_user');
        const token = localStorage.getItem('istpet_token');

        if (savedUser && token) {
            setUser(JSON.parse(savedUser));
        }
        setLoading(false);
    }, []);

    const login = async (username, password) => {
        try {
            const response = await api.post('/auth/login', {
                usuario: username,
                password: password
            });

            if (response.data.success) {
                const { token, ...userData } = response.data.data;
                
                // Guardar en Storage para persistencia
                localStorage.setItem('istpet_token', token);
                localStorage.setItem('istpet_user', JSON.stringify(userData));
                
                setUser(userData);
                return { success: true };
            }
            return { success: false, message: response.data.message };
        } catch (error) {
            return { 
                success: false, 
                message: error.response?.data?.message || 'Error de conexión con el servidor de seguridad.'
            };
        }
    };

    const logout = () => {
        localStorage.removeItem('istpet_token');
        localStorage.removeItem('istpet_user');
        setUser(null);
        window.location.href = '/login';
    };

    const isAuthorized = (allowedRoles) => {
        if (!user || !user.rol) return false;
        if (!allowedRoles || allowedRoles.length === 0) return true;
        return allowedRoles.includes(user.rol.toLowerCase());
    };

    return (
        <AuthContext.Provider value={{ user, login, logout, isAuthenticated: !!user, loading, isAuthorized }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth debe ser usado dentro de un AuthProvider');
    }
    return context;
};
