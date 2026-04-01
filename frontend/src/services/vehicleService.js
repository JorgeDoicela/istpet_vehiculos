import api from './api';

/**
 * Vehicle Service - Unpacks ApiResponse<T> for Fleet Components
 */
const vehicleService = {
  getAll: async () => {
    const response = await api.get('/Vehiculos');
    return response.data?.data || [];
  },
  
  getByPlaca: async (placa) => {
    const response = await api.get(`/Vehiculos/${placa}`);
    return response.data?.data || null;
  }
};

export default vehicleService;
