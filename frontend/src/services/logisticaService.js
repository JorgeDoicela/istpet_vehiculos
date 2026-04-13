import api from './api';
import { normalizeAgendaPractica } from '../utils/agendaUi';

/**
 * Extrae el payload de ApiResponse<T> del backend (camelCase JSON).
 */
function unwrap(response) {
  const body = response?.data;
  if (!body || typeof body !== 'object') return body;

  const hasSuccess =
    Object.prototype.hasOwnProperty.call(body, 'success') ||
    Object.prototype.hasOwnProperty.call(body, 'Success');

  if (hasSuccess) {
    const ok = body.success ?? body.Success;
    if (ok === false) {
      throw new Error(body.message ?? body.Message ?? 'Operación rechazada');
    }
    return body.data ?? body.Data;
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
    if (agendaCtx?.idAsignacionHorario != null && agendaCtx.idAsignacionHorario !== '') {
      p.set('idAsignacionHorario', String(agendaCtx.idAsignacionHorario));
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
    if (data && typeof data === 'object') {
      const practicas = data.practicas ?? data.Practicas;
      if (Array.isArray(practicas)) {
        return {
          practicas: practicas.map(normalizeAgendaPractica),
          fuenteDatos: data.fuenteDatos ?? data.FuenteDatos ?? 'sigafi',
          obtenidoEn: data.obtenidoEn ?? data.ObtenidoEn ?? null
        };
      }
    }
    return {
      practicas: Array.isArray(data) ? data.map(normalizeAgendaPractica) : [],
      fuenteDatos: 'sigafi',
      obtenidoEn: null
    };
  }
};
