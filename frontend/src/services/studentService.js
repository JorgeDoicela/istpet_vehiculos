import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL + '/estudiantes';

export const studentService = {
  getAll: async () => {
    const response = await axios.get(API_URL);
    return response.data;
  },
  getByIdAlumno: async (idAlumno) => {
    const response = await axios.get(`${API_URL}/${idAlumno}`);
    return response.data;
  }
};
