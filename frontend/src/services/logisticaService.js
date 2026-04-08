import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL + '/logistica';

export const logisticaService = {
  buscarEstudiante: async (idAlumno) => {
    const response = await axios.get(`${API_URL}/estudiante/${idAlumno}`);
    return response.data;
  },
  getVehiculosDisponibles: async () => {
    const response = await axios.get(`${API_URL}/vehiculos-disponibles`);
    return response.data;
  },
  getInstructores: async () => {
    const response = await axios.get(`${API_URL}/instructores`);
    return response.data;
  },
  registrarSalida: async (data) => {
    // data must include: idMatricula, idVehiculo, idInstructor, observaciones, registradoPor
    const response = await axios.post(`${API_URL}/salida`, data);
    return response.data;
  },
  registrarLlegada: async (data) => {
    // data must include: idRegistro, observaciones, registradoPor
    const response = await axios.post(`${API_URL}/llegada`, data);
    return response.data;
  },
  buscarSugerencias: async (query) => {
    const response = await axios.get(`${API_URL}/buscar?query=${query}`);
    return response.data;
  },
  getAgendadosHoy: async () => {
    const response = await axios.get(`${API_URL}/agendados-hoy`);
    return response.data;
  }
};
