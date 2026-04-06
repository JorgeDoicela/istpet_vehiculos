import api from './api';

const logisticaService = {
  buscarEstudiante: async (cedula) => {
    try {
      const response = await api.get(`/logistica/estudiante/${cedula}`);
      return response.data.data; // { cedula, estudianteNombre, cursoDetalle, periodo, idMatricula }
    } catch (error) {
      if (error.response && error.response.status === 404) {
        throw new Error('Estudiante no encontrado o sin matrícula activa');
      }
      throw new Error(error.response?.data?.message || 'Error de conexión');
    }
  },

  buscarSugerencias: async (query) => {
    try {
      const response = await api.get(`/logistica/buscar?query=${query}`);
      return response.data.data; // List of { cedula, nombreCompleto }
    } catch (error) {
      console.error('Error fetching suggestions:', error);
      return [];
    }
  },

  getVehiculosDisponibles: async () => {
    try {
      const response = await api.get('/logistica/vehiculos-disponibles');
      return response.data.data;
    } catch (error) {
      throw new Error('No se cargaron los vehículos');
    }
  },

  getInstructores: async () => {
    try {
      const response = await api.get('/logistica/instructores');
      return response.data.data;
    } catch (error) {
      throw new Error('No se cargaron los instructores');
    }
  },

  registrarSalida: async (idMatricula, idVehiculo, idInstructor) => {
    try {
      // payload match with logisticaDTO SalidaRequest
      const response = await api.post('/logistica/salida', {
        idMatricula,
        idVehiculo,
        idInstructor,
        observaciones: 'Salida registrada en UI',
        registradoPor: 1
      });
      return response.data;
    } catch (error) {
       throw new Error(error.response?.data?.message || 'Error al procesar salida');
    }
  },

  registrarLlegada: async (idRegistro) => {
    try {
      const response = await api.post('/logistica/llegada', {
        idRegistro,
        observaciones: 'Llegada registrada en UI',
        registradoPor: 1
      });
      return response.data;
    } catch (error) {
       throw new Error(error.response?.data?.message || 'Error al procesar llegada');
    }
  },

  getAgendadosHoy: async () => {
    try {
      const response = await api.get('/logistica/agendados-hoy');
      return response.data.data;
    } catch (error) {
      throw new Error('No se cargaron los agendados del día');
    }
  }
};

export default logisticaService;
