import api from './api';

/**
 * Extrae el payload de ApiResponse<T> del backend (camelCase JSON).
 */
function unwrap(response) {
  const body = response?.data;
  if (body && typeof body === 'object' && Object.prototype.hasOwnProperty.call(body, 'success')) {
    if (body.success === false) {
      throw new Error(body.message || 'Operación rechazada');
    }
    return body.data;
  }
  return body;
}

function unwrapList(response) {
  const v = unwrap(response);
  return Array.isArray(v) ? v : [];
}

/**
 * Usa `api` (JWT). Desempaqueta `data` del envelope para no romper `.filter` / `.map` en la UI.
 */
export const logisticaService = {
  buscarEstudiante: async (idAlumno) => {
    const response = await api.get(`/logistica/estudiante/${idAlumno}`);
    return unwrap(response);
  },
  getVehiculosDisponibles: async () => {
    const response = await api.get('/logistica/vehiculos-disponibles');
    return unwrapList(response);
  },
  getInstructores: async () => {
    const response = await api.get('/logistica/instructores');
    return unwrapList(response);
  },
  registrarSalida: async (data) => {
    const response = await api.post('/logistica/salida', data);
    return unwrap(response);
  },
  registrarLlegada: async (data) => {
    const response = await api.post('/logistica/llegada', data);
    return unwrap(response);
  },
  buscarSugerencias: async (query) => {
    try {
      const response = await api.get(`/logistica/buscar?query=${encodeURIComponent(query)}`);
      return unwrapList(response);
    } catch {
      return [];
    }
  },
  getAgendadosHoy: async () => {
    const response = await api.get('/logistica/agendados-hoy');
    return unwrapList(response);
  }
};
