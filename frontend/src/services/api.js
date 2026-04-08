// ISTPET API - Ultra-Stable Axios Configuration
import axios from 'axios';

const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5112/api',
    timeout: 30000,
    headers: {
        'Content-Type': 'application/json'
    }
});

// Interceptor de Peticiones: Añadir JWT
api.interceptors.request.use(config => {
    const token = localStorage.getItem('istpet_token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
}, error => {
    return Promise.reject(error);
});

// Interceptor de Respuestas: Manejo Profesional de Errores y Expiración
api.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401) {
            // Token expirado o inválido: Limpieza automática de sesión
            localStorage.removeItem('istpet_token');
            localStorage.removeItem('istpet_user');
            
            // Redirigir al login si no estamos ya allí
            if (!window.location.pathname.includes('/login')) {
                window.location.href = '/login?expired=true';
            }
        }
        
        const errorMsg = error.response?.data?.message || error.message || 'Error de conexión';
        console.error(`[API ERROR] ${errorMsg}`);
        return Promise.reject(error);
    }
);

export default api;
