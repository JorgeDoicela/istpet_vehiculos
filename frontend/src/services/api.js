import axios from 'axios';

/**
 * 🛰️ CONFIGURACIÓN DE API EMPRESARIAL
 * Centraliza la comunicación y el manejo de errores.
 */
const api = axios.create({
  baseURL: 'http://localhost:5112/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
});

// INTERCEPTOR DE RESPUESTA: Procesa el formato ApiResponse<T> automáticamente
api.interceptors.response.use(
  (response) => {
    // Si la respuesta es exitosa segun nuestro estandar ApiResponse
    if (response.data && response.data.success) {
      return response.data.data; // Devolvemos solo la carga útil (Data)
    }
    return Promise.reject(response.data?.message || 'Error desconocido del servidor');
  },
  (error) => {
    // Manejo global de desconexiones o errores 500
    const message = error.response?.data?.message || 'Error de conexión con el sistema ISTPET';
    console.error('API Error:', message);
    return Promise.reject(message);
  }
);

export default api;
