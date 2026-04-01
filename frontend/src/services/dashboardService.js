import api from './api';

/**
 * Dashboard Service - Unpacks ApiResponse<T> for UI components
 */
const dashboardService = {
  getClasesActivas: async () => {
    const response = await api.get('/Dashboard/clases-activas');
    // Desempaquetamos ApiResponse.Data
    return response.data?.data || [];
  },
  
  getAlertasMantenimiento: async () => {
    const response = await api.get('/Dashboard/alertas-mantenimiento');
    return response.data?.data || [];
  },
  
  syncStudents: async () => {
    const response = await api.post('/Sync/students');
    return response.data?.data || null;
  }
};

export default dashboardService;
