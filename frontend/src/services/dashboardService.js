import api from './api';
import { normalizeAgendaPractica } from '../utils/agendaUi';

/**
 * Dashboard Service - Unpacks ApiResponse<T> for UI components
 */
const dashboardService = {
  getClasesActivas: async () => {
    const response = await api.get('/Dashboard/clases-activas');
    const body = response?.data;
    return body?.data ?? body?.Data ?? [];
  },
  
  getAlertasMantenimiento: async () => {
    const response = await api.get('/Dashboard/alertas-mantenimiento');
    const body = response?.data;
    return body?.data ?? body?.Data ?? [];
  },

  getAgendaReciente: async (limit = 12) => {
    const response = await api.get(`/Dashboard/agenda-reciente?limit=${encodeURIComponent(limit)}`);
    const body = response?.data;
    const ok = body?.success ?? body?.Success;
    if (ok === false) {
      throw new Error(body?.message ?? body?.Message ?? 'Agenda no disponible');
    }
    const data = body?.data ?? body?.Data;
    const practicas = data?.practicas ?? data?.Practicas;
    return {
      practicas: Array.isArray(practicas) ? practicas.map(normalizeAgendaPractica) : [],
      fuenteDatos: data?.fuenteDatos ?? data?.FuenteDatos ?? 'sigafi',
      obtenidoEn: data?.obtenidoEn ?? data?.ObtenidoEn ?? null
    };
  },
  
  syncStudents: async () => {
    const response = await api.post('/Sync/students');
    const body = response?.data;
    return body?.data ?? body?.Data ?? null;
  },

  getHistorialHoy: async (limit = 10) => {
    const response = await api.get(`/Dashboard/agenda-historial?limit=${encodeURIComponent(limit)}`);
    const body = response?.data;
    const ok = body?.success ?? body?.Success;
    if (ok === false) {
      throw new Error(body?.message ?? body?.Message ?? 'Historial no disponible');
    }
    const data = body?.data ?? body?.Data;
    const practicas = data?.practicas ?? data?.Practicas;
    return {
      practicas: Array.isArray(practicas) ? practicas.map(normalizeAgendaPractica) : [],
      fuenteDatos: data?.fuenteDatos ?? data?.FuenteDatos ?? 'sigafi',
      obtenidoEn: data?.obtenidoEn ?? data?.ObtenidoEn ?? null
    };
  }
};

export default dashboardService;
