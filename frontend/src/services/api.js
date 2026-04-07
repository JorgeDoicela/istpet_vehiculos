//Deploy
import axios from 'axios';

/**
 * ISTPET API - Ultra-Stable Axios Configuration
 * Port: 5112 (Development)
 */
const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5112/api',
    timeout: 30000,
    headers: {
        'Content-Type': 'application/json'
    }
});

// Logger Seguro de Peticiones
api.interceptors.request.use(config => {
    try {
        console.log(`[API REQUEST] ${config?.method?.toUpperCase() || 'GET'} -> ${config?.url || 'URL_UNKNOWN'}`);
    } catch (e) {
        // Silencio en modo producción si falla el log
    }
    return config;
}, error => {
    return Promise.reject(error);
});

// Logger Seguro de Respuestas
api.interceptors.response.use(
    (response) => {
        try {
            console.log(`[API RESPONSE SUCCESS] ${response?.config?.url || 'OK'}`);
        } catch (e) { }
        return response;
    },
    (error) => {
        const errorMsg = error.response?.data?.message || error.message || 'Error de conexión';
        console.error(`[API ERROR] ${errorMsg}`);
        return Promise.reject(error);
    }
);

export default api;
