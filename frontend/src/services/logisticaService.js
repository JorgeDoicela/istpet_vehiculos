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
  buscarEstudiante: async (idAlumno, agendaCtx = null) => {
    const p = new URLSearchParams();
    if (agendaCtx?.idVehiculo != null && agendaCtx.idVehiculo !== '') {
      p.set('idVehiculoAgenda', String(agendaCtx.idVehiculo));
    }
    if (agendaCtx?.idProfesor != null && String(agendaCtx.idProfesor).trim() !== '') {
      p.set('idProfesorAgenda', String(agendaCtx.idProfesor).trim());
    }
    if (agendaCtx?.idPractica != null && agendaCtx.idPractica !== '') {
      p.set('idPracticaAgenda', String(agendaCtx.idPractica));
    }
    const qs = p.toString();
    const response = await api.get(`/logistica/estudiante/${encodeURIComponent(idAlumno)}${qs ? `?${qs}` : ''}`);
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
    const response = await api.get('/Dashboard/agenda-reciente?limit=100');
    const data = unwrap(response);
    if (data && typeof data === 'object' && Array.isArray(data.practicas)) {
      return {
        practicas: data.practicas,
        fuenteDatos: data.fuenteDatos || 'sigafi',
        obtenidoEn: data.obtenidoEn ?? null
      };
    }
    return {
      practicas: Array.isArray(data) ? data : [],
      fuenteDatos: 'sigafi',
      obtenidoEn: null
    };
  }
};
