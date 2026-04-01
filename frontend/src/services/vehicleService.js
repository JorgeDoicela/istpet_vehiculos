import api from './api';

const vehicleService = {
  getAll: async () => {
    return await api.get('/Vehiculos');
  },
  getByPlaca: async (placa) => {
    return await api.get(`/Vehiculos/${placa}`);
  }
};

export default vehicleService;
