import api from './api';

/**
 * Student Service - Unpacks ApiResponse<T> for Academic Components
 */
const studentService = {
  getByCedula: async (cedula) => {
    const response = await api.get(`/Estudiantes/${cedula}`);
    return response.data?.data || null;
  }
};

export default studentService;
