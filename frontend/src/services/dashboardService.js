import api from './api';
import { normalizeAgendaPractica } from '../utils/agendaUi';

/**
 * Dashboard Service - Unpacks ApiResponse<T> for UI components
 */
const dashboardService = {
  getClasesActivas: async () => {
    const response = await api.get('/Dashboard/clases-activas');
    const body = response?.data;
    return {
      clases: body?.data ?? body?.Data ?? [],
      serverTime: body?.timestamp ?? body?.Timestamp ?? null
    };
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
  },

  getAuditLogs: async ({ usuario, accion, fechaInicio, fechaFin, busqueda, limit = 200 } = {}) => {
    const params = new URLSearchParams();
    if (usuario) params.append('usuario', usuario);
    if (accion) params.append('accion', accion);
    if (fechaInicio) params.append('fechaInicio', fechaInicio);
    if (fechaFin) params.append('fechaFin', fechaFin);
    if (busqueda) params.append('busqueda', busqueda);
    params.append('limit', String(limit));

    const response = await api.get(`/Dashboard/audit-logs?${params.toString()}`);
    const body = response?.data;
    const ok = body?.success ?? body?.Success;
    if (ok === false) throw new Error(body?.message ?? body?.Message ?? 'Logs no disponibles');
    const data = body?.data ?? body?.Data;
    return Array.isArray(data) ? data : [];
  },

  getHistorialPracticas: async ({ fechaInicio, fechaFin, instructorId, busqueda, estado, limit = 200 } = {}) => {
    const params = new URLSearchParams();
    if (fechaInicio) params.append('fechaInicio', fechaInicio);
    if (fechaFin) params.append('fechaFin', fechaFin);
    if (instructorId) params.append('instructorId', instructorId);
    if (busqueda) params.append('busqueda', busqueda);
    if (estado) params.append('estado', estado);
    params.append('limit', String(limit));

    const response = await api.get(`/Dashboard/historial-practicas?${params.toString()}`, { timeout: 30000 });
    const body = response?.data;
    const ok = body?.success ?? body?.Success;
    if (ok === false) throw new Error(body?.message ?? body?.Message ?? 'Historial no disponible');
    const data = body?.data ?? body?.Data;
    return Array.isArray(data) ? data : [];
  }
};

export default dashboardService;
