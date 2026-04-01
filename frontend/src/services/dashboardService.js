import api from './api';

const dashboardService = {
  getClasesActivas: async () => {
    return await api.get('/Dashboard/clases-activas');
  },
  getAlertasMantenimiento: async () => {
    return await api.get('/Dashboard/alertas-mantenimiento');
  },
  syncStudents: async () => {
    // Sincronización Profesional con fuentes externas
    return await api.post('/Sync/students');
  }
};

export default dashboardService;
