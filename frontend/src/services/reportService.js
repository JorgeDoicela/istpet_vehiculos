import api from './api';

const reportService = {
    getReportePracticas: async (filtros = {}) => {
        try {
            const params = new URLSearchParams();
            if (filtros.fechaInicio) params.append('fechaInicio', filtros.fechaInicio);
            if (filtros.fechaFin) params.append('fechaFin', filtros.fechaFin);
            if (filtros.instructorId) params.append('instructorId', filtros.instructorId);

            const response = await api.get(`/Reports/practicas?${params.toString()}`, {
                timeout: 35000 // Tiempo de espera para reportes masivos
            });
            return response.data;
        } catch (error) {
            throw error.response?.data || { message: 'Error connection to server' };
        }
    }
};

export default reportService;
