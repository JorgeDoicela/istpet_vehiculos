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

  getAgendaReciente: async (limit = 12) => {
    const response = await api.get(`/Dashboard/agenda-reciente?limit=${encodeURIComponent(limit)}`);
    const body = response?.data;
    if (body && body.success === false) {
      throw new Error(body.message || 'Agenda no disponible');
    }
    const data = body?.data;
    return {
      practicas: Array.isArray(data?.practicas) ? data.practicas : [],
      fuenteDatos: data?.fuenteDatos || 'sigafi',
      obtenidoEn: data?.obtenidoEn ?? null
    };
  },
  
  syncStudents: async () => {
    const response = await api.post('/Sync/students');
    return response.data?.data || null;
  }
};

export default dashboardService;
