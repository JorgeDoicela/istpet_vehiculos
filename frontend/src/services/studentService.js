import api from './api';

const studentService = {
  getByCedula: async (cedula) => {
    return await api.get(`/Estudiantes/${cedula}`);
  }
};

export default studentService;
